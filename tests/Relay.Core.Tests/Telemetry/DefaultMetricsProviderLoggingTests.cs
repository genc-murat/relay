using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class DefaultMetricsProviderLoggingTests
{
    private readonly Mock<ILogger<DefaultMetricsProvider>> _loggerMock;
    private readonly TestableDefaultMetricsProvider _metricsProvider;

    public DefaultMetricsProviderLoggingTests()
    {
        _loggerMock = new Mock<ILogger<DefaultMetricsProvider>>();
        _metricsProvider = new TestableDefaultMetricsProvider(_loggerMock.Object);
    }

    [Fact]
    public void RecordHandlerExecution_ShouldLogDebugMessage()
    {
        // Arrange
        var metrics = new HandlerExecutionMetrics
        {
            RequestType = typeof(TestRequest<string>),
            ResponseType = typeof(string),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(150),
            Success = true
        };

        // Act
        _metricsProvider.RecordHandlerExecution(metrics);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recorded handler execution: TestRequest`1 -> String in 150ms (Success: True)")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordNotificationPublish_ShouldLogDebugMessage()
    {
        // Arrange
        var metrics = new NotificationPublishMetrics
        {
            NotificationType = typeof(TestNotification),
            HandlerCount = 5,
            Duration = TimeSpan.FromMilliseconds(75),
            Success = true
        };

        // Act
        _metricsProvider.RecordNotificationPublish(metrics);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recorded notification publish: TestNotification to 5 handlers in 75ms (Success: True)")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordStreamingOperation_ShouldLogDebugMessage()
    {
        // Arrange
        var metrics = new StreamingOperationMetrics
        {
            RequestType = typeof(TestStreamRequest<string>),
            ResponseType = typeof(string),
            HandlerName = "StreamHandler",
            Duration = TimeSpan.FromMilliseconds(300),
            ItemCount = 50,
            Success = true
        };

        // Act
        _metricsProvider.RecordStreamingOperation(metrics);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recorded streaming operation: TestStreamRequest`1 -> String (50 items) in 300ms (Success: True)")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}