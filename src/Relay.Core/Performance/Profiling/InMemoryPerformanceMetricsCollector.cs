using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Relay.Core.Performance.Profiling;

/// <summary>
/// In-memory performance metrics collector with statistical analysis
/// </summary>
public class InMemoryPerformanceMetricsCollector : IPerformanceMetricsCollector
{
    private readonly ConcurrentDictionary<string, RequestMetricsAggregate> _aggregates;
    private readonly ConcurrentQueue<RequestPerformanceMetrics> _recentMetrics;
    private readonly int _maxRecentMetrics;
    private long _totalRequests;

    public InMemoryPerformanceMetricsCollector(int maxRecentMetrics = 1000)
    {
        _aggregates = new ConcurrentDictionary<string, RequestMetricsAggregate>();
        _recentMetrics = new ConcurrentQueue<RequestPerformanceMetrics>();
        _maxRecentMetrics = maxRecentMetrics;
    }

    public void RecordMetrics(RequestPerformanceMetrics metrics)
    {
        Interlocked.Increment(ref _totalRequests);

        // Update aggregates
        var aggregate = _aggregates.GetOrAdd(metrics.RequestType, _ => new RequestMetricsAggregate());
        aggregate.AddMetrics(metrics);

        // Store recent metrics (with size limit)
        _recentMetrics.Enqueue(metrics);
        while (_recentMetrics.Count > _maxRecentMetrics)
        {
            _recentMetrics.TryDequeue(out _);
        }
    }

    public PerformanceStatistics GetStatistics(string? requestType = null)
    {
        if (requestType != null)
        {
            if (_aggregates.TryGetValue(requestType, out var aggregate))
            {
                return new PerformanceStatistics
                {
                    RequestType = requestType,
                    TotalRequests = aggregate.Count,
                    SuccessfulRequests = aggregate.SuccessCount,
                    FailedRequests = aggregate.FailureCount,
                    AverageExecutionTime = aggregate.GetAverageExecutionTime(),
                    MinExecutionTime = aggregate.MinExecutionTime,
                    MaxExecutionTime = aggregate.MaxExecutionTime,
                    P50ExecutionTime = aggregate.GetPercentile(50),
                    P95ExecutionTime = aggregate.GetPercentile(95),
                    P99ExecutionTime = aggregate.GetPercentile(99),
                    TotalMemoryAllocated = aggregate.TotalMemoryAllocated,
                    AverageMemoryAllocated = aggregate.GetAverageMemoryAllocated(),
                    TotalGen0Collections = aggregate.TotalGen0Collections,
                    TotalGen1Collections = aggregate.TotalGen1Collections,
                    TotalGen2Collections = aggregate.TotalGen2Collections
                };
            }

            return new PerformanceStatistics { RequestType = requestType };
        }

        // Global statistics
        var allMetrics = _recentMetrics.ToArray();
        var totalSuccesses = allMetrics.Count(m => m.Success);

        return new PerformanceStatistics
        {
            RequestType = "All Requests",
            TotalRequests = _totalRequests,
            SuccessfulRequests = totalSuccesses,
            FailedRequests = allMetrics.Length - totalSuccesses,
            AverageExecutionTime = allMetrics.Any() ? TimeSpan.FromMilliseconds(allMetrics.Average(m => m.ExecutionTime.TotalMilliseconds)) : TimeSpan.Zero,
            MinExecutionTime = allMetrics.Any() ? allMetrics.Min(m => m.ExecutionTime) : TimeSpan.Zero,
            MaxExecutionTime = allMetrics.Any() ? allMetrics.Max(m => m.ExecutionTime) : TimeSpan.Zero,
            TotalMemoryAllocated = allMetrics.Sum(m => m.MemoryAllocated)
        };
    }

    public void Reset()
    {
        _aggregates.Clear();
        while (_recentMetrics.TryDequeue(out _)) { }
        Interlocked.Exchange(ref _totalRequests, 0);
    }

    /// <summary>
    /// Thread-safe aggregate for request metrics
    /// </summary>
    private class RequestMetricsAggregate
    {
        private readonly object _lock = new object();
        private readonly ConcurrentBag<TimeSpan> _executionTimes = new ConcurrentBag<TimeSpan>();

        public long Count { get; private set; }
        public long SuccessCount { get; private set; }
        public long FailureCount { get; private set; }
        public TimeSpan MinExecutionTime { get; private set; } = TimeSpan.MaxValue;
        public TimeSpan MaxExecutionTime { get; private set; } = TimeSpan.MinValue;
        public long TotalMemoryAllocated { get; private set; }
        public long TotalGen0Collections { get; private set; }
        public long TotalGen1Collections { get; private set; }
        public long TotalGen2Collections { get; private set; }

        public void AddMetrics(RequestPerformanceMetrics metrics)
        {
            lock (_lock)
            {
                Count++;
                if (metrics.Success)
                    SuccessCount++;
                else
                    FailureCount++;

                _executionTimes.Add(metrics.ExecutionTime);

                if (metrics.ExecutionTime < MinExecutionTime)
                    MinExecutionTime = metrics.ExecutionTime;

                if (metrics.ExecutionTime > MaxExecutionTime)
                    MaxExecutionTime = metrics.ExecutionTime;

                TotalMemoryAllocated += metrics.MemoryAllocated;
                TotalGen0Collections += metrics.Gen0Collections;
                TotalGen1Collections += metrics.Gen1Collections;
                TotalGen2Collections += metrics.Gen2Collections;
            }
        }

        public TimeSpan GetAverageExecutionTime()
        {
            var times = _executionTimes.ToArray();
            return times.Any() ? TimeSpan.FromMilliseconds(times.Average(t => t.TotalMilliseconds)) : TimeSpan.Zero;
        }

        public long GetAverageMemoryAllocated()
        {
            return Count > 0 ? TotalMemoryAllocated / Count : 0;
        }

        public TimeSpan GetPercentile(int percentile)
        {
            var times = _executionTimes.OrderBy(t => t.TotalMilliseconds).ToArray();
            if (times.Length == 0)
                return TimeSpan.Zero;

            var index = (int)Math.Ceiling(percentile / 100.0 * times.Length) - 1;
            return times[Math.Max(0, Math.Min(index, times.Length - 1))];
        }
    }
}
