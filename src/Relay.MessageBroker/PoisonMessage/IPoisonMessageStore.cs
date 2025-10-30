namespace Relay.MessageBroker.PoisonMessage;

/// <summary>
/// Defines the contract for storing and retrieving poison messages.
/// </summary>
public interface IPoisonMessageStore
{
    /// <summary>
    /// Stores a poison message.
    /// </summary>
    /// <param name="message">The poison message to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask StoreAsync(PoisonMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a poison message by ID.
    /// </summary>
    /// <param name="messageId">The ID of the poison message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The poison message if found, null otherwise.</returns>
    ValueTask<PoisonMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all poison messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of poison messages.</returns>
    ValueTask<IEnumerable<PoisonMessage>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a poison message by ID.
    /// </summary>
    /// <param name="messageId">The ID of the poison message to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask RemoveAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired poison messages based on the retention period.
    /// </summary>
    /// <param name="retentionPeriod">The retention period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages removed.</returns>
    ValueTask<int> CleanupExpiredAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing poison message.
    /// </summary>
    /// <param name="message">The poison message to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask UpdateAsync(PoisonMessage message, CancellationToken cancellationToken = default);
}
