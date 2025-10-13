namespace Relay.Core.Performance.Profiling;

/// <summary>
/// Configuration options for performance profiling
/// </summary>
public class PerformanceProfilingOptions
{
    public bool Enabled { get; set; } = false;
    public bool LogAllRequests { get; set; } = false;
    public int SlowRequestThresholdMs { get; set; } = 1000;
    public int MaxRecentMetrics { get; set; } = 1000;
}
