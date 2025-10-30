namespace Relay.MessageBroker.Deduplication;

/// <summary>
/// Defines the strategy for message deduplication.
/// </summary>
public enum DeduplicationStrategy
{
    /// <summary>
    /// Use content-based hash (SHA256) for deduplication.
    /// </summary>
    ContentHash,

    /// <summary>
    /// Use message ID for deduplication.
    /// </summary>
    MessageId,

    /// <summary>
    /// Use custom deduplication logic.
    /// </summary>
    Custom
}
