using System;

namespace Relay.Core.Performance.Profiling;

/// <summary>
/// Individual request performance metrics
/// </summary>
public readonly record struct RequestPerformanceMetrics
{
    public string RequestType { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public long MemoryAllocated { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public bool Success { get; init; }
}
