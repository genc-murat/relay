using System;
using System.Collections.Generic;

namespace Relay.Core.AI;

/// <summary>
/// Result of an optimization operation.
/// </summary>
public class StrategyExecutionResult
{
    public bool Success { get; set; }
    public string StrategyName { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public object? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
