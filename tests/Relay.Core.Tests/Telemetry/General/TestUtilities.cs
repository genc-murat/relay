using Microsoft.Extensions.Logging;
using Relay.Core.Telemetry;
using Relay.Core.Testing;
using System;
using System.Collections.Generic;

namespace Relay.Core.Tests.Telemetry;

/// <summary>
/// Shared test utilities for telemetry tests
/// </summary>
public static class TestUtilities
{
    // Supporting test classes shared across multiple test files
}

// Supporting test classes
public class TestNotification { }
public class TestMessage { }

/// <summary>
/// Test logger for capturing log messages
/// </summary>
public class TestLogger : ILogger<Relay.Core.Telemetry.RelayTelemetryProvider>
{
    public List<string> LoggedMessages { get; } = new();

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        LoggedMessages.Add(message);
    }
}

/// <summary>
/// Custom metrics provider for testing
/// </summary>
public class CustomMetricsProvider : IMetricsProvider
{
    private readonly object _lock = new();
    private int _callCount;
    public int CallCount => _callCount;
    public List<HandlerExecutionMetrics> HandlerExecutions { get; } = new();
    public List<NotificationPublishMetrics> NotificationPublishes { get; } = new();
    public List<StreamingOperationMetrics> StreamingOperations { get; } = new();

    public void RecordHandlerExecution(HandlerExecutionMetrics metrics)
    {
        lock (_lock)
        {
            HandlerExecutions.Add(metrics);
            _callCount++;
        }
    }

    public void RecordNotificationPublish(NotificationPublishMetrics metrics)
    {
        lock (_lock)
        {
            NotificationPublishes.Add(metrics);
        }
    }

    public void RecordStreamingOperation(StreamingOperationMetrics metrics)
    {
        lock (_lock)
        {
            StreamingOperations.Add(metrics);
        }
    }

    public HandlerExecutionStats GetHandlerExecutionStats(Type requestType, string? handlerName = null)
    {
        return new HandlerExecutionStats { RequestType = requestType, HandlerName = handlerName };
    }

    public NotificationPublishStats GetNotificationPublishStats(Type notificationType)
    {
        return new NotificationPublishStats { NotificationType = notificationType };
    }

    public StreamingOperationStats GetStreamingOperationStats(Type requestType, string? handlerName = null)
    {
        return new StreamingOperationStats { RequestType = requestType, HandlerName = handlerName };
    }

    public IEnumerable<PerformanceAnomaly> DetectAnomalies(TimeSpan lookbackPeriod)
    {
        return new List<PerformanceAnomaly>();
    }

    public TimingBreakdown GetTimingBreakdown(string operationId)
    {
        return new TimingBreakdown { OperationId = operationId };
    }

    public void RecordTimingBreakdown(TimingBreakdown breakdown)
    {
        // Implementation not needed for tests
    }
}
