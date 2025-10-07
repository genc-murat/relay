using System;
using System.Collections.Generic;

namespace Relay.Core.Telemetry;

/// <summary>
/// Statistics for handler execution
/// </summary>
public class HandlerExecutionStats
{
    public Type RequestType { get; set; } = null!;
    public string? HandlerName { get; set; }
    public long TotalExecutions { get; set; }
    public long SuccessfulExecutions { get; set; }
    public long FailedExecutions { get; set; }
    public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions : 0;
    public TimeSpan AverageExecutionTime { get; set; }
    public TimeSpan MinExecutionTime { get; set; }
    public TimeSpan MaxExecutionTime { get; set; }
    public TimeSpan P50ExecutionTime { get; set; }
    public TimeSpan P95ExecutionTime { get; set; }
    public TimeSpan P99ExecutionTime { get; set; }
    public DateTimeOffset LastExecution { get; set; }
    public long TotalMemoryAllocated { get; set; }
    public long AverageMemoryAllocated { get; set; }
    public long MinMemoryAllocated { get; set; }
    public long MaxMemoryAllocated { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}
