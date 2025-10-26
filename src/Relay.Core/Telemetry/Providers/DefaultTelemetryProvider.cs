using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Telemetry;

/// <summary>
/// Default implementation of ITelemetryProvider using System.Diagnostics.Activity
/// </summary>
public class DefaultTelemetryProvider : ITelemetryProvider
{
    private static readonly ActivitySource ActivitySource = new("Relay.Core", "1.0.0");
    private static readonly AsyncLocal<string?> CorrelationIdContext = new();

    private readonly ILogger<DefaultTelemetryProvider>? _logger;

    public DefaultTelemetryProvider(ILogger<DefaultTelemetryProvider>? logger = null, IMetricsProvider? metricsProvider = null)
    {
        _logger = logger;
        MetricsProvider = metricsProvider ?? new DefaultMetricsProvider(null);
    }

    public IMetricsProvider? MetricsProvider { get; }

    public Activity? StartActivity(string operationName, Type requestType, string? correlationId = null)
    {
        var activity = ActivitySource.StartActivity(operationName);

        if (activity != null)
        {
            activity.SetTag("relay.request_type", requestType.FullName);
            activity.SetTag("relay.operation", operationName);

            if (correlationId != null)
            {
                activity.SetTag("relay.correlation_id", correlationId);
                SetCorrelationId(correlationId);
            }

            _logger?.LogDebug("Started activity {ActivityId} for {RequestType}", activity.Id, requestType.Name);
        }

        return activity;
    }

    public void RecordHandlerExecution(Type requestType, Type? responseType, string? handlerName, TimeSpan duration, bool success, Exception? exception = null)
    {
        var activity = Activity.Current;
        var operationId = activity?.Id ?? Guid.NewGuid().ToString();

        if (activity != null)
        {
            activity.SetTag("relay.handler_name", handlerName);
            activity.SetTag("relay.response_type", responseType?.FullName);
            activity.SetTag("relay.duration_ms", duration.TotalMilliseconds);
            activity.SetTag("relay.success", success);

            if (exception != null)
            {
                activity.SetTag("relay.exception_type", exception.GetType().FullName);
                activity.SetTag("relay.exception_message", exception.Message);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else if (success)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
        }

        // Record detailed metrics
        MetricsProvider?.RecordHandlerExecution(new HandlerExecutionMetrics
        {
            OperationId = operationId,
            RequestType = requestType,
            ResponseType = responseType,
            HandlerName = handlerName,
            Duration = duration,
            Success = success,
            Exception = exception
        });

        _logger?.LogDebug("Handler execution completed: {RequestType} -> {ResponseType} in {Duration}ms (Success: {Success})",
            requestType.Name, responseType?.Name, duration.TotalMilliseconds, success);
    }

    public void RecordNotificationPublish(Type notificationType, int handlerCount, TimeSpan duration, bool success, Exception? exception = null)
    {
        var activity = Activity.Current;
        var operationId = activity?.Id ?? Guid.NewGuid().ToString();

        if (activity != null)
        {
            activity.SetTag("relay.notification_type", notificationType.FullName);
            activity.SetTag("relay.handler_count", handlerCount);
            activity.SetTag("relay.duration_ms", duration.TotalMilliseconds);
            activity.SetTag("relay.success", success);

            if (exception != null)
            {
                activity.SetTag("relay.exception_type", exception.GetType().FullName);
                activity.SetTag("relay.exception_message", exception.Message);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else if (success)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
        }

        // Record detailed metrics
        MetricsProvider?.RecordNotificationPublish(new NotificationPublishMetrics
        {
            OperationId = operationId,
            NotificationType = notificationType,
            HandlerCount = handlerCount,
            Duration = duration,
            Success = success,
            Exception = exception
        });

        _logger?.LogDebug("Notification published: {NotificationType} to {HandlerCount} handlers in {Duration}ms (Success: {Success})",
            notificationType.Name, handlerCount, duration.TotalMilliseconds, success);
    }

    public void RecordStreamingOperation(Type requestType, Type responseType, string? handlerName, TimeSpan duration, long itemCount, bool success, Exception? exception = null)
    {
        var activity = Activity.Current;
        var operationId = activity?.Id ?? Guid.NewGuid().ToString();

        if (activity != null)
        {
            activity.SetTag("relay.request_type", requestType.FullName);
            activity.SetTag("relay.response_type", responseType.FullName);
            activity.SetTag("relay.handler_name", handlerName);
            activity.SetTag("relay.item_count", itemCount);
            activity.SetTag("relay.duration_ms", duration.TotalMilliseconds);
            activity.SetTag("relay.success", success);

            if (exception != null)
            {
                activity.SetTag("relay.exception_type", exception.GetType().FullName);
                activity.SetTag("relay.exception_message", exception.Message);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else if (success)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
        }

        // Record detailed metrics
        MetricsProvider?.RecordStreamingOperation(new StreamingOperationMetrics
        {
            OperationId = operationId,
            RequestType = requestType,
            ResponseType = responseType,
            HandlerName = handlerName,
            Duration = duration,
            ItemCount = itemCount,
            Success = success,
            Exception = exception
        });

        _logger?.LogDebug("Streaming operation completed: {RequestType} -> {ResponseType} ({ItemCount} items) in {Duration}ms (Success: {Success})",
            requestType.Name, responseType.Name, itemCount, duration.TotalMilliseconds, success);
    }

    public void SetCorrelationId(string correlationId)
    {
        CorrelationIdContext.Value = correlationId;

        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetTag("relay.correlation_id", correlationId);
        }
    }

    public string? GetCorrelationId()
    {
        return CorrelationIdContext.Value ?? Activity.Current?.GetTagItem("relay.correlation_id")?.ToString();
    }

    public void RecordCircuitBreakerStateChange(string circuitBreakerName, string oldState, string newState)
    {
        var activity = Activity.Current;

        if (activity != null)
        {
            var eventName = newState.ToLower() switch
            {
                "open" => "circuit_breaker.opened",
                "closed" => "circuit_breaker.closed",
                "halfopen" => "circuit_breaker.half_opened",
                _ => "circuit_breaker.state_changed"
            };

            activity.AddEvent(new ActivityEvent(eventName));
            activity.SetTag("relay.circuit_breaker.state", newState);
        }

        _logger?.LogInformation("Circuit breaker '{CircuitBreakerName}' state changed from {OldState} to {NewState}",
            circuitBreakerName, oldState, newState);
    }

    public void RecordCircuitBreakerOperation(string circuitBreakerName, string operation, bool success, Exception? exception = null)
    {
        var activity = Activity.Current;

        if (activity != null)
        {
            activity.SetTag("relay.circuit_breaker.operation", operation);
            activity.SetTag("relay.success", success);

            if (exception != null)
            {
                activity.SetTag("error.type", exception.GetType().FullName);
                activity.SetTag("error.message", exception.Message);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else if (success)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
        }

        _logger?.LogDebug("Circuit breaker '{CircuitBreakerName}' operation '{Operation}' (Success: {Success})",
            circuitBreakerName, operation, success);
    }
}