using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Relay.Core.ContractValidation;
using Relay.MessageBroker.Compression;

namespace Relay.MessageBroker.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of the message broker.
/// </summary>
public sealed class RabbitMQMessageBroker : BaseMessageBroker
{
    private IConnection? _connection;
    private IChannel? _publishChannel;
    private readonly List<IChannel> _consumerChannels = new();

    public RabbitMQMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger<RabbitMQMessageBroker> logger,
        IMessageCompressor? compressor = null,
        IContractValidator? contractValidator = null)
        : base(options, logger, compressor, contractValidator)
    {
        var rabbitMqOptions = options.Value.RabbitMQ ?? new RabbitMQOptions();
        if (string.IsNullOrEmpty(rabbitMqOptions.HostName))
        {
            throw new ArgumentException("HostName cannot be null or empty.", nameof(rabbitMqOptions.HostName));
        }
        if (rabbitMqOptions.Port <= 0 || rabbitMqOptions.Port > 65535)
        {
            throw new ArgumentException("Port must be between 1 and 65535.", nameof(rabbitMqOptions.Port));
        }
        if (string.IsNullOrEmpty(rabbitMqOptions.UserName))
        {
            throw new ArgumentException("UserName cannot be null or empty.", nameof(rabbitMqOptions.UserName));
        }
        if (rabbitMqOptions.Password == null)
        {
            throw new ArgumentException("Password cannot be null.", nameof(rabbitMqOptions.Password));
        }
        if (string.IsNullOrEmpty(rabbitMqOptions.VirtualHost))
        {
            throw new ArgumentException("VirtualHost cannot be null or empty.", nameof(rabbitMqOptions.VirtualHost));
        }
        if (string.IsNullOrEmpty(rabbitMqOptions.ExchangeType))
        {
            throw new ArgumentException("ExchangeType cannot be null or empty.", nameof(rabbitMqOptions.ExchangeType));
        }
        var validExchangeTypes = new[] { "direct", "topic", "fanout", "headers" };
        if (!validExchangeTypes.Contains(rabbitMqOptions.ExchangeType.ToLowerInvariant()))
        {
            throw new ArgumentException($"ExchangeType must be one of: {string.Join(", ", validExchangeTypes)}.", nameof(rabbitMqOptions.ExchangeType));
        }
    }

    protected override async ValueTask PublishInternalAsync<TMessage>(
        TMessage message,
        byte[] serializedMessage,
        PublishOptions? options,
        CancellationToken cancellationToken)
    {
        await EnsureConnectionAsync(cancellationToken);

        var messageType = typeof(TMessage);
        var routingKey = options?.RoutingKey ?? GetRoutingKey(messageType);
        var exchange = options?.Exchange ?? _options.DefaultExchange;

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
            body: serializedMessage,
            cancellationToken: cancellationToken);

        _logger.LogDebug(
            "Published message of type {MessageType} to exchange {Exchange} with routing key {RoutingKey}",
            messageType.Name,
            exchange,
            routingKey);
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
        await EnsureConnectionAsync(cancellationToken);

        foreach (var subscriptionGroup in _subscriptions.Values)
        {
            foreach (var subscription in subscriptionGroup)
            {
                await SetupConsumerAsync(subscription, cancellationToken);
            }
        }

        _logger.LogInformation("RabbitMQ message broker started");
    }

    protected override async ValueTask StopInternalAsync(CancellationToken cancellationToken)
    {
        foreach (var channel in _consumerChannels)
        {
            await channel.CloseAsync(cancellationToken);
            channel.Dispose();
        }

        _consumerChannels.Clear();

        _logger.LogInformation("RabbitMQ message broker stopped");
    }

    protected override async ValueTask DisposeInternalAsync()
    {
        _publishChannel?.Dispose();
        _connection?.Dispose();
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
                var message = DeserializeMessage<object>(ea.Body.ToArray());
                var context = CreateMessageContext(ea, channel);

                await ProcessMessageAsync(message, subscription.MessageType, context, CancellationToken.None);

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
}