using System;
using System.Collections.Generic;

namespace Relay.Core.AI;

/// <summary>
/// Context for optimization operations.
/// </summary>
public class OptimizationContext
{
    public string Operation { get; set; } = string.Empty;
    public Type? RequestType { get; set; }
    public object? Request { get; set; }
    public RequestExecutionMetrics? ExecutionMetrics { get; set; }
    public SystemLoadMetrics? SystemLoad { get; set; }
    public AccessPattern[]? AccessPatterns { get; set; }
    public AppliedOptimizationResult[]? AppliedStrategies { get; set; }
    public TimeSpan? AnalysisWindow { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}
