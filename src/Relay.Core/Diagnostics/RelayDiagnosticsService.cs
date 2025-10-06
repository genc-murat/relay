using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Relay.Core.Diagnostics;

/// <summary>
/// Service providing diagnostic endpoints for Relay runtime inspection
/// This can be used directly or wrapped in web framework controllers
/// </summary>
public class RelayDiagnosticsService
{
    private readonly IRelayDiagnostics _diagnostics;
    private readonly IRequestTracer _tracer;
    private readonly DiagnosticsOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public RelayDiagnosticsService(
        IRelayDiagnostics diagnostics,
        IRequestTracer tracer,
        IOptions<DiagnosticsOptions> options,
        IServiceProvider serviceProvider)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets information about all registered handlers
    /// </summary>
    /// <returns>Handler registry information</returns>
    public DiagnosticResponse<HandlerRegistryInfo> GetHandlers()
    {
        if (!_options.EnableDiagnosticEndpoints)
        {
            return DiagnosticResponse<HandlerRegistryInfo>.NotFound("Diagnostic endpoints are disabled");
        }

        try
        {
            var registry = _diagnostics.GetHandlerRegistry();
            return DiagnosticResponse<HandlerRegistryInfo>.Success(registry);
        }
        catch (Exception ex)
        {
            return DiagnosticResponse<HandlerRegistryInfo>.Error("Failed to retrieve handler registry", ex);
        }
    }

    /// <summary>
    /// Gets performance metrics for all handlers
    /// </summary>
    /// <returns>Handler performance metrics</returns>
    public DiagnosticResponse<IEnumerable<HandlerMetrics>> GetMetrics()
    {
        if (!_options.EnableDiagnosticEndpoints)
        {
            return DiagnosticResponse<IEnumerable<HandlerMetrics>>.NotFound("Diagnostic endpoints are disabled");
        }

        try
        {
            var metrics = _diagnostics.GetHandlerMetrics();
            return DiagnosticResponse<IEnumerable<HandlerMetrics>>.Success(metrics);
        }
        catch (Exception ex)
        {
            return DiagnosticResponse<IEnumerable<HandlerMetrics>>.Error("Failed to retrieve handler metrics", ex);
        }
    }

    /// <summary>
    /// Gets performance metrics for a specific handler
    /// </summary>
    /// <param name="requestType">The request type to get metrics for</param>
    /// <returns>Handler performance metrics</returns>
    public DiagnosticResponse<HandlerMetrics> GetHandlerMetrics(string requestType)
    {
        if (!_options.EnableDiagnosticEndpoints)
        {
            return DiagnosticResponse<HandlerMetrics>.NotFound("Diagnostic endpoints are disabled");
        }

        try
        {
            var metrics = _diagnostics.GetHandlerMetrics()
                .FirstOrDefault(m => m.RequestType.Equals(requestType, StringComparison.OrdinalIgnoreCase));

            if (metrics == null)
            {
                return DiagnosticResponse<HandlerMetrics>.NotFound($"No metrics found for request type: {requestType}");
            }

            return DiagnosticResponse<HandlerMetrics>.Success(metrics);
        }
        catch (Exception ex)
        {
            return DiagnosticResponse<HandlerMetrics>.Error("Failed to retrieve handler metrics", ex);
        }
    }

    /// <summary>
    /// Validates the current Relay configuration
    /// </summary>
    /// <returns>Configuration validation results</returns>
    public DiagnosticResponse<ValidationResult> GetHealth()
    {
        if (!_options.EnableDiagnosticEndpoints)
        {
            return DiagnosticResponse<ValidationResult>.NotFound("Diagnostic endpoints are disabled");
        }

        try
        {
            var validation = _diagnostics.ValidateConfiguration();

            // Return appropriate status based on validation result
            if (validation.IsValid)
            {
                return DiagnosticResponse<ValidationResult>.Success(validation);
            }
            else if (validation.ErrorCount > 0)
            {
                return DiagnosticResponse<ValidationResult>.ServiceUnavailable("Configuration validation failed");
            }
            else
            {
                return DiagnosticResponse<ValidationResult>.Success(validation); // OK but with warnings
            }
        }
        catch (Exception ex)
        {
            return DiagnosticResponse<ValidationResult>.Error("Failed to validate configuration", ex);
        }
    }

    /// <summary>
    /// Gets diagnostic summary information
    /// </summary>
    /// <returns>Diagnostic summary</returns>
    public DiagnosticResponse<DiagnosticSummary> GetSummary()
    {
        if (!_options.EnableDiagnosticEndpoints)
        {
            return DiagnosticResponse<DiagnosticSummary>.NotFound("Diagnostic endpoints are disabled");
        }

        try
        {
            var summary = _diagnostics.GetDiagnosticSummary();
            return DiagnosticResponse<DiagnosticSummary>.Success(summary);
        }
        catch (Exception ex)
        {
            return DiagnosticResponse<DiagnosticSummary>.Error("Failed to retrieve diagnostic summary", ex);
        }
    }

