namespace Relay.MessageBroker.Deduplication;

/// <summary>
/// Metrics for deduplication operations.
/// </summary>
public sealed class DeduplicationMetrics
{
    /// <summary>
    /// Gets or sets the current cache size.
    /// </summary>
    public int CurrentCacheSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of messages checked.
    /// </summary>
    public long TotalMessagesChecked { get; set; }

    /// <summary>
    /// Gets or sets the total number of duplicates detected.
    /// </summary>
    public long TotalDuplicatesDetected { get; set; }

    /// <summary>
    /// Gets or sets the duplicate detection rate (0.0 to 1.0).
    /// </summary>
    public double DuplicateDetectionRate { get; set; }

    /// <summary>
    /// Gets or sets the cache hit rate (0.0 to 1.0).
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// Gets or sets the total number of cache evictions.
    /// </summary>
    public long TotalEvictions { get; set; }

    /// <summary>
    /// Gets or sets the last cleanup timestamp.
    /// </summary>
    public DateTimeOffset? LastCleanupAt { get; set; }
}
