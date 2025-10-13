using System;

namespace Relay.Core.Performance.Profiling;

/// <summary>
/// Aggregated performance statistics
/// </summary>
public readonly record struct PerformanceStatistics
{
    public string RequestType { get; init; }
    public long TotalRequests { get; init; }
    public long SuccessfulRequests { get; init; }
    public long FailedRequests { get; init; }
    public TimeSpan AverageExecutionTime { get; init; }
    public TimeSpan MinExecutionTime { get; init; }
    public TimeSpan MaxExecutionTime { get; init; }
    public TimeSpan P50ExecutionTime { get; init; }
    public TimeSpan P95ExecutionTime { get; init; }
    public TimeSpan P99ExecutionTime { get; init; }
    public long TotalMemoryAllocated { get; init; }
    public long AverageMemoryAllocated { get; init; }
    public long TotalGen0Collections { get; init; }
    public long TotalGen1Collections { get; init; }
    public long TotalGen2Collections { get; init; }

    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests * 100 : 0;

    public override string ToString()
    {
        return $"{RequestType}: {TotalRequests:N0} requests, " +
               $"Avg: {AverageExecutionTime.TotalMilliseconds:F2}ms, " +
               $"P95: {P95ExecutionTime.TotalMilliseconds:F2}ms, " +
               $"Success: {SuccessRate:F1}%";
    }
}
