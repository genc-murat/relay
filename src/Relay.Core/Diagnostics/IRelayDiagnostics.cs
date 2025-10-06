using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Diagnostics;

/// <summary>
/// Provides diagnostic and inspection capabilities for Relay
/// </summary>
public interface IRelayDiagnostics
{
    /// <summary>
    /// Gets the current handler registry information
    /// </summary>
    /// <returns>Information about all registered handlers and pipelines</returns>
    HandlerRegistryInfo GetHandlerRegistry();

    /// <summary>
    /// Gets performance metrics for all handlers
    /// </summary>
    /// <returns>Collection of handler performance metrics</returns>
    IEnumerable<HandlerMetrics> GetHandlerMetrics();

    /// <summary>
    /// Gets the current request trace if tracing is enabled
    /// </summary>
    /// <returns>The active request trace, or null if no trace is active</returns>
    RequestTrace? GetCurrentTrace();

    /// <summary>
    /// Gets all completed traces within the specified time window
    /// </summary>
    /// <param name="since">Only return traces newer than this time</param>
    /// <returns>Collection of completed request traces</returns>
    IEnumerable<RequestTrace> GetCompletedTraces(System.DateTimeOffset? since = null);

    /// <summary>
    /// Validates the current Relay configuration
    /// </summary>
    /// <returns>Validation result with any issues found</returns>
    ValidationResult ValidateConfiguration();

    /// <summary>
    /// Runs a performance benchmark for a specific handler
    /// </summary>
    /// <typeparam name="TRequest">The request type to benchmark</typeparam>
    /// <param name="request">The request instance to use for benchmarking</param>
    /// <param name="iterations">Number of iterations to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Benchmark results</returns>
    Task<BenchmarkResult> BenchmarkHandlerAsync<TRequest>(
        TRequest request,
        int iterations = 1000,
        CancellationToken cancellationToken = default)
        where TRequest : Contracts.Requests.IRequest;

    /// <summary>
    /// Clears all diagnostic data (traces, metrics, etc.)
    /// </summary>
    void ClearDiagnosticData();

    /// <summary>
    /// Gets diagnostic statistics summary
    /// </summary>
    /// <returns>Summary of diagnostic data</returns>
    DiagnosticSummary GetDiagnosticSummary();
}