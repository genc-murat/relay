using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// System load metrics collector implementation
/// </summary>
public class SystemLoadMetricsCollector : MetricsCollectorBase
{
    public SystemLoadMetricsCollector(ILogger logger) : base(logger) { }

    public override string Name => "SystemLoadMetricsCollector";

    public override IReadOnlyCollection<MetricType> SupportedTypes => new[] { MetricType.Gauge };

    public override TimeSpan CollectionInterval => TimeSpan.FromSeconds(30);

    protected override IEnumerable<MetricValue> CollectMetricsCore()
    {
        var loadAverage = GetSystemLoadAverage(0.5, 0.5); // Default values
        var threadCount = GetThreadCount();
        var handleCount = GetHandleCount();

        return new[]
        {
            new MetricValue
            {
                Name = "SystemLoadAverage",
                Value = loadAverage,
                Unit = "load",
                Type = MetricType.Gauge
            },
            new MetricValue
            {
                Name = "ThreadCount",
                Value = threadCount,
                Unit = "count",
                Type = MetricType.Gauge
            },
            new MetricValue
            {
                Name = "HandleCount",
                Value = handleCount,
                Unit = "count",
                Type = MetricType.Gauge
            }
        };
    }

    private double GetSystemLoadAverage(double cpuUtilization, double memoryUtilization)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var threadCount = process.Threads.Count;

            var threadLoad = Math.Min(threadCount / 100.0, 1.0);
            var loadAverage = (cpuUtilization * 0.5) + (memoryUtilization * 0.3) + (threadLoad * 0.2);
            return loadAverage * 10.0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating system load average");
            return 1.0;
        }
    }

    private double GetThreadCount()
    {
        var process = Process.GetCurrentProcess();
        return process.Threads.Count;
    }

    private double GetHandleCount()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            return process.HandleCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting handle count");
            return 0.0;
        }
    }
}