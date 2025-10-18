using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Configuration options for AI prediction cache
    /// </summary>
    public class AIPredictionCacheOptions
    {
        /// <summary>
        /// Maximum number of entries in the cache. Default is 10000.
        /// </summary>
        public int MaxSize { get; set; } = 10000;

        /// <summary>
        /// Default expiry time for cache entries. Default is 30 minutes.
        /// </summary>
        public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Cleanup interval for expired entries. Default is 5 minutes.
        /// </summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to enable cache statistics collection. Default is true.
        /// </summary>
        public bool EnableStatistics { get; set; } = true;

        /// <summary>
        /// Eviction policy to use. Default is LRU.
        /// </summary>
        public CacheEvictionPolicy EvictionPolicy { get; set; } = CacheEvictionPolicy.LRU;
    }

    /// <summary>
    /// Cache eviction policies
    /// </summary>
    public enum CacheEvictionPolicy
    {
        /// <summary>
        /// Least Recently Used - removes oldest accessed entries first
        /// </summary>
        LRU,

        /// <summary>
        /// Least Frequently Used - removes least accessed entries first
        /// </summary>
        LFU,

        /// <summary>
        /// First In First Out - removes oldest entries first
        /// </summary>
        FIFO
    }
}