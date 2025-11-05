using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// Throughput metrics collector implementation
/// </summary>
public class ThroughputMetricsCollector : MetricsCollectorBase
{
    private long _totalRequestsProcessed;
    private DateTime _lastThroughputReset = DateTime.UtcNow;
    private double _currentThroughputPerSecond;

    public ThroughputMetricsCollector(ILogger logger) : base(logger) { }

    public override string Name => "ThroughputMetricsCollector";

    public override IReadOnlyCollection<MetricType> SupportedTypes => new[] { MetricType.Counter, MetricType.Gauge };

    public override TimeSpan CollectionInterval => TimeSpan.FromSeconds(1);

    protected override IEnumerable<MetricValue> CollectMetricsCore()
    {
        var throughput = GetThroughputPerSecond();
        return new[]
        {
            new MetricValue
            {
                Name = "TotalRequestsProcessed",
                Value = _totalRequestsProcessed,
                Unit = "count",
                Type = MetricType.Counter
            },
            new MetricValue
            {
                Name = "ThroughputPerSecond",
                Value = throughput,
                Unit = "requests/second",
                Type = MetricType.Gauge
            },
            new MetricValue
            {
                Name = "RequestsPerSecond",
                Value = throughput,
                Unit = "requests/second",
                Type = MetricType.Gauge
            }
        };
    }

    public void RecordRequestProcessed()
    {
        Interlocked.Increment(ref _totalRequestsProcessed);
    }

    private double GetThroughputPerSecond()
    {
        var now = DateTime.UtcNow;
        var timeElapsed = (now - _lastThroughputReset).TotalSeconds;

        if (timeElapsed >= 1.0) // Update every second
        {
            var requestsInPeriod = Interlocked.Read(ref _totalRequestsProcessed);
            _currentThroughputPerSecond = requestsInPeriod / timeElapsed;

            // Reset for next period
            Interlocked.Exchange(ref _totalRequestsProcessed, 0);
            _lastThroughputReset = now;
        }

        return _currentThroughputPerSecond;
    }
}
