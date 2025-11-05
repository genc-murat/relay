using System.Threading;
using System.Threading.Tasks;
using Relay.Core;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics.Core;
using Relay.Core.Diagnostics.Tracing;

namespace Relay.Core.Testing;

/// <summary>
/// Utilities for tracing and diagnostics in test scenarios
/// </summary>
public static class TracingUtilities
{
    /// <summary>
    /// Captures the execution trace for a request
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="relay">The relay instance</param>
    /// <param name="tracer">The request tracer</param>
    /// <param name="request">The request to trace</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response and execution trace</returns>
    public static async Task<(TResponse Response, RequestTrace? Trace)> CaptureTraceAsync<TResponse>(
        IRelay relay,
        IRequestTracer tracer,
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        // Start tracing
        var trace = tracer.StartTrace(request);

        try
        {
            var response = await relay.SendAsync(request, cancellationToken);
            tracer.CompleteTrace(true);
            return (response, trace);
        }
        catch (System.Exception ex)
        {
            tracer.RecordException(ex);
            tracer.CompleteTrace(false);
            throw;
        }
    }

    /// <summary>
    /// Captures the execution trace for a void request
    /// </summary>
    /// <param name="relay">The relay instance</param>
    /// <param name="tracer">The request tracer</param>
    /// <param name="request">The request to trace</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The execution trace</returns>
    public static async Task<RequestTrace?> CaptureTraceAsync(
        IRelay relay,
        IRequestTracer tracer,
        IRequest request,
        CancellationToken cancellationToken = default)
    {
        // Start tracing
        var trace = tracer.StartTrace(request);

        try
        {
            await relay.SendAsync(request, cancellationToken);
            tracer.CompleteTrace(true);
            return trace;
        }
        catch (System.Exception ex)
        {
            tracer.RecordException(ex);
            tracer.CompleteTrace(false);
            throw;
        }
    }
}
