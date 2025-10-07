using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Telemetry;

/// <summary>
/// Decorator for IRequestDispatcher that adds telemetry capabilities
/// </summary>
public class TelemetryRequestDispatcher : IRequestDispatcher
{
    private readonly IRequestDispatcher _inner;
    private readonly ITelemetryProvider _telemetryProvider;

    public TelemetryRequestDispatcher(IRequestDispatcher inner, ITelemetryProvider telemetryProvider)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _telemetryProvider = telemetryProvider ?? throw new ArgumentNullException(nameof(telemetryProvider));
    }

    public async ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var correlationId = _telemetryProvider.GetCorrelationId();

        using var activity = _telemetryProvider.StartActivity("Relay.Request", requestType, correlationId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _inner.DispatchAsync(request, cancellationToken);
            stopwatch.Stop();

            _telemetryProvider.RecordHandlerExecution(requestType, responseType, null, stopwatch.Elapsed, true);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _telemetryProvider.RecordHandlerExecution(requestType, responseType, null, stopwatch.Elapsed, false, ex);
            throw;
        }
    }

    public async ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var correlationId = _telemetryProvider.GetCorrelationId();

        using var activity = _telemetryProvider.StartActivity("Relay.Request", requestType, correlationId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _inner.DispatchAsync(request, cancellationToken);
            stopwatch.Stop();

            _telemetryProvider.RecordHandlerExecution(requestType, null, null, stopwatch.Elapsed, true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _telemetryProvider.RecordHandlerExecution(requestType, null, null, stopwatch.Elapsed, false, ex);
            throw;
        }
    }

    public async ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var correlationId = _telemetryProvider.GetCorrelationId();

        using var activity = _telemetryProvider.StartActivity("Relay.NamedRequest", requestType, correlationId);
        activity?.SetTag("relay.handler_name", handlerName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _inner.DispatchAsync(request, handlerName, cancellationToken);
            stopwatch.Stop();

            _telemetryProvider.RecordHandlerExecution(requestType, responseType, handlerName, stopwatch.Elapsed, true);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _telemetryProvider.RecordHandlerExecution(requestType, responseType, handlerName, stopwatch.Elapsed, false, ex);
            throw;
        }
    }

    public async ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var correlationId = _telemetryProvider.GetCorrelationId();

        using var activity = _telemetryProvider.StartActivity("Relay.NamedRequest", requestType, correlationId);
        activity?.SetTag("relay.handler_name", handlerName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _inner.DispatchAsync(request, handlerName, cancellationToken);
            stopwatch.Stop();

            _telemetryProvider.RecordHandlerExecution(requestType, null, handlerName, stopwatch.Elapsed, true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _telemetryProvider.RecordHandlerExecution(requestType, null, handlerName, stopwatch.Elapsed, false, ex);
            throw;
        }
    }
}