namespace Relay.MessageBroker.Outbox;

/// <summary>
/// Represents the status of an outbox message.
/// </summary>
public enum OutboxMessageStatus
{
    /// <summary>
    /// The message is pending publication.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The message has been successfully published.
    /// </summary>
    Published = 1,

    /// <summary>
    /// The message failed to publish after maximum retry attempts.
    /// </summary>
    Failed = 2
}
