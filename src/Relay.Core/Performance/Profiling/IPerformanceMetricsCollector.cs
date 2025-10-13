namespace Relay.Core.Performance.Profiling;

/// <summary>
/// Collects and aggregates performance metrics
/// </summary>
public interface IPerformanceMetricsCollector
{
    void RecordMetrics(RequestPerformanceMetrics metrics);
    PerformanceStatistics GetStatistics(string? requestType = null);
    void Reset();
}
