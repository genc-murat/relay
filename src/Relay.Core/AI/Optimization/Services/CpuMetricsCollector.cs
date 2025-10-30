using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// CPU metrics collector implementation
/// </summary>
public class CpuMetricsCollector : MetricsCollectorBase
{
    private readonly List<double> _cpuUtilizationHistory = new();
    private const int MaxCpuHistorySize = 60;
    private readonly object _cpuLock = new();
    private DateTime _lastCpuMeasurementTime = DateTime.UtcNow;
    private TimeSpan _lastProcessorTime = TimeSpan.Zero;

    public CpuMetricsCollector(ILogger logger) : base(logger) { }

    public override string Name => "CpuMetricsCollector";

    public override IReadOnlyCollection<MetricType> SupportedTypes => new[] { MetricType.Gauge };

    public override TimeSpan CollectionInterval => TimeSpan.FromSeconds(1);

    protected override IEnumerable<MetricValue> CollectMetricsCore()
    {
        var utilization = GetCpuUtilization();
        return new[]
        {
            new MetricValue
            {
                Name = "CpuUtilization",
                Value = utilization,
                Unit = "ratio",
                Type = MetricType.Gauge
            },
            new MetricValue
            {
                Name = "CpuUsagePercent",
                Value = utilization * 100,
                Unit = "percent",
                Type = MetricType.Gauge
            }
        };
    }

    private double GetCpuUtilization()
    {
        lock (_cpuLock)
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var now = DateTime.UtcNow;
                var currentProcessorTime = process.TotalProcessorTime;

                // Strategy 1: Calculate incremental CPU usage since last measurement
                var timeSinceLastMeasurement = now - _lastCpuMeasurementTime;
                var processorTimeDelta = currentProcessorTime - _lastProcessorTime;

                double currentCpuUtilization = 0.0;

                // Ensure meaningful time has passed (at least 100ms)
                if (timeSinceLastMeasurement.TotalMilliseconds >= 100)
                {
                    // CPU time used / (elapsed time * processor count)
                    var elapsedMs = timeSinceLastMeasurement.TotalMilliseconds;
                    var processorTimeMs = processorTimeDelta.TotalMilliseconds;
                    var processorCount = Environment.ProcessorCount;

                    // Calculate utilization: actual CPU time / (wallclock time * number of cores)
                    currentCpuUtilization = processorTimeMs / (elapsedMs * processorCount);

                    // Update tracking variables
                    _lastCpuMeasurementTime = now;
                    _lastProcessorTime = currentProcessorTime;
                }
                else
                {
                    // Not enough time passed, use last known value
                    if (_cpuUtilizationHistory.Count > 0)
                    {
                        currentCpuUtilization = _cpuUtilizationHistory.Last();
                    }
                }

                // Clamp to valid range [0, 1]
                currentCpuUtilization = Math.Clamp(currentCpuUtilization, 0.0, 1.0);

                // Add to history for trend analysis
                _cpuUtilizationHistory.Add(currentCpuUtilization);
                if (_cpuUtilizationHistory.Count > MaxCpuHistorySize)
                {
                    _cpuUtilizationHistory.RemoveAt(0);
                }

                return currentCpuUtilization;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating CPU utilization");
                return _cpuUtilizationHistory.Count > 0 ? _cpuUtilizationHistory.Last() : 0.0;
            }
        }
    }

    // Additional methods for CPU analysis
    public double GetAverageCpuUtilization()
    {
        lock (_cpuLock)
        {
            return _cpuUtilizationHistory.Count > 0 ? _cpuUtilizationHistory.Average() : 0.0;
        }
    }

    public double GetMaxCpuUtilization()
    {
        lock (_cpuLock)
        {
            return _cpuUtilizationHistory.Count > 0 ? _cpuUtilizationHistory.Max() : 0.0;
        }
    }

    public double GetMinCpuUtilization()
    {
        lock (_cpuLock)
        {
            return _cpuUtilizationHistory.Count > 0 ? _cpuUtilizationHistory.Min() : 0.0;
        }
    }

    public double GetCpuUtilizationStdDev()
    {
        lock (_cpuLock)
        {
            if (_cpuUtilizationHistory.Count < 2) return 0.0;
            var average = _cpuUtilizationHistory.Average();
            var variance = _cpuUtilizationHistory.Sum(x => Math.Pow(x - average, 2)) / _cpuUtilizationHistory.Count;
            return Math.Sqrt(variance);
        }
    }
}
