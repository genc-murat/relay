using System;
using System.Collections.Generic;

namespace Relay.Core.AI.Models;

/// <summary>
/// Resource optimization result
/// </summary>
public class ResourceOptimizationResult
{
    public bool ShouldOptimize { get; set; }
    public OptimizationStrategy Strategy { get; set; }
    public double Confidence { get; set; }
    public TimeSpan EstimatedImprovement { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = [];
    public TimeSpan EstimatedSavings { get; set; }
    public OptimizationPriority Priority { get; set; }
    public RiskLevel Risk { get; set; }
    public double GainPercentage { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = [];
}