    /// <summary>
    /// Gets request traces
    /// </summary>
    /// <param name="since">Optional timestamp to filter traces</param>
    /// <param name="format">Output format (json or text)</param>
    /// <returns>Request traces</returns>
    public DiagnosticResponse<object> GetTraces(DateTimeOffset? since = null, string format = "json")
    {
        if (!_options.EnableDiagnosticEndpoints)
        {
            return DiagnosticResponse<object>.NotFound("Diagnostic endpoints are disabled");
        }

        if (!_options.EnableRequestTracing)
        {
            return DiagnosticResponse<object>.BadRequest("Request tracing is disabled");
        }

        try
        {
            var traces = _tracer.GetCompletedTraces(since);

            if (format.Equals("text", StringComparison.OrdinalIgnoreCase))
            {
                var textOutput = TraceFormatter.FormatTraceSummary(traces);
                return DiagnosticResponse<object>.Success(new { content = textOutput, contentType = "text/plain" });
            }

            return DiagnosticResponse<object>.Success(traces);
        }
        catch (Exception ex)
        {
            return DiagnosticResponse<object>.Error("Failed to retrieve traces", ex);
        }
    }

    /// <summary>
    /// Gets a specific request trace by ID
    /// </summary>
    /// <param name="traceId">The trace ID</param>
    /// <param name="format">Output format (json or text)</param>
    /// <returns>Request trace</returns>
    public DiagnosticResponse<object> GetTrace(Guid traceId, string format = "json")
    {
        if (!_options.EnableDiagnosticEndpoints)
        {
            return DiagnosticResponse<object>.NotFound("Diagnostic endpoints are disabled");
        }

        if (!_options.EnableRequestTracing)
        {
            return DiagnosticResponse<object>.BadRequest("Request tracing is disabled");
        }

        try
        {
            var trace = _tracer.GetCompletedTraces()
                .FirstOrDefault(t => t.RequestId == traceId);

            if (trace == null)
            {
                return DiagnosticResponse<object>.NotFound($"Trace not found: {traceId}");
            }

            if (format.Equals("text", StringComparison.OrdinalIgnoreCase))
            {
                var textOutput = TraceFormatter.FormatTrace(trace, includeMetadata: true);
                return DiagnosticResponse<object>.Success(new { content = textOutput, contentType = "text/plain" });
            }

            return DiagnosticResponse<object>.Success(trace);
        }
        catch (Exception ex)
        {
            return DiagnosticResponse<object>.Error("Failed to retrieve trace", ex);
        }
    }

    /// <summary>
    /// Clears all diagnostic data
    /// </summary>
    /// <returns>Success message</returns>
    public DiagnosticResponse<object> ClearDiagnosticData()
    {
        if (!_options.EnableDiagnosticEndpoints)
        {
            return DiagnosticResponse<object>.NotFound("Diagnostic endpoints are disabled");
        }

        try
        {
            _diagnostics.ClearDiagnosticData();
            return DiagnosticResponse<object>.Success(new { message = "Diagnostic data cleared successfully" });
        }
        catch (Exception ex)
        {
            return DiagnosticResponse<object>.Error("Failed to clear diagnostic data", ex);
        }
    }

    /// <summary>
    /// Runs a performance benchmark for a specific request type
    /// </summary>
    /// <param name="request">Benchmark request parameters</param>
    /// <returns>Benchmark results</returns>
    public async Task<DiagnosticResponse<BenchmarkResult>> RunBenchmark(BenchmarkRequest request)
    {
        if (!_options.EnableDiagnosticEndpoints)
        {
            return DiagnosticResponse<BenchmarkResult>.NotFound("Diagnostic endpoints are disabled");
        }

        if (request == null || string.IsNullOrWhiteSpace(request.RequestType))
        {
            return DiagnosticResponse<BenchmarkResult>.BadRequest("Invalid benchmark request");
        }

        if (request.Iterations <= 0)
        {
            return DiagnosticResponse<BenchmarkResult>.BadRequest("Iterations must be greater than 0");
        }

        try
        {
            // Try to resolve the request type
            var requestType = FindRequestType(request.RequestType);
            if (requestType == null)
            {
                return DiagnosticResponse<BenchmarkResult>.NotFound($"Request type not found: {request.RequestType}");
            }

            // Create request instance
            var requestInstance = CreateRequestInstance(requestType, request.RequestData);
            if (requestInstance == null)
            {
                return DiagnosticResponse<BenchmarkResult>.BadRequest($"Failed to create instance of request type: {request.RequestType}");
            }

            // Get the IRelay instance
            var relay = _serviceProvider.GetService<IRelay>();
            if (relay == null)
            {
                return DiagnosticResponse<BenchmarkResult>.Error("IRelay service not available");
            }

            // Run the benchmark
            var benchmarkResult = await RunBenchmarkInternal(relay, requestInstance, requestType, request.Iterations);
            
            return DiagnosticResponse<BenchmarkResult>.Success(benchmarkResult);
        }
        catch (Exception ex)
        {
            return DiagnosticResponse<BenchmarkResult>.Error("Failed to run benchmark", ex);
        }
    }

