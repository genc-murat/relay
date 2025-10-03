using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Relay.MessageBroker.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of the message broker.
/// </summary>
public sealed class RabbitMQMessageBroker : IMessageBroker, IDisposable
{
    private readonly MessageBrokerOptions _options;
    private readonly ILogger<RabbitMQMessageBroker> _logger;
    private readonly ConcurrentDictionary<Type, List<SubscriptionInfo>> _subscriptions = new();
    private IConnection? _connection;
    private IChannel? _publishChannel;
    private readonly List<IChannel> _consumerChannels = new();
    private bool _isStarted;
    private bool _disposed;

    public RabbitMQMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger<RabbitMQMessageBroker> logger)
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

        await EnsureConnectionAsync(cancellationToken);

        var messageType = typeof(TMessage);
        var routingKey = options?.RoutingKey ?? GetRoutingKey(messageType);
        var exchange = options?.Exchange ?? _options.DefaultExchange;

        var body = SerializeMessage(message);
        var properties = new BasicProperties
        {
            Persistent = options?.Persistent ?? true,
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            ContentType = "application/json",
            Type = messageType.FullName
        };

        if (options?.Priority.HasValue == true)
        {
            properties.Priority = options.Priority.Value;
        }

        if (options?.Expiration.HasValue == true)
        {
            properties.Expiration = options.Expiration.Value.TotalMilliseconds.ToString("F0");
        }

        if (options?.Headers != null)
        {
            properties.Headers = new Dictionary<string, object?>();
            foreach (var header in options.Headers)
            {
                properties.Headers[header.Key] = header.Value;
            }
        }

        await _publishChannel!.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogDebug(
            "Published message of type {MessageType} to exchange {Exchange} with routing key {RoutingKey}",
            messageType.Name,
            exchange,
            routingKey);
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
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted)
        {
            return;
        }

        await EnsureConnectionAsync(cancellationToken);

        foreach (var subscriptionGroup in _subscriptions.Values)
        {
            foreach (var subscription in subscriptionGroup)
            {
                await SetupConsumerAsync(subscription, cancellationToken);
            }
        }

        _isStarted = true;
        _logger.LogInformation("RabbitMQ message broker started");
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted)
        {
            return;
        }

        foreach (var channel in _consumerChannels)
        {
            await channel.CloseAsync(cancellationToken);
            channel.Dispose();
        }

        _consumerChannels.Clear();

        _isStarted = false;
        _logger.LogInformation("RabbitMQ message broker stopped");
    }

    private async ValueTask EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection != null && _connection.IsOpen)
        {
            return;
        }

        var rabbitOptions = _options.RabbitMQ ?? new RabbitMQOptions();
        var factory = new ConnectionFactory
        {
            HostName = rabbitOptions.HostName,
            Port = rabbitOptions.Port,
            UserName = rabbitOptions.UserName,
            Password = rabbitOptions.Password,
            VirtualHost = rabbitOptions.VirtualHost,
            RequestedConnectionTimeout = rabbitOptions.ConnectionTimeout
        };

        if (!string.IsNullOrEmpty(_options.ConnectionString))
        {
            factory.Uri = new Uri(_options.ConnectionString);
        }

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _publishChannel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Declare default exchange
        await _publishChannel.ExchangeDeclareAsync(
            exchange: _options.DefaultExchange,
            type: rabbitOptions.ExchangeType,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Connected to RabbitMQ at {HostName}:{Port}", rabbitOptions.HostName, rabbitOptions.Port);
    }

    private async ValueTask SetupConsumerAsync(SubscriptionInfo subscription, CancellationToken cancellationToken)
    {
        var channel = await _connection!.CreateChannelAsync(cancellationToken: cancellationToken);
        var rabbitOptions = _options.RabbitMQ ?? new RabbitMQOptions();

        var prefetchCount = subscription.Options.PrefetchCount ?? rabbitOptions.PrefetchCount;
        await channel.BasicQosAsync(0, prefetchCount, false, cancellationToken);

        var queueName = subscription.Options.QueueName ?? $"relay.{subscription.MessageType.Name}";
        var routingKey = subscription.Options.RoutingKey ?? GetRoutingKey(subscription.MessageType);
        var exchange = subscription.Options.Exchange ?? _options.DefaultExchange;

        // Declare queue
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: subscription.Options.Durable,
            exclusive: subscription.Options.Exclusive,
            autoDelete: subscription.Options.AutoDelete,
            arguments: null,
            cancellationToken: cancellationToken);

        // Bind queue to exchange
        await channel.QueueBindAsync(
            queue: queueName,
            exchange: exchange,
            routingKey: routingKey,
            arguments: null,
            cancellationToken: cancellationToken);

        // Setup consumer
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var message = DeserializeMessage(ea.Body.ToArray(), subscription.MessageType);
                var context = CreateMessageContext(ea, channel);

                await subscription.Handler(message, context, CancellationToken.None);

                if (!subscription.Options.AutoAck)
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);

                if (!subscription.Options.AutoAck)
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            }
        };

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: subscription.Options.AutoAck,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _consumerChannels.Add(channel);

        _logger.LogInformation(
            "Setup consumer for queue {QueueName} with routing key {RoutingKey}",
            queueName,
            routingKey);
    }

    private MessageContext CreateMessageContext(BasicDeliverEventArgs ea, IChannel channel)
    {
        var headers = ea.BasicProperties.Headers != null
            ? new Dictionary<string, object>(ea.BasicProperties.Headers
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!))
            : null;

        return new MessageContext
        {
            MessageId = ea.BasicProperties.MessageId,
            CorrelationId = ea.BasicProperties.CorrelationId,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(ea.BasicProperties.Timestamp.UnixTime),
            Headers = headers,
            RoutingKey = ea.RoutingKey,
            Exchange = ea.Exchange,
            Acknowledge = async () => await channel.BasicAckAsync(ea.DeliveryTag, false),
            Reject = async (requeue) => await channel.BasicNackAsync(ea.DeliveryTag, false, requeue)
        };
    }

    private string GetRoutingKey(Type messageType)
    {
        return _options.DefaultRoutingKeyPattern
            .Replace("{MessageType}", messageType.Name)
            .Replace("{MessageFullName}", messageType.FullName ?? messageType.Name);
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

        _publishChannel?.Dispose();
        _connection?.Dispose();

        _disposed = true;
    }

    private sealed class SubscriptionInfo
    {
        public required Type MessageType { get; init; }
        public required Func<object, MessageContext, CancellationToken, ValueTask> Handler { get; init; }
        public required SubscriptionOptions Options { get; init; }
    }
}
