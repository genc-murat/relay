namespace Relay.Core.Configuration
{
    /// <summary>
    /// Configuration options for caching.
    /// </summary>
    public class CachingOptions
    {
        /// <summary>
        /// Gets or sets whether to enable automatic caching for all requests.
        /// </summary>
        public bool EnableAutomaticCaching { get; set; } = false;

        /// <summary>
        /// Gets or sets the default cache duration in seconds.
        /// </summary>
        public int DefaultCacheDurationSeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets whether to use sliding expiration for cached items.
        /// </summary>
        public bool UseSlidingExpiration { get; set; } = false;

        /// <summary>
        /// Gets or sets the sliding expiration time in seconds.
        /// </summary>
        public int SlidingExpirationSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the maximum size of the cache in megabytes.
        /// </summary>
        public long SizeLimitMegabytes { get; set; } = 100;

        /// <summary>
        /// Gets or sets the cache key prefix.
        /// </summary>
        public string CacheKeyPrefix { get; set; } = "RelayCache";

        /// <summary>
        /// Gets or sets whether to enable distributed caching.
        /// </summary>
        public bool EnableDistributedCaching { get; set; } = false;

        /// <summary>
        /// Gets or sets the default order for caching pipeline behaviors.
        /// </summary>
        public int DefaultOrder { get; set; } = -500; // Run early in the pipeline
    }
}