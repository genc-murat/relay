using System;

namespace Relay.Core.Caching.Attributes;

/// <summary>
/// Unified cache attribute with comprehensive configuration options for all caching scenarios.
/// Replaces CacheAttribute, EnhancedCacheAttribute, and DistributedCacheAttribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class UnifiedCacheAttribute : Attribute
{
    /// <summary>
    /// Gets the absolute expiration time for the cache, in seconds.
    /// </summary>
    public int AbsoluteExpirationSeconds { get; set; } = 300;

    /// <summary>
    /// Gets the sliding expiration time for the cache, in seconds.
    /// </summary>
    public int SlidingExpirationSeconds { get; set; } = 0;

    /// <summary>
    /// Gets the cache tags for grouping and invalidation.
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets the cache priority.
    /// </summary>
    public CachePriority Priority { get; set; } = CachePriority.Normal;

    /// <summary>
    /// Gets whether to enable compression for this cache entry.
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Gets whether this cache entry should be preloaded.
    /// </summary>
    public bool Preload { get; set; } = false;

    /// <summary>
    /// Gets the cache region for logical grouping.
    /// </summary>
    public string Region { get; set; } = "default";

    /// <summary>
    /// Gets the custom cache key pattern.
    /// </summary>
    public string KeyPattern { get; set; } = "{RequestType}:{RequestHash}";

    /// <summary>
    /// Gets whether to enable metrics collection for this cache entry.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets whether to use distributed cache for this request type.
    /// </summary>
    public bool UseDistributedCache { get; set; } = false;

    /// <summary>
    /// Gets whether caching is enabled for this request type.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedCacheAttribute"/> class with default settings.
    /// </summary>
    public UnifiedCacheAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedCacheAttribute"/> class with specified absolute expiration.
    /// </summary>
    /// <param name="absoluteExpirationSeconds">The absolute expiration time in seconds.</param>
    public UnifiedCacheAttribute(int absoluteExpirationSeconds)
    {
        if (absoluteExpirationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteExpirationSeconds), "Cache duration must be a positive number.");
        }
        AbsoluteExpirationSeconds = absoluteExpirationSeconds;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedCacheAttribute"/> class with specified absolute and sliding expiration.
    /// </summary>
    /// <param name="absoluteExpirationSeconds">The absolute expiration time in seconds.</param>
    /// <param name="slidingExpirationSeconds">The sliding expiration time in seconds.</param>
    public UnifiedCacheAttribute(int absoluteExpirationSeconds, int slidingExpirationSeconds)
    {
        if (absoluteExpirationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteExpirationSeconds), "Cache duration must be a positive number.");
        }
        if (slidingExpirationSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slidingExpirationSeconds), "Sliding expiration cannot be negative.");
        }
        
        AbsoluteExpirationSeconds = absoluteExpirationSeconds;
        SlidingExpirationSeconds = slidingExpirationSeconds;
    }
}

/// <summary>
/// Cache priority levels.
/// </summary>
public enum CachePriority
{
    /// <summary>
    /// Low priority - first to be evicted.
    /// </summary>
    Low,

    /// <summary>
    /// Normal priority.
    /// </summary>
    Normal,

    /// <summary>
    /// High priority - less likely to be evicted.
    /// </summary>
    High,

    /// <summary>
    /// Never automatically evict.
    /// </summary>
    Never
}