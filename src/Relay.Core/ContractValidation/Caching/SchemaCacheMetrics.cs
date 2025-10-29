using System;

namespace Relay.Core.ContractValidation.Caching;

/// <summary>
/// Represents metrics for schema cache performance monitoring.
/// </summary>
public sealed class SchemaCacheMetrics
{
    /// <summary>
    /// Gets the total number of cache requests.
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    /// Gets the number of cache hits.
    /// </summary>
    public long CacheHits { get; init; }

    /// <summary>
    /// Gets the number of cache misses.
    /// </summary>
    public long CacheMisses { get; init; }

    /// <summary>
    /// Gets the cache hit rate as a percentage (0.0 to 1.0).
    /// </summary>
    public double HitRate => TotalRequests > 0 ? (double)CacheHits / TotalRequests : 0;

    /// <summary>
    /// Gets the current number of items in the cache.
    /// </summary>
    public int CurrentSize { get; init; }

    /// <summary>
    /// Gets the maximum cache size.
    /// </summary>
    public int MaxSize { get; init; }

    /// <summary>
    /// Gets the total number of evictions that have occurred.
    /// </summary>
    public long TotalEvictions { get; init; }

    /// <summary>
    /// Gets the timestamp when these metrics were captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
