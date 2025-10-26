using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Telemetry;

/// <summary>
/// Provides telemetry capabilities for Relay operations
/// </summary>
public interface ITelemetryProvider
{
    /// <summary>
    /// Starts a new activity for request processing
    /// </summary>
    Activity? StartActivity(string operationName, Type requestType, string? correlationId = null);

    /// <summary>
    /// Records metrics for handler execution
    /// </summary>
    void RecordHandlerExecution(Type requestType, Type? responseType, string? handlerName, TimeSpan duration, bool success, Exception? exception = null);

    /// <summary>
    /// Records metrics for notification publishing
    /// </summary>
    void RecordNotificationPublish(Type notificationType, int handlerCount, TimeSpan duration, bool success, Exception? exception = null);

    /// <summary>
    /// Records metrics for streaming operations
    /// </summary>
    void RecordStreamingOperation(Type requestType, Type responseType, string? handlerName, TimeSpan duration, long itemCount, bool success, Exception? exception = null);

    /// <summary>
    /// Sets correlation ID for the current context
    /// </summary>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Gets the current correlation ID
    /// </summary>
    string? GetCorrelationId();

    /// <summary>
    /// Records circuit breaker state change
    /// </summary>
    void RecordCircuitBreakerStateChange(string circuitBreakerName, string oldState, string newState);

    /// <summary>
    /// Records circuit breaker operation
    /// </summary>
    void RecordCircuitBreakerOperation(string circuitBreakerName, string operation, bool success, Exception? exception = null);

    /// <summary>
    /// Gets the metrics provider for detailed metrics collection
    /// </summary>
    IMetricsProvider? MetricsProvider { get; }
}