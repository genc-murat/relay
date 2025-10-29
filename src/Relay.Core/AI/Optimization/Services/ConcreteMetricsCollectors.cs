using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization.Services
{
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
}