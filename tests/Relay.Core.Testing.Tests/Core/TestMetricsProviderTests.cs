using System;
using System.Linq;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Testing.Tests.Core;

public class TestMetricsProviderTests
{
    private readonly TestMetricsProvider _provider = new();

    [Fact]
    public void RecordHandlerExecution_AddsMetricsToList()
    {
        // Arrange
        var metrics = new HandlerExecutionMetrics
        {
            RequestType = typeof(string),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(100),
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        _provider.RecordHandlerExecution(metrics);

        // Assert
        Assert.Single(_provider.HandlerExecutionMetrics);
        Assert.Contains(metrics, _provider.HandlerExecutionMetrics);
    }

    [Fact]
    public void RecordNotificationPublish_AddsMetricsToList()
    {
        // Arrange
        var metrics = new NotificationPublishMetrics
        {
            NotificationType = typeof(string),
            HandlerCount = 5,
            Duration = TimeSpan.FromMilliseconds(50),
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        _provider.RecordNotificationPublish(metrics);

        // Assert
        Assert.Single(_provider.NotificationPublishMetrics);
        Assert.Contains(metrics, _provider.NotificationPublishMetrics);
    }

    [Fact]
    public void RecordStreamingOperation_AddsMetricsToList()
    {
        // Arrange
        var metrics = new StreamingOperationMetrics
        {
            RequestType = typeof(string),
            ResponseType = typeof(int),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(200),
            ItemCount = 100,
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        _provider.RecordStreamingOperation(metrics);

        // Assert
        Assert.Single(_provider.StreamingOperationMetrics);
        Assert.Contains(metrics, _provider.StreamingOperationMetrics);
    }

    [Fact]
    public void GetHandlerExecutionStats_WithNoData_ReturnsEmptyStats()
    {
        // Act
        var stats = _provider.GetHandlerExecutionStats(typeof(string));

        // Assert
        Assert.Equal(typeof(string), stats.RequestType);
        Assert.Null(stats.HandlerName);
        Assert.Equal(0, stats.TotalExecutions);
        Assert.Equal(0, stats.SuccessfulExecutions);
        Assert.Equal(0, stats.FailedExecutions);
        Assert.Equal(TimeSpan.Zero, stats.AverageExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.MinExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.MaxExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.P50ExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.P95ExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.P99ExecutionTime);
        Assert.Equal(default, stats.LastExecution);
    }

    [Fact]
    public void GetHandlerExecutionStats_WithSingleExecution_ReturnsCorrectStats()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var metrics = new HandlerExecutionMetrics
        {
            RequestType = typeof(string),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(100),
            Success = true,
            Timestamp = timestamp
        };
        _provider.RecordHandlerExecution(metrics);

        // Act
        var stats = _provider.GetHandlerExecutionStats(typeof(string));

        // Assert
        Assert.Equal(typeof(string), stats.RequestType);
        Assert.Null(stats.HandlerName);
        Assert.Equal(1, stats.TotalExecutions);
        Assert.Equal(1, stats.SuccessfulExecutions);
        Assert.Equal(0, stats.FailedExecutions);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.AverageExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.MinExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.MaxExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.P50ExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.P95ExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.P99ExecutionTime);
        Assert.Equal(timestamp, stats.LastExecution);
    }

    [Fact]
    public void GetHandlerExecutionStats_WithMultipleExecutions_ReturnsCorrectStats()
    {
        // Arrange
        var baseTime = DateTimeOffset.UtcNow;
        var metrics1 = new HandlerExecutionMetrics
        {
            RequestType = typeof(string),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(100),
            Success = true,
            Timestamp = baseTime
        };
        var metrics2 = new HandlerExecutionMetrics
        {
            RequestType = typeof(string),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(200),
            Success = false,
            Timestamp = baseTime.AddSeconds(1)
        };
        var metrics3 = new HandlerExecutionMetrics
        {
            RequestType = typeof(string),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(150),
            Success = true,
            Timestamp = baseTime.AddSeconds(2)
        };

        _provider.RecordHandlerExecution(metrics1);
        _provider.RecordHandlerExecution(metrics2);
        _provider.RecordHandlerExecution(metrics3);

        // Act
        var stats = _provider.GetHandlerExecutionStats(typeof(string));

        // Assert
        Assert.Equal(3, stats.TotalExecutions);
        Assert.Equal(2, stats.SuccessfulExecutions);
        Assert.Equal(1, stats.FailedExecutions);
        Assert.Equal(TimeSpan.FromMilliseconds(150), stats.AverageExecutionTime); // (100+200+150)/3
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.MinExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(200), stats.MaxExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(150), stats.P50ExecutionTime); // median
        Assert.Equal(TimeSpan.FromMilliseconds(200), stats.P95ExecutionTime); // 95th percentile
        Assert.Equal(TimeSpan.FromMilliseconds(200), stats.P99ExecutionTime); // 99th percentile
        Assert.Equal(baseTime.AddSeconds(2), stats.LastExecution);
    }

