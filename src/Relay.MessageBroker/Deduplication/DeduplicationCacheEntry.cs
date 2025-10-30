namespace Relay.MessageBroker.Deduplication;

/// <summary>
/// Represents an entry in the deduplication cache.
/// </summary>
internal sealed class DeduplicationCacheEntry
{
    /// <summary>
    /// Gets or sets the message hash.
    /// </summary>
    public required string MessageHash { get; init; }

    /// <summary>
    /// Gets or sets the expiration time.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Gets or sets the last access time (for LRU eviction).
    /// </summary>
    public DateTimeOffset LastAccessedAt { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Checks if the entry has expired.
    /// </summary>
    public bool IsExpired() => DateTimeOffset.UtcNow >= ExpiresAt;
}
