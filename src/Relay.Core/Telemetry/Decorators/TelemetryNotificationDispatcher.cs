using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Dispatchers;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Telemetry;

/// <summary>
/// Decorator for INotificationDispatcher that adds telemetry capabilities
/// </summary>
public class TelemetryNotificationDispatcher : INotificationDispatcher
{
    private readonly INotificationDispatcher _inner;
    private readonly ITelemetryProvider _telemetryProvider;

    public TelemetryNotificationDispatcher(INotificationDispatcher inner, ITelemetryProvider telemetryProvider)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _telemetryProvider = telemetryProvider ?? throw new ArgumentNullException(nameof(telemetryProvider));
    }

    private int GetHandlerCount(Type notificationType)
    {
        if (_inner is NotificationDispatcher dispatcher)
        {
            return dispatcher.GetHandlers(notificationType).Count;
        }
        return 0;
    }

    public async ValueTask DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var notificationType = typeof(TNotification);
        var handlerCount = GetHandlerCount(notificationType);
        var correlationId = _telemetryProvider.GetCorrelationId();

        using var activity = _telemetryProvider.StartActivity("Relay.Notification", notificationType, correlationId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _inner.DispatchAsync(notification, cancellationToken);
            stopwatch.Stop();

            _telemetryProvider.RecordNotificationPublish(notificationType, handlerCount, stopwatch.Elapsed, true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _telemetryProvider.RecordNotificationPublish(notificationType, handlerCount, stopwatch.Elapsed, false, ex);
            throw;
        }
    }
}