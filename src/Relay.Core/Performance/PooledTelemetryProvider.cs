using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using Relay.Core.Telemetry;

namespace Relay.Core.Performance;

/// <summary>
/// High-performance telemetry provider that uses object pooling to reduce allocations
/// </summary>
public class PooledTelemetryProvider : ITelemetryProvider
{
    private static readonly ActivitySource ActivitySource = new("Relay.Core", "1.0.0");
    private static readonly AsyncLocal<string?> CorrelationIdContext = new();
    
    private readonly ILogger<PooledTelemetryProvider>? _logger;
    private readonly ITelemetryContextPool _contextPool;
    
    public PooledTelemetryProvider(
        ITelemetryContextPool contextPool,
        ILogger<PooledTelemetryProvider>? logger = null, 
        IMetricsProvider? metricsProvider = null)
    {
        _contextPool = contextPool ?? throw new ArgumentNullException(nameof(contextPool));
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
        
        // Use pooled context for metrics to reduce allocations
        var context = _contextPool.Get();
        try
        {
            context.RequestType = requestType;
            context.ResponseType = responseType;
            context.HandlerName = handlerName;
            context.Activity = activity;
            
            // Record detailed metrics using pooled context
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
        }
        finally
        {
            _contextPool.Return(context);
        }
        
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
        
        // Use pooled context for metrics to reduce allocations
        var context = _contextPool.Get();
        try
        {
            context.RequestType = notificationType;
            context.Activity = activity;
            
            // Record detailed metrics using pooled context
            MetricsProvider?.RecordNotificationPublish(new NotificationPublishMetrics
            {
                OperationId = operationId,
                NotificationType = notificationType,
                HandlerCount = handlerCount,
                Duration = duration,
                Success = success,
                Exception = exception
            });
        }
        finally
        {
            _contextPool.Return(context);
        }
        
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
        
        // Use pooled context for metrics to reduce allocations
        var context = _contextPool.Get();
        try
        {
            context.RequestType = requestType;
            context.ResponseType = responseType;
            context.HandlerName = handlerName;
            context.Activity = activity;
            
            // Record detailed metrics using pooled context
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
        }
        finally
        {
            _contextPool.Return(context);
        }
        
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
}