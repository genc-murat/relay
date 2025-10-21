using Relay.Core.AI;

namespace Relay.Core.Diagnostics.Metrics;

/// <summary>
/// Request model for running benchmarks
/// </summary>
[AIMonitored(Level = MonitoringLevel.Detailed, CollectDetailedMetrics = true, Tags = new[] { "benchmark", "performance" })]
public class BenchmarkRequest
{
    /// <summary>
    /// The request type to benchmark
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// Number of iterations to run
    /// </summary>
    public int Iterations { get; set; } = 1000;

    /// <summary>
    /// Optional request data as JSON
    /// </summary>
    public string? RequestData { get; set; }
}