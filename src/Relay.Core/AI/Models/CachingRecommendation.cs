using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Caching recommendations from AI analysis.
    /// </summary>
    public sealed class CachingRecommendation
    {
        public bool ShouldCache { get; init; }
        public TimeSpan RecommendedTtl { get; init; }
        public CacheStrategy Strategy { get; init; }
        public double ExpectedHitRate { get; init; }
        public string CacheKey { get; init; } = string.Empty;
        public CacheScope Scope { get; init; }
        public double ConfidenceScore { get; init; }

        /// <summary>
        /// Estimated memory savings from caching
        /// </summary>
        public long EstimatedMemorySavings { get; init; }

        /// <summary>
        /// Estimated performance improvement
        /// </summary>
        public TimeSpan EstimatedPerformanceGain { get; init; }

        /// <summary>
        /// Predicted cache hit rate (0.0 to 1.0)
        /// </summary>
        public double PredictedHitRate { get; init; }

        /// <summary>
        /// Cache key generation strategy
        /// </summary>
        public CacheKeyStrategy KeyStrategy { get; init; }

        /// <summary>
        /// Properties to use for key generation (when using SelectedProperties strategy)
        /// </summary>
        public string[] KeyProperties { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Cache priority level
        /// </summary>
        public CachePriority Priority { get; init; }

        /// <summary>
        /// Whether to use distributed cache
        /// </summary>
        public bool UseDistributedCache { get; init; }
    }
}