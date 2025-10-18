using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Represents a cache entry with metadata
    /// </summary>
    public class CacheEntry
    {
        public OptimizationRecommendation Recommendation { get; init; } = null!;
        public DateTime ExpiresAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public int AccessCount { get; set; }
        public DateTime LastAccessedAt { get; set; }
    }
}