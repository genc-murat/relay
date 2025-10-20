namespace Relay.MessageBroker;

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
