using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Performance;
using Relay.Core.Performance.Telemetry;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Performance.Extensions;
using System.Threading;
using Relay.Core.Testing;
using Relay.Core.Telemetry;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq;

namespace Relay.Core.Tests.Performance;

public class PooledTelemetryProviderMetricsTests
{
    [Fact]
    public void PooledTelemetryProvider_Should_RecordHandlerExecutionMetrics()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var metricsProviderMock = new Mock<IMetricsProvider>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool, null, metricsProviderMock.Object);

        var requestType = typeof(string);
        var responseType = typeof(int);
        var handlerName = "TestHandler";
        var duration = TimeSpan.FromMilliseconds(150);
        var exception = new InvalidOperationException("Test exception");

        // Act
        telemetryProvider.RecordHandlerExecution(requestType, responseType, handlerName, duration, false, exception);

        // Assert
        metricsProviderMock.Verify(m => m.RecordHandlerExecution(It.Is<HandlerExecutionMetrics>(metrics =>
            metrics.RequestType == requestType &&
            metrics.ResponseType == responseType &&
            metrics.HandlerName == handlerName &&
            metrics.Duration == duration &&
            metrics.Success == false &&
            metrics.Exception == exception
        )), Times.Once);
    }

    [Fact]
    public void PooledTelemetryProvider_Should_RecordNotificationPublishMetrics()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var metricsProviderMock = new Mock<IMetricsProvider>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool, null, metricsProviderMock.Object);

        var notificationType = typeof(string);
        var handlerCount = 5;
        var duration = TimeSpan.FromMilliseconds(200);
        var exception = new InvalidOperationException("Test exception");

        // Act
        telemetryProvider.RecordNotificationPublish(notificationType, handlerCount, duration, false, exception);

        // Assert
        metricsProviderMock.Verify(m => m.RecordNotificationPublish(It.Is<NotificationPublishMetrics>(metrics =>
            metrics.NotificationType == notificationType &&
            metrics.HandlerCount == handlerCount &&
            metrics.Duration == duration &&
            metrics.Success == false &&
            metrics.Exception == exception
        )), Times.Once);
    }

    [Fact]
    public void PooledTelemetryProvider_Should_RecordStreamingOperationMetrics()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var metricsProviderMock = new Mock<IMetricsProvider>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool, null, metricsProviderMock.Object);

        var requestType = typeof(string);
        var responseType = typeof(int);
        var handlerName = "StreamHandler";
        var duration = TimeSpan.FromMilliseconds(300);
        var itemCount = 100L;
        var exception = new InvalidOperationException("Test exception");

        // Act
        telemetryProvider.RecordStreamingOperation(requestType, responseType, handlerName, duration, itemCount, false, exception);

        // Assert
        metricsProviderMock.Verify(m => m.RecordStreamingOperation(It.Is<StreamingOperationMetrics>(metrics =>
            metrics.RequestType == requestType &&
            metrics.ResponseType == responseType &&
            metrics.HandlerName == handlerName &&
            metrics.Duration == duration &&
            metrics.ItemCount == itemCount &&
            metrics.Success == false &&
            metrics.Exception == exception
        )), Times.Once);
    }

    [Fact]
    public void PooledTelemetryProvider_Should_HandleExceptionDuringMetricsRecording()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var metricsProviderMock = new Mock<IMetricsProvider>();
        metricsProviderMock.Setup(m => m.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()))
                          .Throws(new InvalidOperationException("Metrics recording failed"));
        var telemetryProvider = new PooledTelemetryProvider(contextPool, null, metricsProviderMock.Object);

        // Act & Assert - Should not throw, should handle exception gracefully
        var exception = Record.Exception(() =>
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "Handler", TimeSpan.FromMilliseconds(100), true));

        Assert.Null(exception); // Should not throw
    }
}
