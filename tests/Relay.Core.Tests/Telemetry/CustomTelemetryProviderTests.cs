using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests;

public class CustomTelemetryProviderTests
{
    [Fact]
    public void CustomMetricsProvider_ShouldIntegrateWithTelemetrySystem()
    {
        // Arrange
        var customMetricsProvider = new CustomMetricsProvider();
        var telemetryProvider = new DefaultTelemetryProvider(null, customMetricsProvider);

        // Act
        telemetryProvider.RecordHandlerExecution(
            typeof(TestRequest),
            typeof(string),
            "CustomHandler",
            TimeSpan.FromMilliseconds(150),
            true);

        // Assert
        Assert.Single(customMetricsProvider.RecordedMetrics);
        var metric = customMetricsProvider.RecordedMetrics[0];
        Assert.Equal(typeof(TestRequest), metric.RequestType);
        Assert.Equal(typeof(string), metric.ResponseType);
        Assert.Equal("CustomHandler", metric.HandlerName);
        Assert.Equal(TimeSpan.FromMilliseconds(150), metric.Duration);
        Assert.True(metric.Success);
    }

    [Fact]
    public void ExternalMetricsProvider_ShouldReceiveAllMetricTypes()
    {
        // Arrange
        var externalProvider = new ExternalMetricsProvider();
        var telemetryProvider = new DefaultTelemetryProvider(null, externalProvider);

        // Act - Record different types of metrics
        telemetryProvider.RecordHandlerExecution(
            typeof(TestRequest), typeof(string), "Handler1", TimeSpan.FromMilliseconds(100), true);

        telemetryProvider.RecordNotificationPublish(
            typeof(TestNotification), 3, TimeSpan.FromMilliseconds(50), true);

        telemetryProvider.RecordStreamingOperation(
            typeof(TestStreamRequest), typeof(string), "StreamHandler",
            TimeSpan.FromMilliseconds(200), 100, true);

        // Assert
        Assert.Single(externalProvider.HandlerMetrics);
        Assert.Single(externalProvider.NotificationMetrics);
        Assert.Single(externalProvider.StreamingMetrics);

        // Verify external system integration
        Assert.Equal("EXTERNAL_HANDLER_100ms", externalProvider.HandlerMetrics[0].ExternalId);
        Assert.Equal("EXTERNAL_NOTIFICATION_50ms", externalProvider.NotificationMetrics[0].ExternalId);
        Assert.Equal("EXTERNAL_STREAM_200ms", externalProvider.StreamingMetrics[0].ExternalId);
    }

    [Fact]
    public void AggregatingMetricsProvider_ShouldCombineMultipleProviders()
    {
        // Arrange
        var provider1 = new CustomMetricsProvider();
        var provider2 = new ExternalMetricsProvider();
        var aggregatingProvider = new AggregatingMetricsProvider(provider1, provider2);

        var telemetryProvider = new DefaultTelemetryProvider(null, aggregatingProvider);

        // Act
        telemetryProvider.RecordHandlerExecution(
            typeof(TestRequest), typeof(string), "AggregatedHandler",
            TimeSpan.FromMilliseconds(75), true);

        // Assert - Both providers should receive the metrics
        Assert.Single(provider1.RecordedMetrics);
        Assert.Single(provider2.HandlerMetrics);

        Assert.Equal("AggregatedHandler", provider1.RecordedMetrics[0].HandlerName);
        Assert.Equal("EXTERNAL_HANDLER_75ms", provider2.HandlerMetrics[0].ExternalId);
    }

