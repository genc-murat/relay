using System;
using System.Text.Json;

namespace Relay.Core.AI
{
    /// <summary>
    /// Configuration options for AICachingOptimizationBehavior.
    /// </summary>
    public class AICachingOptimizationOptions
    {
        /// <summary>
        /// Gets or sets whether caching optimization is enabled.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum confidence score to apply cached optimizations.
        /// </summary>
        public double MinConfidenceScore { get; set; } = 0.7;

        /// <summary>
        /// Gets or sets the minimum execution time (ms) to consider caching.
        /// </summary>
        public double MinExecutionTimeForCaching { get; set; } = 10.0;

        /// <summary>
        /// Gets or sets the maximum cache size in bytes.
        /// </summary>
        public long MaxCacheSizeBytes { get; set; } = 1024 * 1024; // 1MB

        /// <summary>
        /// Gets or sets the default cache TTL.
        /// </summary>
        public TimeSpan DefaultCacheTtl { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets or sets the minimum cache TTL for dynamic TTL calculation.
        /// </summary>
        public TimeSpan MinCacheTtl { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the maximum cache TTL for dynamic TTL calculation.
        /// </summary>
        public TimeSpan MaxCacheTtl { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets the JSON serializer options for cache key generation.
        /// </summary>
        public JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
}
