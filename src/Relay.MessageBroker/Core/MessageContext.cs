namespace Relay.MessageBroker;

using System.Collections.Generic;

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
