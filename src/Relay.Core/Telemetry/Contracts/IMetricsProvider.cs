using System;
using System.Collections.Generic;

namespace Relay.Core.Telemetry;

/// <summary>
/// Provides metrics collection capabilities for Relay operations
/// </summary>
public interface IMetricsProvider
{
    /// <summary>
    /// Records handler execution metrics
    /// </summary>
    void RecordHandlerExecution(HandlerExecutionMetrics metrics);

    /// <summary>
    /// Records notification publishing metrics
    /// </summary>
    void RecordNotificationPublish(NotificationPublishMetrics metrics);

    /// <summary>
    /// Records streaming operation metrics
    /// </summary>
    void RecordStreamingOperation(StreamingOperationMetrics metrics);

    /// <summary>
    /// Gets handler execution statistics
    /// </summary>
    HandlerExecutionStats GetHandlerExecutionStats(Type requestType, string? handlerName = null);

    /// <summary>
    /// Gets notification publishing statistics
    /// </summary>
    NotificationPublishStats GetNotificationPublishStats(Type notificationType);

    /// <summary>
    /// Gets streaming operation statistics
    /// </summary>
    StreamingOperationStats GetStreamingOperationStats(Type requestType, string? handlerName = null);

    /// <summary>
    /// Detects performance anomalies based on historical data
    /// </summary>
    IEnumerable<PerformanceAnomaly> DetectAnomalies(TimeSpan lookbackPeriod);

    /// <summary>
    /// Gets detailed timing breakdown for a specific operation
    /// </summary>
    TimingBreakdown GetTimingBreakdown(string operationId);

    /// <summary>
    /// Records detailed timing breakdown for an operation
    /// </summary>
    void RecordTimingBreakdown(TimingBreakdown breakdown);
}
