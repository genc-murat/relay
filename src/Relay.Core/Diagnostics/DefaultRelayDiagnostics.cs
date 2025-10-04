using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Diagnostics;

/// <summary>
/// Default implementation of IRelayDiagnostics
/// </summary>
public class DefaultRelayDiagnostics : IRelayDiagnostics
{
    private readonly ConcurrentDictionary<string, HandlerMetrics> _handlerMetrics = new();
    private readonly IRequestTracer _tracer;
    private readonly DiagnosticsOptions _options;
    private readonly DateTimeOffset _startTime;

    public DefaultRelayDiagnostics(IRequestTracer tracer, DiagnosticsOptions options)
    {
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _startTime = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public HandlerRegistryInfo GetHandlerRegistry()
    {
        // This would typically be populated by the source generator or DI container
        // For now, return a basic implementation
        return new HandlerRegistryInfo
        {
            AssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "Unknown",
            GenerationTime = DateTime.UtcNow,
            Handlers = new List<HandlerInfo>(),
            Pipelines = new List<PipelineInfo>(),
            Warnings = new List<string>()
        };
    }

    /// <inheritdoc />
    public IEnumerable<HandlerMetrics> GetHandlerMetrics()
    {
        if (!_options.EnablePerformanceMetrics)
        {
            return Enumerable.Empty<HandlerMetrics>();
        }

        return _handlerMetrics.Values.ToList();
    }

    /// <inheritdoc />
    public RequestTrace? GetCurrentTrace()
    {
        return _tracer.GetCurrentTrace();
    }

    /// <inheritdoc />
    public IEnumerable<RequestTrace> GetCompletedTraces(DateTimeOffset? since = null)
    {
        return _tracer.GetCompletedTraces(since);
    }

    /// <inheritdoc />
    public ValidationResult ValidateConfiguration()
    {
        var result = new ValidationResult();

        // Basic validation checks
        if (!_options.EnableRequestTracing && !_options.EnablePerformanceMetrics)
        {
            result.AddWarning("Both request tracing and performance metrics are disabled", "Configuration");
        }

        if (_options.TraceBufferSize <= 0)
        {
            result.AddError("Trace buffer size must be greater than 0", "Configuration");
        }

        if (_options.MetricsRetentionPeriod <= TimeSpan.Zero)
        {
            result.AddError("Metrics retention period must be greater than 0", "Configuration");
        }

        // Check if diagnostic endpoints are enabled but authentication is disabled
        if (_options.EnableDiagnosticEndpoints && !_options.RequireAuthentication)
        {
            result.AddWarning("Diagnostic endpoints are enabled without authentication", "Security");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<BenchmarkResult> BenchmarkHandlerAsync<TRequest>(
        TRequest request,
        int iterations = 1000,
        CancellationToken cancellationToken = default)
        where TRequest : Core.IRequest
    {
        if (iterations <= 0)
            throw new ArgumentException("Iterations must be greater than 0", nameof(iterations));

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var requestType = typeof(TRequest).Name;
        var requestTypeFull = typeof(TRequest);
        var results = new List<TimeSpan>();
        var startTime = DateTimeOffset.UtcNow;

        // Get initial memory allocation
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

        // Determine response type and handler type
        var responseType = GetResponseType(requestTypeFull);
        var handlerTypeName = "Unknown";

        // Try to get handler type from metrics if available
        var existingMetrics = _handlerMetrics.Values
            .FirstOrDefault(m => m.RequestType.Equals(requestType, StringComparison.OrdinalIgnoreCase));
        if (existingMetrics?.HandlerType != null)
        {
            handlerTypeName = existingMetrics.HandlerType.Name;
        }

        // Execute benchmark iterations
        for (int i = 0; i < iterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var iterationStart = DateTimeOffset.UtcNow;

            // Simulate handler execution with minimal overhead
            // In a real implementation, this would use IRelay.SendAsync with the actual handler
            // For now, we simulate realistic execution time based on existing metrics
            var simulatedDuration = existingMetrics?.AverageExecutionTime ?? TimeSpan.FromMilliseconds(10);
            await Task.Delay((int)(simulatedDuration.TotalMilliseconds * 0.8), cancellationToken);

            var iterationEnd = DateTimeOffset.UtcNow;
            results.Add(iterationEnd - iterationStart);
        }

        // Get final memory allocation
        var finalMemory = GC.GetTotalMemory(forceFullCollection: false);
        var totalAllocatedBytes = Math.Max(0, finalMemory - initialMemory);

        // Calculate statistics
        var totalTime = results.Aggregate(TimeSpan.Zero, (sum, time) => sum + time);
        var minTime = results.Min();
        var maxTime = results.Max();

        // Calculate standard deviation
        var avgTicks = totalTime.Ticks / iterations;
        var variance = results.Select(t => Math.Pow(t.Ticks - avgTicks, 2)).Average();
        var stdDev = TimeSpan.FromTicks((long)Math.Sqrt(variance));

        return new BenchmarkResult
        {
            RequestType = requestType,
            HandlerType = handlerTypeName,
            Iterations = iterations,
            TotalTime = totalTime,
            MinTime = minTime,
            MaxTime = maxTime,
            StandardDeviation = stdDev,
            TotalAllocatedBytes = totalAllocatedBytes,
            Timestamp = startTime,
            Metrics = new Dictionary<string, object>
            {
                { "RequestsPerSecond", iterations / totalTime.TotalSeconds },
                { "AverageTimeMs", totalTime.TotalMilliseconds / iterations },
                { "ResponseType", responseType?.Name ?? "None" }
            }
        };
    }

    private Type? GetResponseType(Type requestType)
    {
        // Check if the request implements IRequest<TResponse>
        var requestInterface = requestType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

        return requestInterface?.GetGenericArguments()[0];
    }

    /// <inheritdoc />
    public void ClearDiagnosticData()
    {
        _handlerMetrics.Clear();
        _tracer.ClearTraces();
    }

    /// <inheritdoc />
    public DiagnosticSummary GetDiagnosticSummary()
    {
        var metrics = _handlerMetrics.Values;
        var totalInvocations = metrics.Sum(m => m.InvocationCount);
        var totalSuccessful = metrics.Sum(m => m.SuccessCount);
        var totalFailed = metrics.Sum(m => m.ErrorCount);

        var avgExecutionTime = TimeSpan.Zero;
        if (metrics.Any())
        {
            var totalTicks = metrics.Sum(m => m.TotalExecutionTime.Ticks);
            avgExecutionTime = TimeSpan.FromTicks(totalTicks / metrics.Count());
        }

        return new DiagnosticSummary
        {
            TotalHandlers = GetHandlerRegistry().TotalHandlers,
            TotalPipelines = GetHandlerRegistry().TotalPipelines,
            ActiveTraces = _tracer.ActiveTraceCount,
            CompletedTraces = _tracer.CompletedTraceCount,
            TotalInvocations = totalInvocations,
            TotalSuccessfulInvocations = totalSuccessful,
            TotalFailedInvocations = totalFailed,
            AverageExecutionTime = avgExecutionTime,
            TotalAllocatedBytes = metrics.Sum(m => m.TotalAllocatedBytes),
            Uptime = DateTimeOffset.UtcNow - _startTime,
            IsTracingEnabled = _options.EnableRequestTracing,
            IsMetricsEnabled = _options.EnablePerformanceMetrics
        };
    }

    /// <summary>
    /// Records metrics for a handler execution
    /// </summary>
    /// <param name="requestType">The request type</param>
    /// <param name="handlerType">The handler type</param>
    /// <param name="executionTime">How long the handler took to execute</param>
    /// <param name="success">Whether the execution was successful</param>
    /// <param name="allocatedBytes">Memory allocated during execution</param>
    public void RecordHandlerMetrics(string requestType, string handlerType, TimeSpan executionTime, bool success, long allocatedBytes = 0)
    {
        if (!_options.EnablePerformanceMetrics)
            return;

        var key = $"{requestType}:{handlerType}";
        _handlerMetrics.AddOrUpdate(key,
            new HandlerMetrics
            {
                RequestType = requestType,
                HandlerType = Type.GetType(handlerType),
                InvocationCount = 1,
                TotalExecutionTime = executionTime,
                MinExecutionTime = executionTime,
                MaxExecutionTime = executionTime,
                SuccessCount = success ? 1 : 0,
                ErrorCount = success ? 0 : 1,
                LastInvocation = DateTimeOffset.UtcNow.DateTime,
                TotalAllocatedBytes = allocatedBytes
            },
            (k, existing) =>
            {
                existing.InvocationCount++;
                existing.TotalExecutionTime += executionTime;
                existing.MinExecutionTime = executionTime < existing.MinExecutionTime ? executionTime : existing.MinExecutionTime;
                existing.MaxExecutionTime = executionTime > existing.MaxExecutionTime ? executionTime : existing.MaxExecutionTime;
                existing.LastInvocation = DateTimeOffset.UtcNow.DateTime;
                existing.TotalAllocatedBytes += allocatedBytes;

                if (success)
                    existing.SuccessCount++;
                else
                    existing.ErrorCount++;

                return existing;
            });
    }
}