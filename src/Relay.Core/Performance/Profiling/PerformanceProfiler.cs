using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Performance.Profiling;

/// <summary>
/// Performance profiling helper for detailed request metrics
/// Tracks timing, memory allocations, and throughput
/// Note: Can be used as a pipeline behavior by implementing IPipelineBehavior
/// </summary>
public class PerformanceProfiler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceProfiler<TRequest, TResponse>> _logger;
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly PerformanceProfilingOptions _options;

    public PerformanceProfiler(
        ILogger<PerformanceProfiler<TRequest, TResponse>> logger,
        IPerformanceMetricsCollector metricsCollector,
        PerformanceProfilingOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async ValueTask<TResponse> ProfileAsync(
        TRequest request,
        Func<ValueTask<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return await next();

        var requestType = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(false);
        var initialGen0 = GC.CollectionCount(0);
        var initialGen1 = GC.CollectionCount(1);
        var initialGen2 = GC.CollectionCount(2);

        TResponse response;
        Exception? exception = null;

        try
        {
            response = await next();
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            var metrics = new RequestPerformanceMetrics
            {
                RequestType = requestType,
                ExecutionTime = stopwatch.Elapsed,
                MemoryAllocated = GC.GetTotalMemory(false) - initialMemory,
                Gen0Collections = GC.CollectionCount(0) - initialGen0,
                Gen1Collections = GC.CollectionCount(1) - initialGen1,
                Gen2Collections = GC.CollectionCount(2) - initialGen2,
                Timestamp = DateTimeOffset.UtcNow,
                Success = exception == null
            };

            // Record metrics
            _metricsCollector.RecordMetrics(metrics);

            // Log if threshold exceeded or configured to log all
            if (_options.LogAllRequests || stopwatch.ElapsedMilliseconds > _options.SlowRequestThresholdMs)
            {
                var logLevel = stopwatch.ElapsedMilliseconds > _options.SlowRequestThresholdMs
                    ? LogLevel.Warning
                    : LogLevel.Information;

                _logger.Log(
                    logLevel,
                    "Performance: {RequestType} completed in {Duration}ms, Memory: {Memory:N0} bytes, " +
                    "GC: Gen0={Gen0} Gen1={Gen1} Gen2={Gen2}",
                    requestType,
                    stopwatch.ElapsedMilliseconds,
                    metrics.MemoryAllocated,
                    metrics.Gen0Collections,
                    metrics.Gen1Collections,
                    metrics.Gen2Collections);
            }
        }

        return response!;
    }
}

/// <summary>
/// Collects and aggregates performance metrics
/// </summary>
public interface IPerformanceMetricsCollector
{
    void RecordMetrics(RequestPerformanceMetrics metrics);
    PerformanceStatistics GetStatistics(string? requestType = null);
    void Reset();
}

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

/// <summary>
/// Individual request performance metrics
/// </summary>
public readonly record struct RequestPerformanceMetrics
{
    public string RequestType { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public long MemoryAllocated { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public bool Success { get; init; }
}

/// <summary>
/// Aggregated performance statistics
/// </summary>
public readonly record struct PerformanceStatistics
{
    public string RequestType { get; init; }
    public long TotalRequests { get; init; }
    public long SuccessfulRequests { get; init; }
    public long FailedRequests { get; init; }
    public TimeSpan AverageExecutionTime { get; init; }
    public TimeSpan MinExecutionTime { get; init; }
    public TimeSpan MaxExecutionTime { get; init; }
    public TimeSpan P50ExecutionTime { get; init; }
    public TimeSpan P95ExecutionTime { get; init; }
    public TimeSpan P99ExecutionTime { get; init; }
    public long TotalMemoryAllocated { get; init; }
    public long AverageMemoryAllocated { get; init; }
    public long TotalGen0Collections { get; init; }
    public long TotalGen1Collections { get; init; }
    public long TotalGen2Collections { get; init; }

    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests * 100 : 0;

    public override string ToString()
    {
        return $"{RequestType}: {TotalRequests:N0} requests, " +
               $"Avg: {AverageExecutionTime.TotalMilliseconds:F2}ms, " +
               $"P95: {P95ExecutionTime.TotalMilliseconds:F2}ms, " +
               $"Success: {SuccessRate:F1}%";
    }
}

/// <summary>
/// Configuration options for performance profiling
/// </summary>
public class PerformanceProfilingOptions
{
    public bool Enabled { get; set; } = false;
    public bool LogAllRequests { get; set; } = false;
    public int SlowRequestThresholdMs { get; set; } = 1000;
    public int MaxRecentMetrics { get; set; } = 1000;
}
