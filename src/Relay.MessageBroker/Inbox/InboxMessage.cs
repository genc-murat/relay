namespace Relay.MessageBroker.Inbox;

/// <summary>
/// Represents a processed message stored in the inbox for idempotent processing.
/// </summary>
public sealed class InboxMessage
{
    /// <summary>
    /// Gets or sets the unique message identifier.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message type name.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the message was processed.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets the name of the consumer that processed the message.
    /// </summary>
    public string? ConsumerName { get; set; }
}
