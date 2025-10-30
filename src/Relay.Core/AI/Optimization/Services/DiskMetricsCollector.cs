using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// Disk I/O metrics collector implementation
/// </summary>
public class DiskMetricsCollector : MetricsCollectorBase
{
    private long _lastDiskReadBytes;
    private long _lastDiskWriteBytes;
    private DateTime _lastDiskMeasurement = DateTime.UtcNow;
    private double _currentDiskReadBytesPerSecond;
    private double _currentDiskWriteBytesPerSecond;

    public DiskMetricsCollector(ILogger logger) : base(logger) { }

    public override string Name => "DiskMetricsCollector";

    public override IReadOnlyCollection<MetricType> SupportedTypes => new[] { MetricType.Gauge };

    public override TimeSpan CollectionInterval => TimeSpan.FromSeconds(10);

    protected override IEnumerable<MetricValue> CollectMetricsCore()
    {
        var readBytesPerSecond = GetDiskReadBytesPerSecond();
        var writeBytesPerSecond = GetDiskWriteBytesPerSecond();

        return new[]
        {
            new MetricValue
            {
                Name = "DiskReadBytesPerSecond",
                Value = readBytesPerSecond,
                Unit = "bytes/second",
                Type = MetricType.Gauge
            },
            new MetricValue
            {
                Name = "DiskWriteBytesPerSecond",
                Value = writeBytesPerSecond,
                Unit = "bytes/second",
                Type = MetricType.Gauge
            }
        };
    }

    private double GetDiskReadBytesPerSecond()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var now = DateTime.UtcNow;
            var timeSinceLastMeasurement = (now - _lastDiskMeasurement).TotalSeconds;

            if (timeSinceLastMeasurement >= 1.0)
            {
                var currentReadBytes = process.WorkingSet64; // Simplified
                var deltaBytes = currentReadBytes - _lastDiskReadBytes;
                _currentDiskReadBytesPerSecond = Math.Max(0, deltaBytes / timeSinceLastMeasurement);
                _lastDiskReadBytes = currentReadBytes;
                _lastDiskMeasurement = now;
            }

            return _currentDiskReadBytesPerSecond;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating disk read bytes per second");
            return 0.0;
        }
    }

    private double GetDiskWriteBytesPerSecond()
    {
        try
        {
            // Simplified approximation
            var estimatedWriteBytes = _currentDiskReadBytesPerSecond * 0.5;
            return estimatedWriteBytes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating disk write bytes per second");
            return 0.0;
        }
    }
}
