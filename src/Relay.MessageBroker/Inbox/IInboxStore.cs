namespace Relay.MessageBroker.Inbox;

/// <summary>
/// Defines the contract for storing and checking inbox messages for idempotent processing.
/// </summary>
public interface IInboxStore
{
    /// <summary>
    /// Checks if a message ID already exists in the inbox.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message has already been processed, false otherwise.</returns>
    ValueTask<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a processed message in the inbox.
    /// </summary>
    /// <param name="message">The inbox message to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask StoreAsync(InboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired inbox entries based on the retention period.
    /// </summary>
    /// <param name="retentionPeriod">The retention period for inbox entries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of entries removed.</returns>
    ValueTask<int> CleanupExpiredAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
}
