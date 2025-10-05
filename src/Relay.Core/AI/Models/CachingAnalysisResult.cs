using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Caching analysis result
    /// </summary>
    internal class CachingAnalysisResult
    {
        public bool ShouldCache { get; set; }
        public double ExpectedHitRate { get; set; }
        public double ExpectedImprovement { get; set; }
        public double Confidence { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public CacheStrategy RecommendedStrategy { get; set; }
        public TimeSpan RecommendedTTL { get; set; }
    }
}
