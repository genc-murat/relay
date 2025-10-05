using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Performance analysis result
    /// </summary>
    internal class PerformanceAnalysisResult
    {
        public bool ShouldOptimize { get; set; }
        public OptimizationStrategy RecommendedStrategy { get; set; }
        public double Confidence { get; set; }
        public TimeSpan EstimatedImprovement { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public OptimizationPriority Priority { get; set; }
        public RiskLevel Risk { get; set; }
        public double GainPercentage { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}
