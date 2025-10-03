using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.MessageBroker.Kafka;

/// <summary>
/// Kafka implementation of the message broker.
/// </summary>
public sealed class KafkaMessageBroker : IMessageBroker, IDisposable
{
    private readonly MessageBrokerOptions _options;
    private readonly ILogger<KafkaMessageBroker> _logger;
    private readonly ConcurrentDictionary<Type, List<SubscriptionInfo>> _subscriptions = new();
    private IProducer<string, byte[]>? _producer;
    private readonly List<IConsumer<string, byte[]>> _consumers = new();
    private readonly CancellationTokenSource _consumeCts = new();
    private bool _isStarted;
    private bool _disposed;

    public KafkaMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger<KafkaMessageBroker> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        EnsureProducer();

        var messageType = typeof(TMessage);
        var topic = options?.RoutingKey ?? GetTopicName(messageType);
        var key = options?.Headers?.ContainsKey("Key") == true
            ? options.Headers["Key"]?.ToString() ?? Guid.NewGuid().ToString()
            : Guid.NewGuid().ToString();

        var body = SerializeMessage(message);
        var headers = new Headers();

        headers.Add("MessageType", Encoding.UTF8.GetBytes(messageType.FullName ?? messageType.Name));
        headers.Add("MessageId", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
        headers.Add("Timestamp", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")));

        if (options?.Headers != null)
        {
            foreach (var header in options.Headers)
            {
                if (header.Key != "Key" && header.Value != null)
                {
                    headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value.ToString() ?? string.Empty));
                }
            }
        }

        var kafkaMessage = new Message<string, byte[]>
        {
            Key = key,
            Value = body,
            Headers = headers
        };

        var result = await _producer!.ProduceAsync(topic, kafkaMessage, cancellationToken);

