using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// Network metrics collector implementation
/// </summary>
public class NetworkMetricsCollector : MetricsCollectorBase
{
    private readonly Queue<(DateTime timestamp, double latencyMs)> _networkLatencyHistory = new();
    private readonly int _maxNetworkHistorySize = 100;
    private readonly object _metricsLock = new();

    public NetworkMetricsCollector(ILogger logger) : base(logger) { }

    public override string Name => "NetworkMetricsCollector";

    public override IReadOnlyCollection<MetricType> SupportedTypes => new[] { MetricType.Gauge };

    public override TimeSpan CollectionInterval => TimeSpan.FromSeconds(30);

    protected override IEnumerable<MetricValue> CollectMetricsCore()
    {
        var latency = GetNetworkLatency();
        var throughput = GetNetworkThroughput(10); // Assume 10 req/sec for now

        return new[]
        {
            new MetricValue
            {
                Name = "NetworkLatencyMs",
                Value = latency,
                Unit = "milliseconds",
                Type = MetricType.Gauge
            },
            new MetricValue
            {
                Name = "NetworkThroughputMbps",
                Value = throughput,
                Unit = "Mbps",
                Type = MetricType.Gauge
            }
        };
    }

    public void RecordNetworkLatency(double latencyMs)
    {
        lock (_metricsLock)
        {
            _networkLatencyHistory.Enqueue((DateTime.UtcNow, latencyMs));

            while (_networkLatencyHistory.Count > _maxNetworkHistorySize)
            {
                _networkLatencyHistory.Dequeue();
            }
        }
    }

    private double GetNetworkLatency()
    {
        lock (_metricsLock)
        {
            if (_networkLatencyHistory.Count == 0) return 0.0;

            var recentLatencies = _networkLatencyHistory
                .Where(x => (DateTime.UtcNow - x.timestamp).TotalSeconds < 60)
                .Select(x => x.latencyMs)
                .ToList();

            return recentLatencies.Count > 0 ? recentLatencies.Average() : 0.0;
        }
    }

    private double GetNetworkThroughput(double throughputPerSecond)
    {
        // Estimate: Assume each request is ~10KB on average
        var estimatedBytesPerSecond = throughputPerSecond * 10 * 1024;
        var mbps = (estimatedBytesPerSecond * 8) / (1024 * 1024);
        return mbps;
    }
}