    [Fact]
    public void MetricsProviderWithFiltering_ShouldFilterBasedOnCriteria()
    {
        // Arrange
        var baseProvider = new CustomMetricsProvider();
        var filteringProvider = new FilteringMetricsProvider(baseProvider,
            metric => metric.Duration > TimeSpan.FromMilliseconds(100)); // Only record slow operations

        var telemetryProvider = new DefaultTelemetryProvider(null, filteringProvider);

        // Act - Record fast and slow operations
        telemetryProvider.RecordHandlerExecution(
            typeof(TestRequest), typeof(string), "FastHandler",
            TimeSpan.FromMilliseconds(50), true); // Should be filtered out

        telemetryProvider.RecordHandlerExecution(
            typeof(TestRequest), typeof(string), "SlowHandler",
            TimeSpan.FromMilliseconds(150), true); // Should be recorded

        // Assert - Only the slow operation should be recorded
        Assert.Single(baseProvider.RecordedMetrics);
        Assert.Equal("SlowHandler", baseProvider.RecordedMetrics[0].HandlerName);
        Assert.Equal(TimeSpan.FromMilliseconds(150), baseProvider.RecordedMetrics[0].Duration);
    }

    [Fact]
    public async Task AsyncMetricsProvider_ShouldHandleAsyncOperations()
    {
        // Arrange
        var asyncProvider = new AsyncMetricsProvider();
        var telemetryProvider = new DefaultTelemetryProvider(null, asyncProvider);

        // Act
        telemetryProvider.RecordHandlerExecution(
            typeof(TestRequest), typeof(string), "AsyncHandler",
            TimeSpan.FromMilliseconds(200), true);

        // Wait for async processing to complete
        await Task.Delay(100);

        // Assert
        Assert.Single(asyncProvider.ProcessedMetrics);
        Assert.True(asyncProvider.ProcessedMetrics[0].ProcessedAt > DateTimeOffset.UtcNow.AddSeconds(-1));
    }
}

/// <summary>
/// Custom metrics provider for testing extensibility
/// </summary>
public class CustomMetricsProvider : IMetricsProvider
{
    public List<HandlerExecutionMetrics> RecordedMetrics { get; } = new();
    public List<NotificationPublishMetrics> RecordedNotifications { get; } = new();
    public List<StreamingOperationMetrics> RecordedStreaming { get; } = new();

    public void RecordHandlerExecution(HandlerExecutionMetrics metrics)
    {
        RecordedMetrics.Add(metrics);
    }

    public void RecordNotificationPublish(NotificationPublishMetrics metrics)
    {
        RecordedNotifications.Add(metrics);
    }

    public void RecordStreamingOperation(StreamingOperationMetrics metrics)
    {
        RecordedStreaming.Add(metrics);
    }

    public HandlerExecutionStats GetHandlerExecutionStats(Type requestType, string? handlerName = null)
    {
        var executions = RecordedMetrics
            .Where(m => m.RequestType == requestType && (handlerName == null || m.HandlerName == handlerName))
            .ToList();

        if (executions.Count == 0)
        {
            return new HandlerExecutionStats { RequestType = requestType, HandlerName = handlerName };
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
            LastExecution = executions.Max(e => e.Timestamp)
        };
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
        return Enumerable.Empty<PerformanceAnomaly>();
    }

    public TimingBreakdown GetTimingBreakdown(string operationId)
    {
        return new TimingBreakdown { OperationId = operationId };
    }

    public void RecordTimingBreakdown(TimingBreakdown breakdown)
    {
        // Custom implementation could store breakdowns
    }
}

/// <summary>
/// External metrics provider that integrates with external monitoring systems
/// </summary>
public class ExternalMetricsProvider : IMetricsProvider
{
    public List<ExternalHandlerMetric> HandlerMetrics { get; } = new();
    public List<ExternalNotificationMetric> NotificationMetrics { get; } = new();
    public List<ExternalStreamingMetric> StreamingMetrics { get; } = new();

