using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Telemetry;

/// <summary>
/// Decorator for IRelay that adds telemetry capabilities
/// </summary>
public class TelemetryRelay : IRelay
{
    private readonly IRelay _inner;
    private readonly ITelemetryProvider _telemetryProvider;

    public TelemetryRelay(IRelay inner, ITelemetryProvider telemetryProvider)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _telemetryProvider = telemetryProvider ?? throw new ArgumentNullException(nameof(telemetryProvider));
    }

    public async ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var correlationId = _telemetryProvider.GetCorrelationId();

        using var activity = _telemetryProvider.StartActivity("Relay.Send", requestType, correlationId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _inner.SendAsync(request, cancellationToken);
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

    public async ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var correlationId = _telemetryProvider.GetCorrelationId();

        using var activity = _telemetryProvider.StartActivity("Relay.Send", requestType, correlationId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _inner.SendAsync(request, cancellationToken);
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

    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var correlationId = _telemetryProvider.GetCorrelationId();

        return StreamWithTelemetryAsync(
            _inner.StreamAsync(request, cancellationToken),
            requestType,
            responseType,
            correlationId);
    }

    private async IAsyncEnumerable<TResponse> StreamWithTelemetryAsync<TResponse>(
        IAsyncEnumerable<TResponse> source,
        Type requestType,
        Type responseType,
        string? correlationId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = _telemetryProvider.StartActivity("Relay.Stream", requestType, correlationId);
        var stopwatch = Stopwatch.StartNew();
        var itemCount = 0L;
        Exception? exception = null;

        await foreach (var item in source)
        {
            itemCount++;
            yield return item;
        }

        stopwatch.Stop();
        _telemetryProvider.RecordStreamingOperation(requestType, responseType, null, stopwatch.Elapsed, itemCount, exception == null, exception);
    }

    public async ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var notificationType = typeof(TNotification);
        var correlationId = _telemetryProvider.GetCorrelationId();

        using var activity = _telemetryProvider.StartActivity("Relay.Publish", notificationType, correlationId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _inner.PublishAsync(notification, cancellationToken);
            stopwatch.Stop();

            // Estimate handler count - for IRelay.PublishAsync, we assume at least 1 handler
            // In a more sophisticated implementation, this could be tracked or accessed from registry
            _telemetryProvider.RecordNotificationPublish(notificationType, 1, stopwatch.Elapsed, true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _telemetryProvider.RecordNotificationPublish(notificationType, 1, stopwatch.Elapsed, false, ex);
            throw;
        }
    }
}