using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Collects system metrics.
    /// </summary>
    internal class MetricsCollector : IDisposable
    {
        private readonly ILogger _logger;
        private readonly SystemLoadMetricsOptions _options;
        private readonly Process _currentProcess;
        private DateTime _lastCpuCheckTime;
        private TimeSpan _lastCpuTime;
        private double _lastCpuUtilization;
        private readonly ConcurrentQueue<RequestTiming> _requestTimings;
        private long _totalRequests;
        private long _errorCount;

        public MetricsCollector(ILogger logger, SystemLoadMetricsOptions options)
        {
            _logger = logger;
            _options = options;
            _currentProcess = Process.GetCurrentProcess();
            _lastCpuCheckTime = DateTime.UtcNow;
            _lastCpuTime = _currentProcess.TotalProcessorTime;
            _requestTimings = new ConcurrentQueue<RequestTiming>();
        }

        public async ValueTask<SystemLoadMetrics> CollectMetricsAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            var cpuUtilization = GetCpuUtilization();
            var memoryUtilization = GetMemoryUtilization();
            var threadPoolUtilization = GetThreadPoolUtilization();
            var activeRequests = GetActiveRequestCount();
            var queuedRequests = GetQueuedRequestCount();

            var metrics = new SystemLoadMetrics
            {
                CpuUtilization = cpuUtilization,
                MemoryUtilization = memoryUtilization,
                AvailableMemory = GC.GetTotalMemory(false),
                ActiveRequestCount = activeRequests,
                QueuedRequestCount = queuedRequests,
                ThroughputPerSecond = CalculateThroughput(),
                AverageResponseTime = CalculateAverageResponseTime(),
                ErrorRate = CalculateErrorRate(),
                Timestamp = DateTime.UtcNow,
                ActiveConnections = activeRequests,
                DatabasePoolUtilization = 0.0, // Would be injected from DB connection pool
                ThreadPoolUtilization = threadPoolUtilization
            };

            if (_options.EnableDetailedLogging)
            {
                _logger.LogDebug(
                    "System load metrics: CPU={Cpu:P2}, Memory={Memory:P2}, ThreadPool={ThreadPool:P2}, " +
                    "Active={Active}, Queued={Queued}, Throughput={Throughput:F2}/s, AvgResponseTime={AvgResponse}ms",
                    metrics.CpuUtilization,
                    metrics.MemoryUtilization,
                    metrics.ThreadPoolUtilization,
                    metrics.ActiveRequestCount,
                    metrics.QueuedRequestCount,
                    metrics.ThroughputPerSecond,
                    metrics.AverageResponseTime.TotalMilliseconds);
            }

            return metrics;
        }

        private double GetCpuUtilization()
        {
            try
            {
                if (_options.UseCachedCpuMeasurements)
                {
                    // Non-blocking approach using cached measurements
                    var currentTime = DateTime.UtcNow;
                    var currentCpuTime = _currentProcess.TotalProcessorTime;

                    var timeDiff = (currentTime - _lastCpuCheckTime).TotalMilliseconds;

                    if (timeDiff >= _options.CpuMeasurementIntervalMs)
                    {
                        var cpuTimeDiff = (currentCpuTime - _lastCpuTime).TotalMilliseconds;
                        var cpuUsage = cpuTimeDiff / (Environment.ProcessorCount * timeDiff);

                        _lastCpuUtilization = Math.Max(0.0, Math.Min(1.0, cpuUsage));
                        _lastCpuCheckTime = currentTime;
                        _lastCpuTime = currentCpuTime;

                        if (_options.EnableDetailedLogging)
                        {
                            _logger.LogTrace("CPU utilization: {CpuUsage:P2}", _lastCpuUtilization);
                        }
                    }

                    return _lastCpuUtilization;
                }
                else
                {
                    // Blocking measurement with sleep
                    var startTime = DateTime.UtcNow;
                    var startCpuTime = _currentProcess.TotalProcessorTime;

                    Thread.Sleep(_options.CpuMeasurementIntervalMs);

                    var endTime = DateTime.UtcNow;
                    var endCpuTime = _currentProcess.TotalProcessorTime;

                    var cpuUsedMs = (endCpuTime - startCpuTime).TotalMilliseconds;
                    var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                    var cpuUsage = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                    return Math.Max(0.0, Math.Min(1.0, cpuUsage));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get CPU utilization");
                return GetThreadPoolUtilization() * 0.8;
            }
        }

        private double GetMemoryUtilization()
        {
            try
            {
                var totalMemory = GC.GetTotalMemory(false);
                return Math.Min(1.0, (double)totalMemory / _options.BaselineMemory);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get memory utilization");
                return 0.5;
            }
        }

        private double GetThreadPoolUtilization()
        {
            try
            {
                ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
                ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);

                return 1.0 - ((double)workerThreads / maxWorkerThreads);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get thread pool utilization");
                return 0.3;
            }
        }

        private int GetActiveRequestCount()
        {
            if (_options.ActiveRequestCounter != null)
            {
                return _options.ActiveRequestCounter.GetActiveRequestCount();
            }

            // Fallback: estimate from thread pool
            ThreadPool.GetAvailableThreads(out var workerThreads, out _);
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out _);
            return maxWorkerThreads - workerThreads;
        }

        private int GetQueuedRequestCount()
        {
            if (_options.ActiveRequestCounter != null)
            {
                return _options.ActiveRequestCounter.GetQueuedRequestCount();
            }

            return 0;
        }

        private double CalculateThroughput()
        {
            // Clean old entries
            while (_requestTimings.TryPeek(out var timing) &&
                   (DateTime.UtcNow - timing.Timestamp).TotalSeconds > 60)
            {
                _requestTimings.TryDequeue(out _);
            }

            var recentRequests = _requestTimings.Count;
            return recentRequests / 60.0; // Requests per second over last minute
        }

        private TimeSpan CalculateAverageResponseTime()
        {
            if (_requestTimings.IsEmpty)
                return TimeSpan.FromMilliseconds(100);

            var timings = _requestTimings.ToArray();
            var average = timings.Average(t => t.Duration.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(average);
        }

        private double CalculateErrorRate()
        {
            var total = Interlocked.Read(ref _totalRequests);
            var errors = Interlocked.Read(ref _errorCount);

            return total > 0 ? (double)errors / total : 0.0;
        }

        public void RecordRequest(TimeSpan duration, bool success)
        {
            _requestTimings.Enqueue(new RequestTiming
            {
                Timestamp = DateTime.UtcNow,
                Duration = duration
            });

            Interlocked.Increment(ref _totalRequests);
            if (!success)
            {
                Interlocked.Increment(ref _errorCount);
            }
        }

        public void Dispose()
        {
            _currentProcess?.Dispose();
        }

        private class RequestTiming
        {
            public DateTime Timestamp { get; set; }
            public TimeSpan Duration { get; set; }
        }
    }
}