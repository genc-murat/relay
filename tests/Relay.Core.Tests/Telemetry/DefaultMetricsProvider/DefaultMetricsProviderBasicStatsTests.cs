using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Relay.Core.Testing;
using System;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class DefaultMetricsProviderBasicStatsTests
{
    private readonly Mock<ILogger<DefaultMetricsProvider>> _loggerMock;
    private readonly TestableDefaultMetricsProvider _metricsProvider;

    public DefaultMetricsProviderBasicStatsTests()
    {
        _loggerMock = new Mock<ILogger<DefaultMetricsProvider>>();
        _metricsProvider = new TestableDefaultMetricsProvider(_loggerMock.Object);
    }

    [Fact]
    public void GetNotificationPublishStats_WithNoData_ShouldReturnEmptyStats()
    {
        // Act
        var stats = _metricsProvider.GetNotificationPublishStats(typeof(TestNotification));

        // Assert
        Assert.Equal(typeof(TestNotification), stats.NotificationType);
        Assert.Equal(0, stats.TotalPublishes);
        Assert.Equal(0, stats.SuccessfulPublishes);
        Assert.Equal(0, stats.FailedPublishes);
        Assert.Equal(0, stats.SuccessRate);
    }

    [Fact]
    public void GetStreamingOperationStats_WithNoData_ShouldReturnEmptyStats()
    {
        // Act
        var stats = _metricsProvider.GetStreamingOperationStats(typeof(TestStreamRequest<string>), "TestHandler");

        // Assert
        Assert.Equal(typeof(TestStreamRequest<string>), stats.RequestType);
        Assert.Equal("TestHandler", stats.HandlerName);
        Assert.Equal(0, stats.TotalOperations);
        Assert.Equal(0, stats.SuccessfulOperations);
        Assert.Equal(0, stats.FailedOperations);
    }

    [Fact]
    public void RecordHandlerExecution_CleansUpOldEntries_WhenLimitExceeded()
    {
        // Arrange - Record more than MaxRecordsPerHandler (1000) executions
        const int recordCount = 1100;
        const int expectedKeptCount = 1000; // MaxRecordsPerHandler

        for (int i = 0; i < recordCount; i++)
        {
            var metrics = new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow
            };
            _metricsProvider.RecordHandlerExecution(metrics);
        }

        // Act
        var stats = _metricsProvider.GetHandlerExecutionStats(typeof(TestRequest<string>), "TestHandler");

        // Assert - Should keep only the most recent MaxRecordsPerHandler entries
        Assert.Equal(expectedKeptCount, stats.TotalExecutions);
    }

    [Fact]
    public void RecordNotificationPublish_CleansUpOldEntries_WhenLimitExceeded()
    {
        // Arrange - Record more than MaxRecordsPerHandler (1000) publishes
        const int recordCount = 1100;
        const int expectedKeptCount = 1000;

        for (int i = 0; i < recordCount; i++)
        {
            var metrics = new NotificationPublishMetrics
            {
                NotificationType = typeof(TestNotification),
                HandlerCount = 2,
                Duration = TimeSpan.FromMilliseconds(50),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow
            };
            _metricsProvider.RecordNotificationPublish(metrics);
        }

        // Act
        var stats = _metricsProvider.GetNotificationPublishStats(typeof(TestNotification));

        // Assert
        Assert.Equal(expectedKeptCount, stats.TotalPublishes);
    }

    [Fact]
    public void RecordStreamingOperation_CleansUpOldEntries_WhenLimitExceeded()
    {
        // Arrange - Record more than MaxRecordsPerHandler (1000) operations
        const int recordCount = 1100;
        const int expectedKeptCount = 1000;

        for (int i = 0; i < recordCount; i++)
        {
            var metrics = new StreamingOperationMetrics
            {
                RequestType = typeof(TestStreamRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(200),
                ItemCount = 10,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow
            };
            _metricsProvider.RecordStreamingOperation(metrics);
        }

        // Act
        var stats = _metricsProvider.GetStreamingOperationStats(typeof(TestStreamRequest<string>), "TestHandler");

        // Assert
        Assert.Equal(expectedKeptCount, stats.TotalOperations);
    }
}