        _logger.LogDebug(
            "Published message of type {MessageType} to topic {Topic} at partition {Partition} offset {Offset}",
            messageType.Name,
            topic,
            result.Partition,
            result.Offset);
    }

    public async ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var messageType = typeof(TMessage);
        var subscriptionInfo = new SubscriptionInfo
        {
            MessageType = messageType,
            Handler = async (msg, ctx, ct) => await handler((TMessage)msg, ctx, ct),
            Options = options ?? new SubscriptionOptions()
        };

        _subscriptions.AddOrUpdate(
            messageType,
            _ => new List<SubscriptionInfo> { subscriptionInfo },
            (_, list) =>
            {
                list.Add(subscriptionInfo);
                return list;
            });

        if (_isStarted)
        {
            await SetupConsumerAsync(subscriptionInfo, cancellationToken);
        }

        _logger.LogInformation(
            "Subscribed to messages of type {MessageType}",
            messageType.Name);

        await ValueTask.CompletedTask;
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted)
        {
            return;
        }

        EnsureProducer();

        foreach (var subscriptionGroup in _subscriptions.Values)
        {
            foreach (var subscription in subscriptionGroup)
            {
                await SetupConsumerAsync(subscription, cancellationToken);
            }
        }

        _isStarted = true;
        _logger.LogInformation("Kafka message broker started");
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted)
        {
            return;
        }

        await _consumeCts.CancelAsync();

        foreach (var consumer in _consumers)
        {
            consumer.Close();
            consumer.Dispose();
        }

        _consumers.Clear();

        _isStarted = false;
        _logger.LogInformation("Kafka message broker stopped");
    }

    private void EnsureProducer()
    {
        if (_producer != null)
        {
            return;
        }

        var kafkaOptions = _options.Kafka ?? new KafkaOptions();
        var config = new ProducerConfig
        {
            BootstrapServers = kafkaOptions.BootstrapServers,
            CompressionType = Enum.Parse<CompressionType>(kafkaOptions.CompressionType, true),
            EnableIdempotence = true,
            MaxInFlight = 5,
            Acks = Acks.All
        };

        if (!string.IsNullOrEmpty(_options.ConnectionString))
        {
            config.BootstrapServers = _options.ConnectionString;
        }

        _producer = new ProducerBuilder<string, byte[]>(config).Build();

        _logger.LogInformation("Created Kafka producer for {BootstrapServers}", config.BootstrapServers);
    }

    private async ValueTask SetupConsumerAsync(SubscriptionInfo subscription, CancellationToken cancellationToken)
    {
        var kafkaOptions = _options.Kafka ?? new KafkaOptions();
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaOptions.BootstrapServers,
            GroupId = subscription.Options.ConsumerGroup ?? kafkaOptions.ConsumerGroupId,
            AutoOffsetReset = Enum.Parse<AutoOffsetReset>(kafkaOptions.AutoOffsetReset, true),
            EnableAutoCommit = kafkaOptions.EnableAutoCommit,
            SessionTimeoutMs = (int)kafkaOptions.SessionTimeout.TotalMilliseconds
        };

        if (!string.IsNullOrEmpty(_options.ConnectionString))
        {
            config.BootstrapServers = _options.ConnectionString;
        }

        var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        var topic = subscription.Options.RoutingKey ?? GetTopicName(subscription.MessageType);

        consumer.Subscribe(topic);
        _consumers.Add(consumer);

        // Start consuming in background
        _ = Task.Run(async () =>
        {
            try
            {
                while (!_consumeCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(_consumeCts.Token);

                        if (consumeResult?.Message != null)
                        {
                            await ProcessMessageAsync(consumeResult, subscription, consumer);
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error for topic {Topic}", topic);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Kafka consumer loop for topic {Topic}", topic);
            }
        }, _consumeCts.Token);

        _logger.LogInformation(
            "Setup Kafka consumer for topic {Topic} with group {GroupId}",
            topic,
            config.GroupId);

        await ValueTask.CompletedTask;
    }

    private async ValueTask ProcessMessageAsync(
        ConsumeResult<string, byte[]> result,
        SubscriptionInfo subscription,
        IConsumer<string, byte[]> consumer)
    {
        try
        {
            var message = DeserializeMessage(result.Message.Value, subscription.MessageType);
            var context = CreateMessageContext(result, consumer);

            await subscription.Handler(message, context, CancellationToken.None);

            if (!subscription.Options.AutoAck && !_options.Kafka!.EnableAutoCommit)
            {
                consumer.Commit(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing Kafka message from topic {Topic} partition {Partition} offset {Offset}",
                result.Topic,
                result.Partition,
                result.Offset);

            // Don't commit on error - message will be reprocessed
        }
    }

    private MessageContext CreateMessageContext(
        ConsumeResult<string, byte[]> result,
        IConsumer<string, byte[]> consumer)
    {
        var headers = new Dictionary<string, object>();

        if (result.Message.Headers != null)
        {
            foreach (var header in result.Message.Headers)
            {
                headers[header.Key] = Encoding.UTF8.GetString(header.GetValueBytes());
            }
        }

        var messageId = headers.ContainsKey("MessageId") ? headers["MessageId"]?.ToString() : null;
        var correlationId = headers.ContainsKey("CorrelationId") ? headers["CorrelationId"]?.ToString() : null;
        var timestampStr = headers.ContainsKey("Timestamp") ? headers["Timestamp"]?.ToString() : null;
        var timestamp = !string.IsNullOrEmpty(timestampStr)
            ? DateTimeOffset.Parse(timestampStr)
            : result.Message.Timestamp.UtcDateTime;

        return new MessageContext
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            Timestamp = timestamp,
            Headers = headers,
            RoutingKey = result.Topic,
            Acknowledge = async () =>
            {
                consumer.Commit(result);
                await ValueTask.CompletedTask;
            },
            Reject = async (requeue) =>
            {
                // Kafka doesn't have explicit reject/requeue
                // Not committing will cause reprocessing on consumer restart
                if (!requeue)
                {
                    consumer.Commit(result);
                }
                await ValueTask.CompletedTask;
            }
        };
    }

    private string GetTopicName(Type messageType)
    {
        return _options.DefaultRoutingKeyPattern
            .Replace("{MessageType}", messageType.Name)
            .Replace("{MessageFullName}", messageType.FullName ?? messageType.Name)
            .ToLowerInvariant();
    }

    private byte[] SerializeMessage<TMessage>(TMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        return Encoding.UTF8.GetBytes(json);
    }

    private object DeserializeMessage(byte[] body, Type messageType)
    {
        var json = Encoding.UTF8.GetString(body);
        return JsonSerializer.Deserialize(json, messageType)!;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopAsync().AsTask().Wait();

        _producer?.Dispose();
        _consumeCts.Dispose();

        _disposed = true;
    }

    private sealed class SubscriptionInfo
    {
        public required Type MessageType { get; init; }
        public required Func<object, MessageContext, CancellationToken, ValueTask> Handler { get; init; }
        public required SubscriptionOptions Options { get; init; }
    }
}
