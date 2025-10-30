using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// Error metrics collector implementation
/// </summary>
public class ErrorMetricsCollector : MetricsCollectorBase
{
    private long _totalErrors;
    private long _totalExceptions;
    private DateTime _lastErrorReset = DateTime.UtcNow;

    public ErrorMetricsCollector(ILogger logger) : base(logger) { }

    public override string Name => "ErrorMetricsCollector";

    public override IReadOnlyCollection<MetricType> SupportedTypes => new[] { MetricType.Counter, MetricType.Gauge };

    public override TimeSpan CollectionInterval => TimeSpan.FromSeconds(10);

    protected override IEnumerable<MetricValue> CollectMetricsCore()
    {
        var errorRate = GetErrorRate();
        return new[]
        {
            new MetricValue
            {
                Name = "TotalErrors",
                Value = _totalErrors,
                Unit = "count",
                Type = MetricType.Counter
            },
            new MetricValue
            {
                Name = "TotalExceptions",
                Value = _totalExceptions,
                Unit = "count",
                Type = MetricType.Counter
            },
            new MetricValue
            {
                Name = "ErrorRate",
                Value = errorRate,
                Unit = "ratio",
                Type = MetricType.Gauge
            }
        };
    }

    public void RecordError()
    {
        Interlocked.Increment(ref _totalErrors);
    }

    public void RecordException()
    {
        Interlocked.Increment(ref _totalExceptions);
    }

    private double GetErrorRate()
    {
        var now = DateTime.UtcNow;
        var timeElapsed = (now - _lastErrorReset).TotalSeconds;

        if (timeElapsed < 1.0)
        {
            return 0.0; // Simplified - in real implementation would track last rate
        }

        var totalErrors = Interlocked.Read(ref _totalErrors);
        var totalRequests = 100; // This should come from ThroughputMetricsCollector, but for now assume 100

        var errorRate = totalRequests > 0 ? (double)totalErrors / totalRequests : 0.0;
        return Math.Min(errorRate, 1.0);
    }
}
