using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Relay.Core.Testing;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class DefaultMetricsProviderComplexStatsTests
{
    private readonly Mock<ILogger<DefaultMetricsProvider>> _loggerMock;
    private readonly TestableDefaultMetricsProvider _metricsProvider;

    public DefaultMetricsProviderComplexStatsTests()
    {
        _loggerMock = new Mock<ILogger<DefaultMetricsProvider>>();
        _metricsProvider = new TestableDefaultMetricsProvider(_loggerMock.Object);
    }

    [Fact]
    public void GetNotificationPublishStats_WithMultiplePublishes_ShouldCalculateCorrectStats()
    {
        // Arrange
        var publishes = new[]
        {
            new NotificationPublishMetrics
            {
                NotificationType = typeof(TestNotification),
                HandlerCount = 2,
                Duration = TimeSpan.FromMilliseconds(50),
                Success = true
            },
            new NotificationPublishMetrics
            {
                NotificationType = typeof(TestNotification),
                HandlerCount = 3,
                Duration = TimeSpan.FromMilliseconds(75),
                Success = true
            },
            new NotificationPublishMetrics
            {
                NotificationType = typeof(TestNotification),
                HandlerCount = 1,
                Duration = TimeSpan.FromMilliseconds(100),
                Success = false,
                Exception = new Exception("Publish failed")
            }
        };

        // Act
        foreach (var publish in publishes)
        {
            _metricsProvider.RecordNotificationPublish(publish);
        }

        // Assert
        var stats = _metricsProvider.GetNotificationPublishStats(typeof(TestNotification));
        Assert.Equal(3, stats.TotalPublishes);
        Assert.Equal(2, stats.SuccessfulPublishes);
        Assert.Equal(1, stats.FailedPublishes);
        Assert.Equal(2.0 / 3.0, stats.SuccessRate, 3);
        Assert.Equal(TimeSpan.FromMilliseconds(75), stats.AveragePublishTime);
        Assert.Equal(TimeSpan.FromMilliseconds(50), stats.MinPublishTime);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.MaxPublishTime);
        Assert.Equal(2.0, stats.AverageHandlerCount);
    }

    [Fact]
    public void GetStreamingOperationStats_WithMultipleOperations_ShouldCalculateCorrectStats()
    {
        // Arrange
        var operations = new[]
        {
            new StreamingOperationMetrics
            {
                RequestType = typeof(TestStreamRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "StreamHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                ItemCount = 10,
                Success = true
            },
            new StreamingOperationMetrics
            {
                RequestType = typeof(TestStreamRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "StreamHandler",
                Duration = TimeSpan.FromMilliseconds(200),
                ItemCount = 20,
                Success = true
            },
            new StreamingOperationMetrics
            {
                RequestType = typeof(TestStreamRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "StreamHandler",
                Duration = TimeSpan.FromMilliseconds(150),
                ItemCount = 15,
                Success = false,
                Exception = new Exception("Stream failed")
            }
        };

        // Act
        foreach (var operation in operations)
        {
            _metricsProvider.RecordStreamingOperation(operation);
        }

        // Assert
        var stats = _metricsProvider.GetStreamingOperationStats(typeof(TestStreamRequest<string>), "StreamHandler");
        Assert.Equal(3, stats.TotalOperations);
        Assert.Equal(2, stats.SuccessfulOperations);
        Assert.Equal(1, stats.FailedOperations);
        Assert.Equal(2.0 / 3.0, stats.SuccessRate, 3);
        Assert.Equal(TimeSpan.FromMilliseconds(150), stats.AverageOperationTime);
        Assert.Equal(45, stats.TotalItemsStreamed);
        Assert.Equal(15.0, stats.AverageItemsPerOperation);
        Assert.Equal(45.0 / (100 + 200 + 150) * 1000, stats.ItemsPerSecond, 1); // Items per second
    }

    [Fact]
    public void GetStreamingOperationStats_WithZeroDuration_ShouldHandleDivisionByZero()
    {
        // Arrange
        _metricsProvider.RecordStreamingOperation(new StreamingOperationMetrics
        {
            RequestType = typeof(TestStreamRequest<string>),
            ResponseType = typeof(string),
            HandlerName = "StreamHandler",
            Duration = TimeSpan.Zero,
            ItemCount = 10,
            Success = true
        });

        // Act
        var stats = _metricsProvider.GetStreamingOperationStats(typeof(TestStreamRequest<string>), "StreamHandler");

        // Assert
        Assert.Equal(0, stats.ItemsPerSecond);
    }

    [Fact]
    public void GetHandlerExecutionStats_WithMultipleExecutions_ShouldCalculateCorrectStats()
    {
        // Arrange
        var executions = new[]
        {
            new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
            },
            new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(200),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-4)
            },
            new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(150),
                Success = false,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-3)
            },
            new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(300),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-2)
            },
            new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(250),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
            }
        };

        // Act
        foreach (var execution in executions)
        {
            _metricsProvider.RecordHandlerExecution(execution);
        }

        // Assert
        var stats = _metricsProvider.GetHandlerExecutionStats(typeof(TestRequest<string>), "TestHandler");
        Assert.Equal(5, stats.TotalExecutions);
        Assert.Equal(4, stats.SuccessfulExecutions);
        Assert.Equal(1, stats.FailedExecutions);
        Assert.Equal(4.0 / 5.0, stats.SuccessRate, 3);
        Assert.Equal(TimeSpan.FromMilliseconds(200), stats.AverageExecutionTime); // (100+200+150+300+250)/5 = 200
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.MinExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(300), stats.MaxExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(200), stats.P50ExecutionTime); // Median of sorted: 100,150,200,250,300 -> 200
        Assert.Equal(TimeSpan.FromMilliseconds(300), stats.P95ExecutionTime); // 95th percentile
        Assert.Equal(TimeSpan.FromMilliseconds(300), stats.P99ExecutionTime); // 99th percentile
    }
}
