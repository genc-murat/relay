namespace Relay.MessageBroker.Outbox;

/// <summary>
/// Defines the contract for storing and retrieving outbox messages.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Stores a message in the outbox.
    /// </summary>
    /// <param name="message">The message to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored message with generated ID.</returns>
    ValueTask<OutboxMessage> StoreAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves pending messages from the outbox.
    /// </summary>
    /// <param name="batchSize">The maximum number of messages to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of pending messages.</returns>
    ValueTask<IEnumerable<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as successfully published.
    /// </summary>
    /// <param name="messageId">The ID of the message to mark as published.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as failed with an error message.
    /// </summary>
    /// <param name="messageId">The ID of the message to mark as failed.</param>
    /// <param name="error">The error message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves failed messages from the outbox.
    /// </summary>
    /// <param name="batchSize">The maximum number of messages to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of failed messages.</returns>
    ValueTask<IEnumerable<OutboxMessage>> GetFailedAsync(int batchSize, CancellationToken cancellationToken = default);
}
