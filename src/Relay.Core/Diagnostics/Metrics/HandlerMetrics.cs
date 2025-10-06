using System;

namespace Relay.Core.Diagnostics.Metrics;

/// <summary>
/// Metrics for a specific handler.
/// </summary>
public class HandlerMetrics
{
    /// <summary>
    /// Gets or sets the handler name.
    /// </summary>
    public string HandlerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request type.
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of invocations.
    /// </summary>
    public int InvocationCount { get; set; }

    /// <summary>
    /// Gets or sets the total execution time.
    /// </summary>
    public TimeSpan TotalExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the minimum execution time.
    /// </summary>
    public TimeSpan MinExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the maximum execution time.
    /// </summary>
    public TimeSpan MaxExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the number of successful executions.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed executions.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the handler type.
    /// </summary>
    public Type? HandlerType { get; set; }

    /// <summary>
    /// Gets or sets the last invocation time.
    /// </summary>
    public DateTime LastInvocation { get; set; }

    /// <summary>
    /// Gets or sets the total allocated bytes.
    /// </summary>
    public long TotalAllocatedBytes { get; set; }

    /// <summary>
    /// Gets the average execution time.
    /// </summary>
    public TimeSpan AverageExecutionTime => 
        InvocationCount > 0 ? TimeSpan.FromTicks(TotalExecutionTime.Ticks / InvocationCount) : TimeSpan.Zero;

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate => 
        InvocationCount > 0 ? (double)SuccessCount / InvocationCount * 100 : 0;

    /// <summary>
    /// Gets the error rate as a percentage.
    /// </summary>
    public double ErrorRate => 
        InvocationCount > 0 ? (double)ErrorCount / InvocationCount * 100 : 0;
}