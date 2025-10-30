namespace Relay.MessageBroker.PoisonMessage;

/// <summary>
/// Defines the contract for handling poison messages.
/// </summary>
public interface IPoisonMessageHandler
{
    /// <summary>
    /// Handles a poison message by storing it for later analysis or reprocessing.
    /// </summary>
    /// <param name="message">The poison message to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask HandleAsync(PoisonMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all poison messages from storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of poison messages.</returns>
    ValueTask<IEnumerable<PoisonMessage>> GetPoisonMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reprocesses a poison message by attempting to handle it again.
    /// </summary>
    /// <param name="messageId">The ID of the poison message to reprocess.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask ReprocessAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a message processing failure.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="messageType">The message type.</param>
    /// <param name="payload">The message payload.</param>
    /// <param name="error">The error that occurred.</param>
    /// <param name="context">The message context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message should be moved to poison queue, false otherwise.</returns>
    ValueTask<bool> TrackFailureAsync(
        string messageId,
        string messageType,
        byte[] payload,
        string error,
        MessageContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired poison messages based on the retention period.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages removed.</returns>
    ValueTask<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}
