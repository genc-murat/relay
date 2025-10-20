using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
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
}