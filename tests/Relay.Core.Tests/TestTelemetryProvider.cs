using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Relay.Core.Telemetry;

namespace Relay.Core.Tests;

public class TestTelemetryProvider : ITelemetryProvider
{
    public List<TestActivity> Activities { get; } = new();
    public List<HandlerExecution> HandlerExecutions { get; } = new();
    public List<NotificationPublish> NotificationPublishes { get; } = new();
    public List<StreamingOperation> StreamingOperations { get; } = new();

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

    public void SetCorrelationId(string correlationId)
    {
        _correlationId = correlationId;
    }

    public string? GetCorrelationId()
    {
        return _correlationId;
    }
}

public class TestActivity
{
    public string OperationName { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class HandlerExecution
{
    public Type RequestType { get; set; } = null!;
    public Type? ResponseType { get; set; }
    public string? HandlerName { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
}

public class NotificationPublish
{
    public Type NotificationType { get; set; } = null!;
    public int HandlerCount { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
}

public class StreamingOperation
{
    public Type RequestType { get; set; } = null!;
    public Type ResponseType { get; set; } = null!;
    public string? HandlerName { get; set; }
    public TimeSpan Duration { get; set; }
    public long ItemCount { get; set; }
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
}

public class TestMetricsProvider : IMetricsProvider
{
    public List<HandlerExecutionMetrics> HandlerExecutionMetrics { get; } = new();
    public List<NotificationPublishMetrics> NotificationPublishMetrics { get; } = new();
    public List<StreamingOperationMetrics> StreamingOperationMetrics { get; } = new();
    public List<PerformanceAnomaly> DetectedAnomalies { get; } = new();
    public Dictionary<string, TimingBreakdown> TimingBreakdowns { get; } = new();

    public void RecordHandlerExecution(HandlerExecutionMetrics metrics)
    {
        HandlerExecutionMetrics.Add(metrics);
    }

    public void RecordNotificationPublish(NotificationPublishMetrics metrics)
    {
        NotificationPublishMetrics.Add(metrics);
    }

    public void RecordStreamingOperation(StreamingOperationMetrics metrics)
    {
        StreamingOperationMetrics.Add(metrics);
    }

    public HandlerExecutionStats GetHandlerExecutionStats(Type requestType, string? handlerName = null)
    {
        var executions = HandlerExecutionMetrics
            .Where(m => m.RequestType == requestType && (handlerName == null || m.HandlerName == handlerName))
            .ToList();

        if (executions.Count == 0)
        {
            return new HandlerExecutionStats
            {
                RequestType = requestType,
                HandlerName = handlerName
            };
        }

        var successful = executions.Where(e => e.Success).ToList();
        var durations = executions.Select(e => e.Duration).OrderBy(d => d).ToList();

        return new HandlerExecutionStats
        {
            RequestType = requestType,
            HandlerName = handlerName,
            TotalExecutions = executions.Count,
            SuccessfulExecutions = successful.Count,
            FailedExecutions = executions.Count - successful.Count,
            AverageExecutionTime = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks)),
            MinExecutionTime = durations.First(),
            MaxExecutionTime = durations.Last(),
            P50ExecutionTime = GetPercentile(durations, 0.5),
            P95ExecutionTime = GetPercentile(durations, 0.95),
            P99ExecutionTime = GetPercentile(durations, 0.99),
            LastExecution = executions.Max(e => e.Timestamp)
        };
    }

    public NotificationPublishStats GetNotificationPublishStats(Type notificationType)
    {
        var publishes = NotificationPublishMetrics
            .Where(m => m.NotificationType == notificationType)
            .ToList();

        if (publishes.Count == 0)
        {
            return new NotificationPublishStats
            {
                NotificationType = notificationType
            };
        }

        var successful = publishes.Where(p => p.Success).ToList();
        var durations = publishes.Select(p => p.Duration).OrderBy(d => d).ToList();

        return new NotificationPublishStats
        {
            NotificationType = notificationType,
            TotalPublishes = publishes.Count,
            SuccessfulPublishes = successful.Count,
            FailedPublishes = publishes.Count - successful.Count,
            AveragePublishTime = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks)),
            MinPublishTime = durations.First(),
            MaxPublishTime = durations.Last(),
            AverageHandlerCount = publishes.Average(p => p.HandlerCount),
            LastPublish = publishes.Max(p => p.Timestamp)
        };
    }

    public StreamingOperationStats GetStreamingOperationStats(Type requestType, string? handlerName = null)
    {
        var operations = StreamingOperationMetrics
            .Where(m => m.RequestType == requestType && (handlerName == null || m.HandlerName == handlerName))
            .ToList();

        if (operations.Count == 0)
        {
            return new StreamingOperationStats
            {
                RequestType = requestType,
                HandlerName = handlerName
            };
        }

        var successful = operations.Where(o => o.Success).ToList();
        var totalItems = operations.Sum(o => o.ItemCount);
        var totalDuration = TimeSpan.FromTicks(operations.Sum(o => o.Duration.Ticks));

        return new StreamingOperationStats
        {
            RequestType = requestType,
            HandlerName = handlerName,
            TotalOperations = operations.Count,
            SuccessfulOperations = successful.Count,
            FailedOperations = operations.Count - successful.Count,
            AverageOperationTime = TimeSpan.FromTicks((long)operations.Average(o => o.Duration.Ticks)),
            TotalItemsStreamed = totalItems,
            AverageItemsPerOperation = operations.Average(o => o.ItemCount),
            ItemsPerSecond = totalDuration.TotalSeconds > 0 ? totalItems / totalDuration.TotalSeconds : 0,
            LastOperation = operations.Max(o => o.Timestamp)
        };
    }

    public IEnumerable<PerformanceAnomaly> DetectAnomalies(TimeSpan lookbackPeriod)
    {
        return DetectedAnomalies.Where(a => a.DetectedAt >= DateTimeOffset.UtcNow - lookbackPeriod);
    }

    public TimingBreakdown GetTimingBreakdown(string operationId)
    {
        return TimingBreakdowns.TryGetValue(operationId, out var breakdown)
            ? breakdown
            : new TimingBreakdown { OperationId = operationId };
    }

    public void RecordTimingBreakdown(TimingBreakdown breakdown)
    {
        TimingBreakdowns[breakdown.OperationId] = breakdown;
    }

    public void AddAnomaly(PerformanceAnomaly anomaly)
    {
        DetectedAnomalies.Add(anomaly);
    }

    public void AddTimingBreakdown(TimingBreakdown breakdown)
    {
        TimingBreakdowns[breakdown.OperationId] = breakdown;
    }

    private static TimeSpan GetPercentile(List<TimeSpan> sortedDurations, double percentile)
    {
        if (sortedDurations.Count == 0) return TimeSpan.Zero;
        if (sortedDurations.Count == 1) return sortedDurations[0];

        var index = (int)Math.Ceiling(sortedDurations.Count * percentile) - 1;
        index = Math.Max(0, Math.Min(index, sortedDurations.Count - 1));
        return sortedDurations[index];
    }
}