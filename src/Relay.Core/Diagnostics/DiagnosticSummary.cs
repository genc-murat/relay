using System;

namespace Relay.Core.Diagnostics;

/// <summary>
/// Summary of diagnostic information
/// </summary>
public class DiagnosticSummary
{
    /// <summary>
    /// Total number of registered handlers
    /// </summary>
    public int TotalHandlers { get; set; }

    /// <summary>
    /// Total number of registered pipelines
    /// </summary>
    public int TotalPipelines { get; set; }

    /// <summary>
    /// Number of active request traces
    /// </summary>
    public int ActiveTraces { get; set; }

    /// <summary>
    /// Number of completed request traces
    /// </summary>
    public int CompletedTraces { get; set; }

    /// <summary>
    /// Total number of handler invocations across all handlers
    /// </summary>
    public long TotalInvocations { get; set; }

    /// <summary>
    /// Total number of successful handler invocations
    /// </summary>
    public long TotalSuccessfulInvocations { get; set; }

    /// <summary>
    /// Total number of failed handler invocations
    /// </summary>
    public long TotalFailedInvocations { get; set; }

    /// <summary>
    /// Overall success rate as a percentage
    /// </summary>
    public double OverallSuccessRate =>
        TotalInvocations > 0 ? (double)TotalSuccessfulInvocations / TotalInvocations * 100 : 0;

    /// <summary>
    /// Average execution time across all handlers
    /// </summary>
    public TimeSpan AverageExecutionTime { get; set; }

    /// <summary>
    /// Total memory allocated across all handlers (bytes)
    /// </summary>
    public long TotalAllocatedBytes { get; set; }

    /// <summary>
    /// When the diagnostic data was last reset
    /// </summary>
    public DateTimeOffset? LastReset { get; set; }

    /// <summary>
    /// Uptime since diagnostics were initialized
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Whether request tracing is currently enabled
    /// </summary>
    public bool IsTracingEnabled { get; set; }

    /// <summary>
    /// Whether performance metrics collection is enabled
    /// </summary>
    public bool IsMetricsEnabled { get; set; }
}