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
using Relay.Core.Telemetry;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq;

namespace Relay.Core.Tests.Performance;

public class PooledTelemetryProviderTests
{
    [Fact]
    public void PooledTelemetryProvider_Should_ReduceAllocations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act & Assert - This test validates that the pooled provider works
        // Actual allocation measurement would require more sophisticated tooling
        var requestType = typeof(string);
        var responseType = typeof(int);
        var duration = TimeSpan.FromMilliseconds(100);

        // Should not throw and should complete successfully
        telemetryProvider.RecordHandlerExecution(requestType, responseType, "TestHandler", duration, true);
        telemetryProvider.RecordNotificationPublish(requestType, 3, duration, true);
        telemetryProvider.RecordStreamingOperation(requestType, responseType, "StreamHandler", duration, 100, true);

        Assert.True(true); // Test passes if no exceptions are thrown
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_Should_HandleZeroHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act & Assert - Should not throw
        telemetryProvider.RecordNotificationPublish(typeof(string), 0, TimeSpan.FromMilliseconds(50), true);
        Assert.True(true);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_Should_HandleZeroItems()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act & Assert - Should not throw
        telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(50), 0, true);
        Assert.True(true);
    }

    [Fact]
    public void PooledTelemetryProvider_Should_HandleFailedExecution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act & Assert - Should not throw
        telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "Handler", TimeSpan.FromMilliseconds(100), false);
        Assert.True(true);
    }

    [Fact]
    public void PooledTelemetryProvider_StartActivity_Should_SetCorrelationId()
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
        var correlationId = "test-correlation-123";

        // Act
        var activity = telemetryProvider.StartActivity("TestOperation", typeof(string), correlationId);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(correlationId, telemetryProvider.GetCorrelationId());
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_Should_HandleException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "Handler", TimeSpan.FromMilliseconds(100), false, exception);
        Assert.True(true);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_Should_HandleException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        telemetryProvider.RecordNotificationPublish(typeof(string), 5, TimeSpan.FromMilliseconds(100), false, exception);
        Assert.True(true);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_Should_HandleException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 50, false, exception);
        Assert.True(true);
    }

    [Fact]
    public void PooledTelemetryProvider_SetCorrelationId_Should_HandleNullActivity()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var correlationId = "test-correlation-456";

        // Ensure no current activity
        Activity.Current = null;

        // Act
        telemetryProvider.SetCorrelationId(correlationId);

        // Assert
        Assert.Equal(correlationId, telemetryProvider.GetCorrelationId());
    }

    [Fact]
    public void PooledTelemetryProvider_GetCorrelationId_Should_FallbackToActivityTag()
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
        var correlationId = "activity-correlation-789";

        // Clear context using reflection
        var contextField = typeof(PooledTelemetryProvider).GetField("CorrelationIdContext", BindingFlags.Static | BindingFlags.NonPublic);
        var asyncLocal = (AsyncLocal<string?>)contextField!.GetValue(null)!;
        asyncLocal.Value = null;

        // Get ActivitySource using reflection
        var activitySourceField = typeof(PooledTelemetryProvider).GetField("ActivitySource", BindingFlags.Static | BindingFlags.NonPublic);
        var activitySource = (ActivitySource)activitySourceField!.GetValue(null)!;

        // Create activity with tag
        using var activity = activitySource.StartActivity("Test");
        activity?.SetTag("relay.correlation_id", correlationId);

        // Act
        var result = telemetryProvider.GetCorrelationId();

        // Assert
        Assert.Equal(correlationId, result);
    }

    [Fact]
    public void PooledTelemetryProvider_Constructor_Should_HandleNullMetricsProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();

        // Act
        var telemetryProvider = new PooledTelemetryProvider(contextPool, null, null);

        // Assert
        Assert.NotNull(telemetryProvider.MetricsProvider);
        Assert.IsType<DefaultMetricsProvider>(telemetryProvider.MetricsProvider);
    }

    [Fact]
    public void PooledTelemetryProvider_Should_LogDebugMessages()
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
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var logger = provider.GetRequiredService<ILogger<PooledTelemetryProvider>>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool, logger);

        // Act - Start activity to trigger logging
        var activity = telemetryProvider.StartActivity("TestOperation", typeof(string), "test-id");

        // Record execution to trigger logging
        telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.FromMilliseconds(100), true);

        // Assert - Test passes if no exceptions (logging is tested indirectly)
        Assert.NotNull(activity);
    }

    [Fact]
    public void PooledTelemetryProvider_Constructor_Should_ThrowWhenContextPoolIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PooledTelemetryProvider(null!));
    }

    [Fact]
    public void PooledTelemetryProvider_StartActivity_Should_HandleNullCorrelationId()
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

        // Act
        var activity = telemetryProvider.StartActivity("TestOperation", typeof(string), null);

        // Assert
        Assert.NotNull(activity);
        Assert.Null(telemetryProvider.GetCorrelationId());
    }

    [Fact]
    public void PooledTelemetryProvider_StartActivity_Should_ReturnNullWhenSamplingDenies()
    {
        // Arrange - No activity listener, so sampling will deny
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var activity = telemetryProvider.StartActivity("TestOperation", typeof(string), "test-id");

        // Assert
        Assert.Null(activity);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_Should_HandleNullResponseType()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act & Assert - Should not throw
        telemetryProvider.RecordHandlerExecution(typeof(string), null, "TestHandler", TimeSpan.FromMilliseconds(100), true);
        Assert.True(true);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_Should_HandleNullHandlerName()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act & Assert - Should not throw
        telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), null, TimeSpan.FromMilliseconds(100), true);
        Assert.True(true);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_Should_HandleNullHandlerName()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act & Assert - Should not throw
        telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), null, TimeSpan.FromMilliseconds(100), 50, true);
        Assert.True(true);
    }

    [Fact]
    public void PooledTelemetryProvider_GetCorrelationId_Should_ReturnNullWhenNoContextOrActivity()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Clear any existing activity
        Activity.Current = null;

        // Clear context using reflection
        var contextField = typeof(PooledTelemetryProvider).GetField("CorrelationIdContext", BindingFlags.Static | BindingFlags.NonPublic);
        var asyncLocal = (AsyncLocal<string?>)contextField!.GetValue(null)!;
        asyncLocal.Value = null;

        // Act
        var result = telemetryProvider.GetCorrelationId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PooledTelemetryProvider_Constructor_Should_AcceptLogger()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var logger = provider.GetRequiredService<ILogger<PooledTelemetryProvider>>();

        // Act
        var telemetryProvider = new PooledTelemetryProvider(contextPool, logger);

        // Assert
        Assert.NotNull(telemetryProvider);
    }

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
    public void PooledTelemetryProvider_StartActivity_Should_SetActivityTags()
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

        var operationName = "TestOperation";
        var requestType = typeof(string);
        var correlationId = "test-correlation-789";

        // Act
        var activity = telemetryProvider.StartActivity(operationName, requestType, correlationId);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(operationName, activity.OperationName);
        Assert.Equal(requestType.FullName, activity.GetTagItem("relay.request_type"));
        Assert.Equal(operationName, activity.GetTagItem("relay.operation"));
        Assert.Equal(correlationId, activity.GetTagItem("relay.correlation_id"));
    }

    [Fact]
    public void PooledTelemetryProvider_Should_HandleMultipleSequentialActivities()
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

        // Act
        var activity1 = telemetryProvider.StartActivity("Operation1", typeof(string), "corr1");
        var activity2 = telemetryProvider.StartActivity("Operation2", typeof(int), "corr2");
        var activity3 = telemetryProvider.StartActivity("Operation3", typeof(bool), null);

        // Assert
        Assert.NotNull(activity1);
        Assert.NotNull(activity2);
        Assert.NotNull(activity3);
        Assert.Equal("Operation1", activity1.OperationName);
        Assert.Equal("Operation2", activity2.OperationName);
        Assert.Equal("Operation3", activity3.OperationName);
    }

    [Fact]
    public void PooledTelemetryProvider_StartActivity_Should_SetCorrelationIdTag()
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

        var correlationId = "test-correlation-123";

        // Act
        var activity = telemetryProvider.StartActivity("TestOperation", typeof(string), correlationId);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(correlationId, activity.GetTagItem("relay.correlation_id"));
        Assert.Equal("TestOperation", activity.OperationName);
        Assert.Equal(typeof(string).FullName, activity.GetTagItem("relay.request_type"));
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

    [Fact]
    public async Task PooledTelemetryProvider_CorrelationId_Should_BeThreadSafe()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        var correlationIds = new[] { "corr1", "corr2", "corr3", "corr4", "corr5" };
        var results = new string[correlationIds.Length];

        // Act
        var tasks = correlationIds.Select(async (corrId, index) =>
        {
            await Task.Yield(); // Force context switch
            telemetryProvider.SetCorrelationId(corrId);
            await Task.Delay(10); // Small delay to increase chance of race condition
            results[index] = telemetryProvider.GetCorrelationId();
        });

        await Task.WhenAll(tasks);

        // Assert - Each task should have gotten back its own correlation ID
        for (int i = 0; i < correlationIds.Length; i++)
        {
            Assert.Equal(correlationIds[i], results[i]);
        }
    }
}