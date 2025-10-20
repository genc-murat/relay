using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Configures AI-powered caching recommendations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class IntelligentCachingAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to enable AI-based cache analysis.
        /// </summary>
        public bool EnableAIAnalysis { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum access frequency for caching consideration.
        /// </summary>
        public int MinAccessFrequency { get; set; } = 5;

        /// <summary>
        /// Gets or sets the minimum cache hit rate prediction for enabling caching.
        /// </summary>
        public double MinPredictedHitRate { get; set; } = 0.3;

        /// <summary>
        /// Gets or sets the cache scope for AI recommendations.
        /// </summary>
        public CacheScope PreferredScope { get; set; } = CacheScope.Global;

        /// <summary>
        /// Gets or sets the preferred cache strategy.
        /// </summary>
        public CacheStrategy PreferredStrategy { get; set; } = CacheStrategy.Adaptive;

        /// <summary>
        /// Gets or sets whether to use dynamic TTL based on access patterns.
        /// </summary>
        public bool UseDynamicTtl { get; set; } = true;
    }
}