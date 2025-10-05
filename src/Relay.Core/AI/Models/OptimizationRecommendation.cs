using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Represents optimization recommendations from the AI engine.
    /// </summary>
    public sealed class OptimizationRecommendation
    {
        public OptimizationStrategy Strategy { get; init; }
        public double ConfidenceScore { get; init; }
        public TimeSpan EstimatedImprovement { get; init; }
        public string Reasoning { get; init; } = string.Empty;
        public Dictionary<string, object> Parameters { get; init; } = new();
        public OptimizationPriority Priority { get; init; }
        
        /// <summary>
        /// Estimated performance gain percentage (0.0 to 1.0)
        /// </summary>
        public double EstimatedGainPercentage { get; init; }
        
        /// <summary>
        /// Risk level of applying this optimization
        /// </summary>
        public RiskLevel Risk { get; init; }
    }
}