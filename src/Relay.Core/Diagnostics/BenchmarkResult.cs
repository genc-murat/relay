using System;
using System.Collections.Generic;

namespace Relay.Core.Diagnostics;

/// <summary>
/// Results from performance benchmarking of handlers
/// </summary>
public class BenchmarkResult
{
    /// <summary>
    /// The request type that was benchmarked
    /// </summary>
    public string RequestType { get; set; } = string.Empty;
    
    /// <summary>
    /// The handler type that was benchmarked
    /// </summary>
    public string HandlerType { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of iterations executed
    /// </summary>
    public int Iterations { get; set; }
    
    /// <summary>
    /// Total time for all iterations
    /// </summary>
    public TimeSpan TotalTime { get; set; }
    
    /// <summary>
    /// Average execution time per iteration
    /// </summary>
    public TimeSpan AverageTime => TimeSpan.FromTicks(TotalTime.Ticks / Math.Max(1, Iterations));
    
    /// <summary>
    /// Minimum execution time observed
    /// </summary>
    public TimeSpan MinTime { get; set; }
    
    /// <summary>
    /// Maximum execution time observed
    /// </summary>
    public TimeSpan MaxTime { get; set; }
    
    /// <summary>
    /// Standard deviation of execution times
    /// </summary>
    public TimeSpan StandardDeviation { get; set; }
    
    /// <summary>
    /// Total memory allocated during benchmarking (bytes)
    /// </summary>
    public long TotalAllocatedBytes { get; set; }
    
    /// <summary>
    /// Average memory allocated per iteration (bytes)
    /// </summary>
    public long AverageAllocatedBytes => TotalAllocatedBytes / Math.Max(1, Iterations);
    
    /// <summary>
    /// When the benchmark was executed
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Additional performance metrics
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();
}