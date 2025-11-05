using System;

namespace Relay.Core.Testing;

/// <summary>
/// Represents performance metrics for a single operation.
/// </summary>
public class OperationMetrics
{
    /// <summary>
    /// Gets or sets the name of the operation.
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration of the operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the memory used by the operation in bytes.
    /// </summary>
    public long MemoryUsed { get; set; }

    /// <summary>
    /// Gets or sets the number of allocations made during the operation.
    /// </summary>
    public long Allocations { get; set; }

    /// <summary>
    /// Gets or sets the start time of the operation.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time of the operation.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Gets the average memory usage per millisecond.
    /// </summary>
    public double MemoryPerMs => Duration.TotalMilliseconds > 0 ? MemoryUsed / Duration.TotalMilliseconds : 0;

    /// <summary>
    /// Gets the allocation rate per millisecond.
    /// </summary>
    public double AllocationsPerMs => Duration.TotalMilliseconds > 0 ? Allocations / Duration.TotalMilliseconds : 0;
}