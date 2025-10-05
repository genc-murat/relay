using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Resource optimization result
    /// </summary>
    internal class ResourceOptimizationResult
    {
        public bool ShouldOptimize { get; set; }
        public OptimizationStrategy Strategy { get; set; }
        public double Confidence { get; set; }
        public TimeSpan EstimatedImprovement { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public OptimizationPriority Priority { get; set; }
        public RiskLevel Risk { get; set; }
        public double GainPercentage { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}
