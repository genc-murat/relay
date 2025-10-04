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

/// <summary>
/// Metrics for handler execution
/// </summary>
public class HandlerExecutionMetrics
{
    public string OperationId { get; set; } = string.Empty;
    public Type RequestType { get; set; } = null!;
    public Type? ResponseType { get; set; }
    public string? HandlerName { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Metrics for notification publishing
/// </summary>
public class NotificationPublishMetrics
{
    public string OperationId { get; set; } = string.Empty;
    public Type NotificationType { get; set; } = null!;
    public int HandlerCount { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Metrics for streaming operations
/// </summary>
public class StreamingOperationMetrics
{
    public string OperationId { get; set; } = string.Empty;
    public Type RequestType { get; set; } = null!;
    public Type ResponseType { get; set; } = null!;
    public string? HandlerName { get; set; }
    public TimeSpan Duration { get; set; }
    public long ItemCount { get; set; }
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Statistics for handler execution
/// </summary>
public class HandlerExecutionStats
{
    public Type RequestType { get; set; } = null!;
    public string? HandlerName { get; set; }
    public long TotalExecutions { get; set; }
    public long SuccessfulExecutions { get; set; }
    public long FailedExecutions { get; set; }
    public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions : 0;
    public TimeSpan AverageExecutionTime { get; set; }
    public TimeSpan MinExecutionTime { get; set; }
    public TimeSpan MaxExecutionTime { get; set; }
    public TimeSpan P50ExecutionTime { get; set; }
    public TimeSpan P95ExecutionTime { get; set; }
    public TimeSpan P99ExecutionTime { get; set; }
    public DateTimeOffset LastExecution { get; set; }
    public long TotalMemoryAllocated { get; set; }
    public long AverageMemoryAllocated { get; set; }
    public long MinMemoryAllocated { get; set; }
    public long MaxMemoryAllocated { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Statistics for notification publishing
/// </summary>
public class NotificationPublishStats
{
    public Type NotificationType { get; set; } = null!;
    public long TotalPublishes { get; set; }
    public long SuccessfulPublishes { get; set; }
    public long FailedPublishes { get; set; }
    public double SuccessRate => TotalPublishes > 0 ? (double)SuccessfulPublishes / TotalPublishes : 0;
    public TimeSpan AveragePublishTime { get; set; }
    public TimeSpan MinPublishTime { get; set; }
    public TimeSpan MaxPublishTime { get; set; }
    public double AverageHandlerCount { get; set; }
    public DateTimeOffset LastPublish { get; set; }
}

/// <summary>
/// Statistics for streaming operations
/// </summary>
public class StreamingOperationStats
{
    public Type RequestType { get; set; } = null!;
    public string? HandlerName { get; set; }
    public long TotalOperations { get; set; }
    public long SuccessfulOperations { get; set; }
    public long FailedOperations { get; set; }
    public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations : 0;
    public TimeSpan AverageOperationTime { get; set; }
    public long TotalItemsStreamed { get; set; }
    public double AverageItemsPerOperation { get; set; }
    public double ItemsPerSecond { get; set; }
    public DateTimeOffset LastOperation { get; set; }
}

/// <summary>
/// Represents a performance anomaly
/// </summary>
public class PerformanceAnomaly
{
    public string OperationId { get; set; } = string.Empty;
    public Type RequestType { get; set; } = null!;
    public string? HandlerName { get; set; }
    public AnomalyType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public TimeSpan ActualDuration { get; set; }
    public TimeSpan ExpectedDuration { get; set; }
    public double Severity { get; set; }
    public DateTimeOffset DetectedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Types of performance anomalies
/// </summary>
public enum AnomalyType
{
    SlowExecution,
    HighFailureRate,
    UnusualItemCount,
    MemorySpike,
    TimeoutExceeded
}

/// <summary>
/// Detailed timing breakdown for an operation
/// </summary>
public class TimingBreakdown
{
    public string OperationId { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public Dictionary<string, TimeSpan> PhaseTimings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}