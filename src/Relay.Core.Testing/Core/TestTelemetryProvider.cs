using System;
using System.Collections.Generic;
using System.Diagnostics;
using Relay.Core.Telemetry;

namespace Relay.Core.Testing;

public class TestTelemetryProvider : ITelemetryProvider
{
    public List<TestActivity> Activities { get; } = new();
    public List<HandlerExecution> HandlerExecutions { get; } = new();
    public List<NotificationPublish> NotificationPublishes { get; } = new();
    public List<StreamingOperation> StreamingOperations { get; } = new();
    public List<CircuitBreakerStateChange> CircuitBreakerStateChanges { get; } = new();
    public List<CircuitBreakerOperation> CircuitBreakerOperations { get; } = new();

    private string? _correlationId;

    public IMetricsProvider? MetricsProvider { get; } = new TestMetricsProvider();

    public Activity? StartActivity(string operationName, Type requestType, string? correlationId = null)
    {
        var testActivity = new TestActivity
        {
            OperationName = operationName,
            Tags = new Dictionary<string, string>
            {
                ["relay.request_type"] = requestType.FullName ?? requestType.Name,
                ["relay.operation"] = operationName
            }
        };

        if (correlationId != null)
        {
            testActivity.Tags["relay.correlation_id"] = correlationId;
            SetCorrelationId(correlationId);
        }

        Activities.Add(testActivity);

        // Return a real Activity for integration with System.Diagnostics
        var activitySource = new ActivitySource("Test");
        var activity = activitySource.StartActivity(operationName);
        if (activity != null)
        {
            foreach (var tag in testActivity.Tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        return activity;
    }

    public void RecordHandlerExecution(Type requestType, Type? responseType, string? handlerName, TimeSpan duration, bool success, Exception? exception = null)
    {
        HandlerExecutions.Add(new HandlerExecution
        {
            RequestType = requestType,
            ResponseType = responseType,
            HandlerName = handlerName,
            Duration = duration,
            Success = success,
            Exception = exception
        });
    }

    public void RecordNotificationPublish(Type notificationType, int handlerCount, TimeSpan duration, bool success, Exception? exception = null)
    {
        NotificationPublishes.Add(new NotificationPublish
        {
            NotificationType = notificationType,
            HandlerCount = handlerCount,
            Duration = duration,
            Success = success,
            Exception = exception
        });
    }

    public void RecordStreamingOperation(Type requestType, Type responseType, string? handlerName, TimeSpan duration, long itemCount, bool success, Exception? exception = null)
    {
        StreamingOperations.Add(new StreamingOperation
        {
            RequestType = requestType,
            ResponseType = responseType,
            HandlerName = handlerName,
            Duration = duration,
            ItemCount = itemCount,
            Success = success,
            Exception = exception
        });
    }

    public void RecordCircuitBreakerStateChange(string circuitBreakerName, string oldState, string newState)
    {
        CircuitBreakerStateChanges.Add(new CircuitBreakerStateChange
        {
            CircuitBreakerName = circuitBreakerName,
            OldState = oldState,
            NewState = newState
        });
    }

    public void RecordCircuitBreakerOperation(string circuitBreakerName, string operation, bool success, Exception? exception = null)
    {
        CircuitBreakerOperations.Add(new CircuitBreakerOperation
        {
            CircuitBreakerName = circuitBreakerName,
            Operation = operation,
            Success = success,
            Exception = exception
        });
    }

    public void SetCorrelationId(string correlationId)
    {
        _correlationId = correlationId;
    }

    public string? GetCorrelationId()
    {
        return _correlationId;
    }
}