    [Fact]
    public void GetHandlerExecutionStats_WithHandlerNameFilter_ReturnsFilteredStats()
    {
        // Arrange
        var metrics1 = new HandlerExecutionMetrics
        {
            RequestType = typeof(string),
            HandlerName = "Handler1",
            Duration = TimeSpan.FromMilliseconds(100),
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        };
        var metrics2 = new HandlerExecutionMetrics
        {
            RequestType = typeof(string),
            HandlerName = "Handler2",
            Duration = TimeSpan.FromMilliseconds(200),
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        };

        _provider.RecordHandlerExecution(metrics1);
        _provider.RecordHandlerExecution(metrics2);

        // Act
        var stats = _provider.GetHandlerExecutionStats(typeof(string), "Handler1");

        // Assert
        Assert.Equal(1, stats.TotalExecutions);
        Assert.Equal("Handler1", stats.HandlerName);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.AverageExecutionTime);
    }

    [Fact]
    public void GetNotificationPublishStats_WithNoData_ReturnsEmptyStats()
    {
        // Act
        var stats = _provider.GetNotificationPublishStats(typeof(string));

        // Assert
        Assert.Equal(typeof(string), stats.NotificationType);
        Assert.Equal(0, stats.TotalPublishes);
        Assert.Equal(0, stats.SuccessfulPublishes);
        Assert.Equal(0, stats.FailedPublishes);
        Assert.Equal(TimeSpan.Zero, stats.AveragePublishTime);
        Assert.Equal(TimeSpan.Zero, stats.MinPublishTime);
        Assert.Equal(TimeSpan.Zero, stats.MaxPublishTime);
        Assert.Equal(0, stats.AverageHandlerCount);
        Assert.Equal(default, stats.LastPublish);
    }

    [Fact]
    public void GetNotificationPublishStats_WithData_ReturnsCorrectStats()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var metrics1 = new NotificationPublishMetrics
        {
            NotificationType = typeof(string),
            HandlerCount = 3,
            Duration = TimeSpan.FromMilliseconds(50),
            Success = true,
            Timestamp = timestamp
        };
        var metrics2 = new NotificationPublishMetrics
        {
            NotificationType = typeof(string),
            HandlerCount = 5,
            Duration = TimeSpan.FromMilliseconds(100),
            Success = false,
            Timestamp = timestamp.AddSeconds(1)
        };

        _provider.RecordNotificationPublish(metrics1);
        _provider.RecordNotificationPublish(metrics2);

        // Act
        var stats = _provider.GetNotificationPublishStats(typeof(string));

        // Assert
        Assert.Equal(2, stats.TotalPublishes);
        Assert.Equal(1, stats.SuccessfulPublishes);
        Assert.Equal(1, stats.FailedPublishes);
        Assert.Equal(TimeSpan.FromMilliseconds(75), stats.AveragePublishTime); // (50+100)/2
        Assert.Equal(TimeSpan.FromMilliseconds(50), stats.MinPublishTime);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.MaxPublishTime);
        Assert.Equal(4, stats.AverageHandlerCount); // (3+5)/2
        Assert.Equal(timestamp.AddSeconds(1), stats.LastPublish);
    }

    [Fact]
    public void GetStreamingOperationStats_WithNoData_ReturnsEmptyStats()
    {
        // Act
        var stats = _provider.GetStreamingOperationStats(typeof(string));

        // Assert
        Assert.Equal(typeof(string), stats.RequestType);
        Assert.Null(stats.HandlerName);
        Assert.Equal(0, stats.TotalOperations);
        Assert.Equal(0, stats.SuccessfulOperations);
        Assert.Equal(0, stats.FailedOperations);
        Assert.Equal(TimeSpan.Zero, stats.AverageOperationTime);
        Assert.Equal(0, stats.TotalItemsStreamed);
        Assert.Equal(0, stats.AverageItemsPerOperation);
        Assert.Equal(0, stats.ItemsPerSecond);
        Assert.Equal(default, stats.LastOperation);
    }

    [Fact]
    public void GetStreamingOperationStats_WithData_ReturnsCorrectStats()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var metrics1 = new StreamingOperationMetrics
        {
            RequestType = typeof(string),
            ResponseType = typeof(int),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(100),
            ItemCount = 50,
            Success = true,
            Timestamp = timestamp
        };
        var metrics2 = new StreamingOperationMetrics
        {
            RequestType = typeof(string),
            ResponseType = typeof(int),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(200),
            ItemCount = 100,
            Success = true,
            Timestamp = timestamp.AddSeconds(1)
        };

        _provider.RecordStreamingOperation(metrics1);
        _provider.RecordStreamingOperation(metrics2);

        // Act
        var stats = _provider.GetStreamingOperationStats(typeof(string));

        // Assert
        Assert.Equal(2, stats.TotalOperations);
        Assert.Equal(2, stats.SuccessfulOperations);
        Assert.Equal(0, stats.FailedOperations);
        Assert.Equal(TimeSpan.FromMilliseconds(150), stats.AverageOperationTime); // (100+200)/2
        Assert.Equal(150, stats.TotalItemsStreamed); // 50+100
        Assert.Equal(75, stats.AverageItemsPerOperation); // (50+100)/2
        Assert.Equal(150 / 0.3, stats.ItemsPerSecond); // 150 items / 0.3 seconds
        Assert.Equal(timestamp.AddSeconds(1), stats.LastOperation);
    }

    [Fact]
    public void DetectAnomalies_ReturnsAnomaliesWithinLookbackPeriod()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var oldAnomaly = new PerformanceAnomaly
        {
            Type = AnomalyType.SlowExecution,
            Description = "Old anomaly",
            DetectedAt = now.AddHours(-2)
        };
        var recentAnomaly = new PerformanceAnomaly
        {
            Type = AnomalyType.HighFailureRate,
            Description = "Recent anomaly",
            DetectedAt = now.AddMinutes(-30)
        };

        _provider.AddAnomaly(oldAnomaly);
        _provider.AddAnomaly(recentAnomaly);

        // Act
        var anomalies = _provider.DetectAnomalies(TimeSpan.FromHours(1)).ToList();

        // Assert
        Assert.Single(anomalies);
        Assert.Contains(recentAnomaly, anomalies);
        Assert.DoesNotContain(oldAnomaly, anomalies);
    }

    [Fact]
    public void GetTimingBreakdown_WhenExists_ReturnsBreakdown()
    {
        // Arrange
        var breakdown = new TimingBreakdown
        {
            OperationId = "test-op",
            TotalDuration = TimeSpan.FromMilliseconds(100),
            PhaseTimings = new System.Collections.Generic.Dictionary<string, TimeSpan>
            {
                ["step1"] = TimeSpan.FromMilliseconds(30),
                ["step2"] = TimeSpan.FromMilliseconds(70)
            }
        };
        _provider.RecordTimingBreakdown(breakdown);

        // Act
        var result = _provider.GetTimingBreakdown("test-op");

        // Assert
        Assert.Equal(breakdown, result);
    }

    [Fact]
    public void GetTimingBreakdown_WhenNotExists_ReturnsEmptyBreakdown()
    {
        // Act
        var result = _provider.GetTimingBreakdown("nonexistent");

        // Assert
        Assert.Equal("nonexistent", result.OperationId);
        Assert.Equal(TimeSpan.Zero, result.TotalDuration);
        Assert.Empty(result.PhaseTimings);
    }

    [Fact]
    public void RecordTimingBreakdown_AddsToDictionary()
    {
        // Arrange
        var breakdown = new TimingBreakdown { OperationId = "test-op" };

        // Act
        _provider.RecordTimingBreakdown(breakdown);

        // Assert
        Assert.Contains("test-op", _provider.TimingBreakdowns.Keys);
        Assert.Equal(breakdown, _provider.TimingBreakdowns["test-op"]);
    }

    [Fact]
    public void AddAnomaly_AddsToDetectedAnomalies()
    {
        // Arrange
        var anomaly = new PerformanceAnomaly
        {
            Type = AnomalyType.MemorySpike,
            Description = "Test anomaly"
        };

        // Act
        _provider.AddAnomaly(anomaly);

        // Assert
        Assert.Single(_provider.DetectedAnomalies);
        Assert.Contains(anomaly, _provider.DetectedAnomalies);
    }

    [Fact]
    public void AddTimingBreakdown_AddsToDictionary()
    {
        // Arrange
        var breakdown = new TimingBreakdown { OperationId = "test-op" };

        // Act
        _provider.AddTimingBreakdown(breakdown);

        // Assert
        Assert.Contains("test-op", _provider.TimingBreakdowns.Keys);
        Assert.Equal(breakdown, _provider.TimingBreakdowns["test-op"]);
    }
}