using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// Memory metrics collector implementation
/// </summary>
public class MemoryMetricsCollector : MetricsCollectorBase
{
    public MemoryMetricsCollector(ILogger logger) : base(logger) { }

    public override string Name => "MemoryMetricsCollector";

    public override IReadOnlyCollection<MetricType> SupportedTypes => new[] { MetricType.Gauge };

    public override TimeSpan CollectionInterval => TimeSpan.FromSeconds(5);

    protected override IEnumerable<MetricValue> CollectMetricsCore()
    {
        var (utilization, usedMB, availableMB) = GetMemoryInfo();
        return new[]
        {
            new MetricValue
            {
                Name = "MemoryUtilization",
                Value = utilization,
                Unit = "ratio",
                Type = MetricType.Gauge
            },
            new MetricValue
            {
                Name = "MemoryUsageMB",
                Value = usedMB,
                Unit = "MB",
                Type = MetricType.Gauge
            },
            new MetricValue
            {
                Name = "AvailableMemoryMB",
                Value = availableMB,
                Unit = "MB",
                Type = MetricType.Gauge
            }
        };
    }

    private (double utilization, double usedMB, double availableMB) GetMemoryInfo()
    {
        var process = Process.GetCurrentProcess();
        var usedMB = process.WorkingSet64 / 1024.0 / 1024.0;

        // Estimate available memory (simplified)
        var totalMemoryMB = 8192; // Assume 8GB system
        var availableMB = Math.Max(0, totalMemoryMB - usedMB);
        var utilization = usedMB / totalMemoryMB;

        return (utilization, usedMB, availableMB);
    }
}