    public void RecordHandlerExecution(HandlerExecutionMetrics metrics)
    {
        // Simulate sending to external monitoring system
        HandlerMetrics.Add(new ExternalHandlerMetric
        {
            ExternalId = $"EXTERNAL_HANDLER_{metrics.Duration.TotalMilliseconds}ms",
            RequestType = metrics.RequestType.Name,
            Duration = metrics.Duration,
            Success = metrics.Success,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public void RecordNotificationPublish(NotificationPublishMetrics metrics)
    {
        NotificationMetrics.Add(new ExternalNotificationMetric
        {
            ExternalId = $"EXTERNAL_NOTIFICATION_{metrics.Duration.TotalMilliseconds}ms",
            NotificationType = metrics.NotificationType.Name,
            HandlerCount = metrics.HandlerCount,
            Duration = metrics.Duration,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public void RecordStreamingOperation(StreamingOperationMetrics metrics)
    {
        StreamingMetrics.Add(new ExternalStreamingMetric
        {
            ExternalId = $"EXTERNAL_STREAM_{metrics.Duration.TotalMilliseconds}ms",
            RequestType = metrics.RequestType.Name,
            ItemCount = metrics.ItemCount,
            Duration = metrics.Duration,
            Timestamp = DateTimeOffset.UtcNow
        });
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
        return Enumerable.Empty<PerformanceAnomaly>();
    }

    public TimingBreakdown GetTimingBreakdown(string operationId)
    {
        return new TimingBreakdown { OperationId = operationId };
    }

    public void RecordTimingBreakdown(TimingBreakdown breakdown)
    {
        // External system integration
    }
}

/// <summary>
/// Aggregating metrics provider that forwards to multiple providers
/// </summary>
public class AggregatingMetricsProvider : IMetricsProvider
{
    private readonly IMetricsProvider[] _providers;

    public AggregatingMetricsProvider(params IMetricsProvider[] providers)
    {
        _providers = providers;
    }

    public void RecordHandlerExecution(HandlerExecutionMetrics metrics)
    {
        foreach (var provider in _providers)
        {
            provider.RecordHandlerExecution(metrics);
        }
    }

    public void RecordNotificationPublish(NotificationPublishMetrics metrics)
    {
        foreach (var provider in _providers)
        {
            provider.RecordNotificationPublish(metrics);
        }
    }

    public void RecordStreamingOperation(StreamingOperationMetrics metrics)
    {
        foreach (var provider in _providers)
        {
            provider.RecordStreamingOperation(metrics);
        }
    }

    public HandlerExecutionStats GetHandlerExecutionStats(Type requestType, string? handlerName = null)
    {
        // Return stats from the first provider (could be enhanced to aggregate)
        return _providers.FirstOrDefault()?.GetHandlerExecutionStats(requestType, handlerName)
            ?? new HandlerExecutionStats { RequestType = requestType, HandlerName = handlerName };
    }

    public NotificationPublishStats GetNotificationPublishStats(Type notificationType)
    {
        return _providers.FirstOrDefault()?.GetNotificationPublishStats(notificationType)
            ?? new NotificationPublishStats { NotificationType = notificationType };
    }

    public StreamingOperationStats GetStreamingOperationStats(Type requestType, string? handlerName = null)
    {
        return _providers.FirstOrDefault()?.GetStreamingOperationStats(requestType, handlerName)
            ?? new StreamingOperationStats { RequestType = requestType, HandlerName = handlerName };
    }

    public IEnumerable<PerformanceAnomaly> DetectAnomalies(TimeSpan lookbackPeriod)
    {
        return _providers.SelectMany(p => p.DetectAnomalies(lookbackPeriod));
    }

    public TimingBreakdown GetTimingBreakdown(string operationId)
    {
        return _providers.FirstOrDefault()?.GetTimingBreakdown(operationId)
            ?? new TimingBreakdown { OperationId = operationId };
    }

    public void RecordTimingBreakdown(TimingBreakdown breakdown)
    {
        foreach (var provider in _providers)
        {
            provider.RecordTimingBreakdown(breakdown);
        }
    }
}

/// <summary>
/// Filtering metrics provider that only records metrics meeting certain criteria
/// </summary>
public class FilteringMetricsProvider : IMetricsProvider
{
    private readonly IMetricsProvider _baseProvider;
    private readonly Func<HandlerExecutionMetrics, bool> _handlerFilter;

    public FilteringMetricsProvider(IMetricsProvider baseProvider, Func<HandlerExecutionMetrics, bool> handlerFilter)
    {
        _baseProvider = baseProvider;
        _handlerFilter = handlerFilter;
    }

    public void RecordHandlerExecution(HandlerExecutionMetrics metrics)
    {
        if (_handlerFilter(metrics))
        {
            _baseProvider.RecordHandlerExecution(metrics);
        }
    }

    public void RecordNotificationPublish(NotificationPublishMetrics metrics)
    {
        _baseProvider.RecordNotificationPublish(metrics);
    }

    public void RecordStreamingOperation(StreamingOperationMetrics metrics)
    {
        _baseProvider.RecordStreamingOperation(metrics);
    }

    public HandlerExecutionStats GetHandlerExecutionStats(Type requestType, string? handlerName = null)
    {
        return _baseProvider.GetHandlerExecutionStats(requestType, handlerName);
    }

    public NotificationPublishStats GetNotificationPublishStats(Type notificationType)
    {
        return _baseProvider.GetNotificationPublishStats(notificationType);
    }

    public StreamingOperationStats GetStreamingOperationStats(Type requestType, string? handlerName = null)
    {
        return _baseProvider.GetStreamingOperationStats(requestType, handlerName);
    }

    public IEnumerable<PerformanceAnomaly> DetectAnomalies(TimeSpan lookbackPeriod)
    {
        return _baseProvider.DetectAnomalies(lookbackPeriod);
    }

    public TimingBreakdown GetTimingBreakdown(string operationId)
    {
        return _baseProvider.GetTimingBreakdown(operationId);
    }

    public void RecordTimingBreakdown(TimingBreakdown breakdown)
    {
        _baseProvider.RecordTimingBreakdown(breakdown);
    }
}

/// <summary>
/// Async metrics provider that processes metrics asynchronously
/// </summary>
public class AsyncMetricsProvider : IMetricsProvider
{
    public List<ProcessedMetric> ProcessedMetrics { get; } = new();

    public void RecordHandlerExecution(HandlerExecutionMetrics metrics)
    {
        // Simulate async processing
        Task.Run(async () =>
        {
            await Task.Delay(50); // Simulate processing time
            ProcessedMetrics.Add(new ProcessedMetric
            {
                OriginalMetrics = metrics,
                ProcessedAt = DateTimeOffset.UtcNow
            });
        });
    }

    public void RecordNotificationPublish(NotificationPublishMetrics metrics)
    {
        // Async processing for notifications
    }

    public void RecordStreamingOperation(StreamingOperationMetrics metrics)
    {
        // Async processing for streaming
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
        return Enumerable.Empty<PerformanceAnomaly>();
    }

    public TimingBreakdown GetTimingBreakdown(string operationId)
    {
        return new TimingBreakdown { OperationId = operationId };
    }

    public void RecordTimingBreakdown(TimingBreakdown breakdown)
    {
        // Async processing for timing breakdowns
    }
}

// Supporting classes for external metrics
public class ExternalHandlerMetric
{
    public string ExternalId { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class ExternalNotificationMetric
{
    public string ExternalId { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public int HandlerCount { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class ExternalStreamingMetric
{
    public string ExternalId { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public long ItemCount { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class ProcessedMetric
{
    public HandlerExecutionMetrics OriginalMetrics { get; set; } = null!;
    public DateTimeOffset ProcessedAt { get; set; }
}

// Test request types
public class TestRequest : IRequest<string> { }
public class TestStreamRequest : IStreamRequest<string> { }
public class TestNotification : INotification { }