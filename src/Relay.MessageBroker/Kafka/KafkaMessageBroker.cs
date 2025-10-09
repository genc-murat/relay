using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.MessageBroker.Compression;
using Relay.Core.ContractValidation;

namespace Relay.MessageBroker.Kafka;

/// <summary>
/// Kafka implementation of the message broker.
/// </summary>
public sealed class KafkaMessageBroker : BaseMessageBroker
{
    private IProducer<string, byte[]>? _producer;
    private readonly List<IConsumer<string, byte[]>> _consumers = new();
    private readonly CancellationTokenSource _consumeCts = new();

    public KafkaMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger<KafkaMessageBroker> logger,
        IMessageCompressor? compressor = null,
        IContractValidator? contractValidator = null)
        : base(options, logger, compressor, contractValidator)
    {
    }

protected override async ValueTask PublishInternalAsync<TMessage>(
        TMessage message,
        byte[] serializedMessage,
        PublishOptions? options,
        CancellationToken cancellationToken)
    {
        EnsureProducer();

        var messageType = typeof(TMessage);
        var topic = options?.RoutingKey ?? GetTopicName(messageType);
        var key = options?.Headers?.ContainsKey("Key") == true
            ? options.Headers["Key"]?.ToString() ?? Guid.NewGuid().ToString()
            : Guid.NewGuid().ToString();

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
            Value = serializedMessage,
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

protected override async ValueTask SubscribeInternalAsync(
        Type messageType,
        SubscriptionInfo subscriptionInfo,
        CancellationToken cancellationToken)
    {
        await SetupConsumerAsync(subscriptionInfo, cancellationToken);
    }

protected override async ValueTask StartInternalAsync(CancellationToken cancellationToken)
    {
        EnsureProducer();

        foreach (var subscriptionGroup in _subscriptions.Values)
        {
            foreach (var subscription in subscriptionGroup)
            {
                await SetupConsumerAsync(subscription, cancellationToken);
            }
        }

        _logger.LogInformation("Kafka message broker started");
    }

protected override async ValueTask StopInternalAsync(CancellationToken cancellationToken)
    {
        await _consumeCts.CancelAsync();

        foreach (var consumer in _consumers)
        {
            consumer.Close();
            consumer.Dispose();
        }

        _consumers.Clear();

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
            var message = DeserializeMessage<object>(result.Message.Value);
            var context = CreateMessageContext(result, consumer);

            await ProcessMessageAsync(message, subscription.MessageType, context, CancellationToken.None);

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

protected override async ValueTask DisposeInternalAsync()
    {
        _producer?.Dispose();
        _consumeCts.Dispose();
    }
}
