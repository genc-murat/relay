using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Relay.Core.Telemetry;

/// <summary>
/// Decorator for IStreamDispatcher that adds telemetry capabilities
/// </summary>
public class TelemetryStreamDispatcher : IStreamDispatcher
{
    private readonly IStreamDispatcher _inner;
    private readonly ITelemetryProvider _telemetryProvider;

    public TelemetryStreamDispatcher(IStreamDispatcher inner, ITelemetryProvider telemetryProvider)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _telemetryProvider = telemetryProvider ?? throw new ArgumentNullException(nameof(telemetryProvider));
    }

    public IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var correlationId = _telemetryProvider.GetCorrelationId();

        return StreamWithTelemetryAsync(
            _inner.DispatchAsync(request, cancellationToken),
            requestType,
            responseType,
            null,
            correlationId,
            "Relay.Stream");
    }

    public IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var correlationId = _telemetryProvider.GetCorrelationId();

        return StreamWithTelemetryAsync(
            _inner.DispatchAsync(request, handlerName, cancellationToken),
            requestType,
            responseType,
            handlerName,
            correlationId,
            "Relay.NamedStream");
    }

    private async IAsyncEnumerable<TResponse> StreamWithTelemetryAsync<TResponse>(
        IAsyncEnumerable<TResponse> source,
        Type requestType,
        Type responseType,
        string? handlerName,
        string? correlationId,
        string operationName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = _telemetryProvider.StartActivity(operationName, requestType, correlationId);

        var stopwatch = Stopwatch.StartNew();
        var itemCount = 0L;
        Exception? caughtException = null;

        var enumerator = source.GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                try
                {
                    if (!await enumerator.MoveNextAsync())
                        break;
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                    throw;
                }

                itemCount++;
                yield return enumerator.Current;
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
            stopwatch.Stop();
            _telemetryProvider.RecordStreamingOperation(requestType, responseType, handlerName, stopwatch.Elapsed, itemCount, caughtException == null, caughtException);
        }
    }
}