    private Type? FindRequestType(string requestTypeName)
    {
        // Try to find the type in all loaded assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();
                var requestType = types.FirstOrDefault(t => 
                    t.Name.Equals(requestTypeName, StringComparison.OrdinalIgnoreCase) ||
                    t.FullName?.Equals(requestTypeName, StringComparison.OrdinalIgnoreCase) == true);

                if (requestType != null)
                {
                    // Verify it implements IRequest
                    var isRequest = requestType.GetInterfaces().Any(i => 
                        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>) ||
                        i == typeof(IRequest));

                    if (isRequest)
                    {
                        return requestType;
                    }
                }
            }
            catch
            {
                // Skip assemblies that can't be loaded
                continue;
            }
        }

        return null;
    }

    private object? CreateRequestInstance(Type requestType, string? requestData)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(requestData))
            {
                // Try to deserialize from JSON
                var instance = JsonSerializer.Deserialize(requestData, requestType, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return instance;
            }

            // Try to create a default instance
            return Activator.CreateInstance(requestType);
        }
        catch
        {
            return null;
        }
    }

    private async Task<BenchmarkResult> RunBenchmarkInternal(IRelay relay, object request, Type requestType, int iterations)
    {
        var results = new List<TimeSpan>();
        var startTime = DateTimeOffset.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Get initial memory
        var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

        // Determine handler type by checking what interface the request implements
        var requestInterfaces = requestType.GetInterfaces();
        var responseType = requestInterfaces
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
            ?.GetGenericArguments()[0];

        var hasResponse = responseType != null;
        var handlerTypeName = "Unknown";

        // Warm-up iteration
        try
        {
            if (hasResponse)
            {
                var sendMethod = typeof(IRelay).GetMethod(nameof(IRelay.SendAsync), new[] { requestType, typeof(CancellationToken) });
                if (sendMethod != null)
                {
                    var genericMethod = sendMethod.MakeGenericMethod(responseType!);
                    await (dynamic)genericMethod.Invoke(relay, new[] { request, cancellationToken })!;
                }
            }
            else
            {
                await relay.SendAsync((IRequest)request, cancellationToken);
            }
        }
        catch
        {
            // Ignore warm-up errors
        }

        // Run benchmark iterations
        for (int i = 0; i < iterations; i++)
        {
            var iterationStart = DateTimeOffset.UtcNow;

            try
            {
                if (hasResponse)
                {
                    var sendMethod = typeof(IRelay).GetMethod(nameof(IRelay.SendAsync), new[] { requestType, typeof(CancellationToken) });
                    if (sendMethod != null)
                    {
                        var genericMethod = sendMethod.MakeGenericMethod(responseType!);
                        await (dynamic)genericMethod.Invoke(relay, new[] { request, cancellationToken })!;
                    }
                }
                else
                {
                    await relay.SendAsync((IRequest)request, cancellationToken);
                }
            }
            catch
            {
                // Continue even if some iterations fail
            }

            var iterationEnd = DateTimeOffset.UtcNow;
            results.Add(iterationEnd - iterationStart);
        }

        // Get final memory
        var finalMemory = GC.GetTotalMemory(forceFullCollection: false);
        var totalAllocated = Math.Max(0, finalMemory - initialMemory);

        // Calculate statistics
        var totalTime = results.Aggregate(TimeSpan.Zero, (sum, time) => sum + time);
        var minTime = results.Min();
        var maxTime = results.Max();

        // Calculate standard deviation
        var avgTicks = totalTime.Ticks / iterations;
        var variance = results.Select(t => Math.Pow(t.Ticks - avgTicks, 2)).Average();
        var stdDev = TimeSpan.FromTicks((long)Math.Sqrt(variance));

        // Try to get handler type from metrics
        var metrics = _diagnostics.GetHandlerMetrics()
            .FirstOrDefault(m => m.RequestType.Equals(requestType.Name, StringComparison.OrdinalIgnoreCase));
        
        if (metrics?.HandlerType != null)
        {
            handlerTypeName = metrics.HandlerType.Name;
        }

        return new BenchmarkResult
        {
            RequestType = requestType.Name,
            HandlerType = handlerTypeName,
            Iterations = iterations,
            TotalTime = totalTime,
            MinTime = minTime,
            MaxTime = maxTime,
            StandardDeviation = stdDev,
            TotalAllocatedBytes = totalAllocated,
            Timestamp = startTime,
            Metrics = new Dictionary<string, object>
            {
                { "SuccessfulIterations", results.Count },
                { "FailedIterations", iterations - results.Count },
                { "RequestsPerSecond", iterations / totalTime.TotalSeconds }
            }
        };
    }
}
