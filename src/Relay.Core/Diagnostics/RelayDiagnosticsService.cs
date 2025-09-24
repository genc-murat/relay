using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public RelayDiagnosticsService(
        IRelayDiagnostics diagnostics,
        IRequestTracer tracer,
        IOptions<DiagnosticsOptions> options)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
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
    public Task<DiagnosticResponse<BenchmarkResult>> RunBenchmark(BenchmarkRequest request)
    {
        if (!_options.EnableDiagnosticEndpoints)
        {
            return Task.FromResult(DiagnosticResponse<BenchmarkResult>.NotFound("Diagnostic endpoints are disabled"));
        }

        if (request == null || string.IsNullOrWhiteSpace(request.RequestType))
        {
            return Task.FromResult(DiagnosticResponse<BenchmarkResult>.BadRequest("Invalid benchmark request"));
        }

        try
        {
            // This would need to be implemented based on the specific request type
            // For now, return a placeholder response
            var result = DiagnosticResponse<BenchmarkResult>.Error("Benchmark functionality not yet implemented", statusCode: 501);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            var result = DiagnosticResponse<BenchmarkResult>.Error("Failed to run benchmark", ex);
            return Task.FromResult(result);
        }
    }
}

/// <summary>
/// Request model for running benchmarks
/// </summary>
public class BenchmarkRequest
{
    /// <summary>
    /// The request type to benchmark
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// Number of iterations to run
    /// </summary>
    public int Iterations { get; set; } = 1000;

    /// <summary>
    /// Optional request data as JSON
    /// </summary>
    public string? RequestData { get; set; }
}