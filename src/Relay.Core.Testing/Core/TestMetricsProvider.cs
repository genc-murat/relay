using System;
using System.Collections.Generic;
using System.Linq;
using Relay.Core.Telemetry;

namespace Relay.Core.Testing;

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
