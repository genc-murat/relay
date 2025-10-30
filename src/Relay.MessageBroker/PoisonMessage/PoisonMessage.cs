namespace Relay.MessageBroker.PoisonMessage;

/// <summary>
/// Represents a message that has repeatedly failed processing and is considered poisonous.
/// </summary>
public sealed class PoisonMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the poison message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the message type name.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized message payload.
    /// </summary>
    public byte[] Payload { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the number of times the message has failed processing.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the list of error messages from failed processing attempts.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when the message first failed.
    /// </summary>
    public DateTimeOffset FirstFailureAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the message last failed.
    /// </summary>
    public DateTimeOffset LastFailureAt { get; set; }

    /// <summary>
    /// Gets or sets the original message ID.
    /// </summary>
    public string? OriginalMessageId { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for tracking.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the message headers.
    /// </summary>
    public Dictionary<string, object>? Headers { get; set; }

    /// <summary>
    /// Gets or sets the routing key for the message.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Gets or sets the exchange name for the message.
    /// </summary>
    public string? Exchange { get; set; }
}
