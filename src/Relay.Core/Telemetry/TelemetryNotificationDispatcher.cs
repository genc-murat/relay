using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;

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

    public async ValueTask DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var notificationType = typeof(TNotification);
        var correlationId = _telemetryProvider.GetCorrelationId();

        using var activity = _telemetryProvider.StartActivity("Relay.Notification", notificationType, correlationId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _inner.DispatchAsync(notification, cancellationToken);
            stopwatch.Stop();

            // We don't have direct access to handler count here, so we'll use 0 as a placeholder
            // The actual implementation might need to be enhanced to track this
            _telemetryProvider.RecordNotificationPublish(notificationType, 0, stopwatch.Elapsed, true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _telemetryProvider.RecordNotificationPublish(notificationType, 0, stopwatch.Elapsed, false, ex);
            throw;
        }
    }
}