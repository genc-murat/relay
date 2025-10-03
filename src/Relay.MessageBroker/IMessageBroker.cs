namespace Relay.MessageBroker;

/// <summary>
/// Represents a message broker abstraction for publishing and subscribing to messages.
/// </summary>
public interface IMessageBroker
{
    /// <summary>
    /// Publishes a message to the message broker.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">Optional publishing options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask PublishAsync<TMessage>(TMessage message, PublishOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to messages of a specific type.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="handler">The handler to process messages.</param>
    /// <param name="options">Optional subscription options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SubscribeAsync<TMessage>(Func<TMessage, MessageContext, CancellationToken, ValueTask> handler, SubscriptionOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts consuming messages.
    /// </summary>
    ValueTask StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops consuming messages.
    /// </summary>
    ValueTask StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Publishing options for message brokers.
/// </summary>
public sealed class PublishOptions
{
    /// <summary>
    /// Gets or sets the routing key or topic name.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Gets or sets the exchange name (for RabbitMQ).
    /// </summary>
    public string? Exchange { get; set; }

    /// <summary>
    /// Gets or sets custom headers.
    /// </summary>
    public Dictionary<string, object>? Headers { get; set; }

    /// <summary>
    /// Gets or sets the message priority (0-9).
    /// </summary>
    public byte? Priority { get; set; }

    /// <summary>
    /// Gets or sets the message expiration time.
    /// </summary>
    public TimeSpan? Expiration { get; set; }

    /// <summary>
    /// Gets or sets whether the message should be persisted.
    /// </summary>
    public bool Persistent { get; set; } = true;
}

/// <summary>
/// Subscription options for message brokers.
/// </summary>
public sealed class SubscriptionOptions
{
    /// <summary>
    /// Gets or sets the queue name.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the routing key or topic pattern.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Gets or sets the exchange name (for RabbitMQ).
    /// </summary>
    public string? Exchange { get; set; }

    /// <summary>
    /// Gets or sets the consumer group (for Kafka).
    /// </summary>
    public string? ConsumerGroup { get; set; }

    /// <summary>
    /// Gets or sets the prefetch count.
    /// </summary>
    public ushort? PrefetchCount { get; set; }

    /// <summary>
    /// Gets or sets whether to auto-acknowledge messages.
    /// </summary>
    public bool AutoAck { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the queue should be durable.
    /// </summary>
    public bool Durable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the queue should be exclusive.
    /// </summary>
    public bool Exclusive { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the queue should auto-delete.
    /// </summary>
    public bool AutoDelete { get; set; } = false;
}

/// <summary>
/// Message context providing metadata about the message.
/// </summary>
public sealed class MessageContext
{
    /// <summary>
    /// Gets or sets the message ID.
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for message tracking.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the message was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets custom headers.
    /// </summary>
    public Dictionary<string, object>? Headers { get; set; }

    /// <summary>
    /// Gets or sets the routing key or topic.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Gets or sets the exchange name (for RabbitMQ).
    /// </summary>
    public string? Exchange { get; set; }

    /// <summary>
    /// Acknowledges the message.
    /// </summary>
    public Func<ValueTask>? Acknowledge { get; set; }

    /// <summary>
    /// Rejects the message and optionally requeues it.
    /// </summary>
    public Func<bool, ValueTask>? Reject { get; set; }
}
