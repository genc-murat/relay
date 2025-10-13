using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Requests;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
