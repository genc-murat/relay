namespace Relay.MessageBroker.Deduplication;

/// <summary>
/// Interface for message deduplication cache.
/// </summary>
public interface IDeduplicationCache
{
    /// <summary>
    /// Checks if a message is a duplicate.
    /// </summary>
    /// <param name="messageHash">The message hash to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message is a duplicate, false otherwise.</returns>
    ValueTask<bool> IsDuplicateAsync(string messageHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a message hash to the cache.
    /// </summary>
    /// <param name="messageHash">The message hash to add.</param>
    /// <param name="ttl">Time-to-live for the cache entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask AddAsync(string messageHash, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current deduplication metrics.
    /// </summary>
    /// <returns>The deduplication metrics.</returns>
    DeduplicationMetrics GetMetrics();
}
