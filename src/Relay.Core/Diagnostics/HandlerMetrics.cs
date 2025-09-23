using System;
using System.Collections.Generic;

namespace Relay.Core.Diagnostics;

/// <summary>
/// Performance metrics for a specific handler
/// </summary>
public class HandlerMetrics
{
    /// <summary>
    /// The request type this handler processes
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// The handler type
    /// </summary>
    public string HandlerType { get; set; } = string.Empty;

    /// <summary>
    /// Total number of times this handler has been invoked
    /// </summary>
    public long InvocationCount { get; set; }

    /// <summary>
    /// Total time spent executing this handler
    /// </summary>
    public TimeSpan TotalExecutionTime { get; set; }

    /// <summary>
    /// Average execution time per invocation
    /// </summary>
    public TimeSpan AverageExecutionTime =>
        InvocationCount > 0 ? TimeSpan.FromTicks(TotalExecutionTime.Ticks / InvocationCount) : TimeSpan.Zero;

    /// <summary>
    /// Minimum execution time observed
    /// </summary>
    public TimeSpan MinExecutionTime { get; set; } = TimeSpan.MaxValue;

    /// <summary>
    /// Maximum execution time observed
    /// </summary>
    public TimeSpan MaxExecutionTime { get; set; }

    /// <summary>
    /// Number of successful executions
    /// </summary>
    public long SuccessCount { get; set; }

    /// <summary>
    /// Number of failed executions
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Success rate as a percentage
    /// </summary>
    public double SuccessRate =>
        InvocationCount > 0 ? (double)SuccessCount / InvocationCount * 100 : 0;

    /// <summary>
    /// Last time this handler was invoked
    /// </summary>
    public DateTimeOffset? LastInvocation { get; set; }

    /// <summary>
    /// Total memory allocated by this handler (bytes)
    /// </summary>
    public long TotalAllocatedBytes { get; set; }

    /// <summary>
    /// Average memory allocated per invocation (bytes)
    /// </summary>
    public long AverageAllocatedBytes =>
        InvocationCount > 0 ? TotalAllocatedBytes / InvocationCount : 0;

    /// <summary>
    /// Additional custom metrics
    /// </summary>
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}