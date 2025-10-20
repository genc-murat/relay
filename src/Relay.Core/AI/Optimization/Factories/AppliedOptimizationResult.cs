using System;

namespace Relay.Core.AI;

/// <summary>
/// Represents the result of an applied optimization strategy.
/// </summary>
public class AppliedOptimizationResult
{
    public OptimizationStrategy Strategy { get; set; }
    public bool Success { get; set; }
    public TimeSpan? ActualImprovement { get; set; }
    public TimeSpan? ExpectedImprovement { get; set; }
    public double ConfidenceScore { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}