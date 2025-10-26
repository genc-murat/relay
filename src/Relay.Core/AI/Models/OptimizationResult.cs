using System;

namespace Relay.Core.AI.Models;

/// <summary>
/// Optimization result tracking
/// </summary>
public class OptimizationResult
{
    public OptimizationStrategy Strategy { get; init; }
    public RequestExecutionMetrics ActualMetrics { get; init; } = null!;
    public DateTime Timestamp { get; init; }
}
