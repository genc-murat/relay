using System;

namespace Relay.Core.Caching.Metrics;

/// <summary>
/// Interface for cache metrics collection.
/// </summary>
public interface ICacheMetrics
{
    /// <summary>
    /// Records a cache hit.
    /// </summary>
    void RecordHit(string cacheKey, string requestType);

    /// <summary>
    /// Records a cache miss.
    /// </summary>
    void RecordMiss(string cacheKey, string requestType);

    /// <summary>
    /// Records cache set operation.
    /// </summary>
    void RecordSet(string cacheKey, string requestType, long dataSize);

    /// <summary>
    /// Records cache eviction.
    /// </summary>
    void RecordEviction(string cacheKey, string requestType);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    CacheStatistics GetStatistics(string? requestType = null);
}

/// <summary>
/// Cache statistics.
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Total number of cache hits.
    /// </summary>
    public long Hits { get; set; }

    /// <summary>
    /// Total number of cache misses.
    /// </summary>
    public long Misses { get; set; }

    /// <summary>
    /// Total number of cache sets.
    /// </summary>
    public long Sets { get; set; }

    /// <summary>
    /// Total number of cache evictions.
    /// </summary>
    public long Evictions { get; set; }

    /// <summary>
    /// Total data size cached in bytes.
    /// </summary>
    public long TotalDataSize { get; set; }

    /// <summary>
    /// Hit ratio (Hits / (Hits + Misses)).
    /// </summary>
    public double HitRatio => (Hits + Misses) > 0 ? (double)Hits / (Hits + Misses) : 0;

    /// <summary>
    /// Average data size per cache entry.
    /// </summary>
    public double AverageDataSize => Sets > 0 ? (double)TotalDataSize / Sets : 0;
}