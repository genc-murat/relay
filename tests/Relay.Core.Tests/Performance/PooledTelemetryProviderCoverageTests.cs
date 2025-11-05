using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Performance;
using Relay.Core.Performance.Telemetry;
using Relay.Core.Telemetry;
using System;
using System.Diagnostics;
using Xunit;

using Relay.Core.Testing;
namespace Relay.Core.Tests.Performance;

/// <summary>
/// Additional coverage tests for PooledTelemetryProvider
/// </summary>
public class PooledTelemetryProviderCoverageTests
{
    #region Constructor Tests

    [Fact]
    public void PooledTelemetryProvider_Constructor_Should_Throw_When_ContextPool_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PooledTelemetryProvider(null!));
    }

    [Fact]
    public void PooledTelemetryProvider_Constructor_Should_Create_Instance_With_Null_Logger()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();

        // Act
        var telemetryProvider = new PooledTelemetryProvider(contextPool, null);

        // Assert
        Assert.NotNull(telemetryProvider);
        Assert.NotNull(telemetryProvider.MetricsProvider);
    }

    [Fact]
    public void PooledTelemetryProvider_Constructor_Should_Create_Instance_With_Null_MetricsProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var logger = new Mock<ILogger<PooledTelemetryProvider>>().Object;

        // Act
        var telemetryProvider = new PooledTelemetryProvider(contextPool, logger, null);

        // Assert
        Assert.NotNull(telemetryProvider);
        Assert.NotNull(telemetryProvider.MetricsProvider);
    }

    #endregion

    #region Activity Creation Disabled Tests

    [Fact]
    public void PooledTelemetryProvider_StartActivity_Should_Return_Null_When_Activity_Creation_Disabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Temporarily disable activity creation
        PooledTelemetryProvider.DisableActivityCreation = true;

        try
        {
            // Act
            var activity = telemetryProvider.StartActivity("TestOperation", typeof(string), "correlation-123");

            // Assert
            Assert.Null(activity);
        }
        finally
        {
            // Restore activity creation
            PooledTelemetryProvider.DisableActivityCreation = false;
        }
    }

    #endregion

    #region Correlation ID Tests

    [Fact]
    public void PooledTelemetryProvider_SetCorrelationId_Should_Store_And_Retrieve_CorrelationId()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var correlationId = "test-correlation-id-123";

        // Act
        telemetryProvider.SetCorrelationId(correlationId);
        var retrievedId = telemetryProvider.GetCorrelationId();

        // Assert
        Assert.Equal(correlationId, retrievedId);
    }

    [Fact]
    public void PooledTelemetryProvider_GetCorrelationId_Should_Return_Null_When_No_CorrelationId_Set()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var correlationId = telemetryProvider.GetCorrelationId();

        // Assert
        Assert.Null(correlationId);
    }

    [Fact]
    public void PooledTelemetryProvider_StartActivity_Should_Use_Existing_CorrelationId_From_Context()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Relay.Core",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var correlationId = "existing-correlation-id";

        // Set correlation ID first
        telemetryProvider.SetCorrelationId(correlationId);

        // Act
        var activity = telemetryProvider.StartActivity("TestOperation", typeof(string));

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(correlationId, activity.GetTagItem("relay.correlation_id"));
    }

    #endregion

    #region Metrics Recording Failure Tests

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_Should_Not_Throw_When_MetricsProvider_Throws()
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
        var requestType = typeof(string);
        var duration = TimeSpan.FromMilliseconds(100);
        var success = true;

        // Act & Assert - Should not throw despite metrics provider throwing
        var exception = Record.Exception(() => telemetryProvider.RecordHandlerExecution(requestType, typeof(int), "TestHandler", duration, success));

        // Assert
        Assert.Null(exception); // Should not throw
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_Should_Not_Throw_When_MetricsProvider_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();

        var metricsProviderMock = new Mock<IMetricsProvider>();
        metricsProviderMock.Setup(m => m.RecordNotificationPublish(It.IsAny<NotificationPublishMetrics>()))
                           .Throws(new InvalidOperationException("Metrics recording failed"));

        var telemetryProvider = new PooledTelemetryProvider(contextPool, null, metricsProviderMock.Object);
        var notificationType = typeof(string);
        var handlerCount = 3;
        var duration = TimeSpan.FromMilliseconds(50);
        var success = true;

        // Act & Assert - Should not throw despite metrics provider throwing
        var exception = Record.Exception(() => telemetryProvider.RecordNotificationPublish(notificationType, handlerCount, duration, success));

        // Assert
        Assert.Null(exception); // Should not throw
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_Should_Not_Throw_When_MetricsProvider_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();

        var metricsProviderMock = new Mock<IMetricsProvider>();
        metricsProviderMock.Setup(m => m.RecordStreamingOperation(It.IsAny<StreamingOperationMetrics>()))
                           .Throws(new InvalidOperationException("Metrics recording failed"));

        var telemetryProvider = new PooledTelemetryProvider(contextPool, null, metricsProviderMock.Object);
        var requestType = typeof(string);
        var responseType = typeof(int);
        var duration = TimeSpan.FromMilliseconds(200);
        var itemCount = 100L;
        var success = true;

        // Act & Assert - Should not throw despite metrics provider throwing
        var exception = Record.Exception(() => telemetryProvider.RecordStreamingOperation(requestType, responseType, "TestHandler", duration, itemCount, success));

        // Assert
        Assert.Null(exception); // Should not throw
    }

    #endregion

    #region Context Pool Failure Tests

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_Should_Throw_When_ContextPool_Throws_On_Get()
    {
        // Arrange
        var contextPoolMock = new Mock<ITelemetryContextPool>();
        contextPoolMock.Setup(cp => cp.Get())
                       .Throws(new InvalidOperationException("Context pool failed"));

        var telemetryProvider = new PooledTelemetryProvider(contextPoolMock.Object);
        var requestType = typeof(string);
        var duration = TimeSpan.FromMilliseconds(100);
        var success = true;

        // Act & Assert - Should throw when context pool throws
        Assert.Throws<InvalidOperationException>(() =>
            telemetryProvider.RecordHandlerExecution(requestType, typeof(int), "TestHandler", duration, success));
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_Should_Throw_When_ContextPool_Throws_On_Return()
    {
        // Arrange
        var contextMock = new Mock<TelemetryContext>();
        contextMock.SetupAllProperties();

        var contextPoolMock = new Mock<ITelemetryContextPool>();
        contextPoolMock.Setup(cp => cp.Get())
                       .Returns(contextMock.Object);
        contextPoolMock.Setup(cp => cp.Return(It.IsAny<TelemetryContext>()))
                       .Throws(new InvalidOperationException("Context pool return failed"));

        var telemetryProvider = new PooledTelemetryProvider(contextPoolMock.Object);
        var requestType = typeof(string);
        var duration = TimeSpan.FromMilliseconds(100);
        var success = true;

        // Act & Assert - Should throw when context pool throws on return
        Assert.Throws<InvalidOperationException>(() =>
            telemetryProvider.RecordHandlerExecution(requestType, typeof(int), "TestHandler", duration, success));
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_Should_Handle_Null_Exception_Parameter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var requestType = typeof(string);
        var duration = TimeSpan.FromMilliseconds(100);
        var success = false;

        // Act & Assert - Should handle null exception parameter gracefully
        var exception = Record.Exception(() => telemetryProvider.RecordHandlerExecution(requestType, typeof(int), "TestHandler", duration, success, null));

        // Assert
        Assert.Null(exception); // Should not throw
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_Should_Handle_Null_Exception_Parameter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var notificationType = typeof(string);
        var handlerCount = 3;
        var duration = TimeSpan.FromMilliseconds(50);
        var success = false;

        // Act & Assert - Should handle null exception parameter gracefully
        var exception = Record.Exception(() => telemetryProvider.RecordNotificationPublish(notificationType, handlerCount, duration, success, null));

        // Assert
        Assert.Null(exception); // Should not throw
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_Should_Handle_Null_Exception_Parameter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var requestType = typeof(string);
        var responseType = typeof(int);
        var duration = TimeSpan.FromMilliseconds(200);
        var itemCount = 100L;
        var success = false;

        // Act & Assert - Should handle null exception parameter gracefully
        var exception = Record.Exception(() => telemetryProvider.RecordStreamingOperation(requestType, responseType, "TestHandler", duration, itemCount, success, null));

        // Assert
        Assert.Null(exception); // Should not throw
    }

    #endregion
}
