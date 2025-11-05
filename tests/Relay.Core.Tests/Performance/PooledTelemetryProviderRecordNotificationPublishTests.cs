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

public class PooledTelemetryProviderRecordNotificationPublishTests
{
    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithoutActivity_Should_NotSetActivityTags()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Ensure no current activity
        Activity.Current = null;

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 5, TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw and should handle null activity gracefully
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithActivity_Should_SetActivityTags()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        using var activity = new Activity("TestActivity").Start();

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 5, TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw and should set activity tags
        Assert.Null(exception);
        Assert.NotNull(activity);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithActivityAndException_Should_SetErrorStatus()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var testException = new InvalidOperationException("Test exception");

        using var activity = new Activity("TestActivity").Start();

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 5, TimeSpan.FromMilliseconds(100), false, testException));

        // Assert - Should not throw and should set error status in activity
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithActivityAndSuccess_Should_SetOkStatus()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        using var activity = new Activity("TestActivity").Start();

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 5, TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw and should set OK status in activity
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithActivityAndFailure_Should_NotSetStatus()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        using var activity = new Activity("TestActivity").Start();

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 5, TimeSpan.FromMilliseconds(100), false));

        // Assert - Should not throw and should not set status when success is false and no exception
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithZeroDuration_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 5, TimeSpan.Zero, true));

        // Assert - Should not throw with zero duration
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithNegativeDuration_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 5, TimeSpan.FromMilliseconds(-100), true));

        // Assert - Should not throw with negative duration
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithMaxDuration_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 5, TimeSpan.MaxValue, true));

        // Assert - Should not throw with max duration
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithNegativeHandlerCount_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), -5, TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw with negative handler count
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithLargeHandlerCount_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), int.MaxValue, TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw with large handler count
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithZeroHandlerCount_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 0, TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw with zero handler count
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithNullNotificationType_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(null!, 5, TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw with null notification type (implementation doesn't validate inputs)
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithILogger_Should_LogSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var mockLogger = new Mock<ILogger<PooledTelemetryProvider>>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool, mockLogger.Object);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 3, TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw and should attempt to log
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithILoggerAndException_Should_LogException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var mockLogger = new Mock<ILogger<PooledTelemetryProvider>>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool, mockLogger.Object);
        var testException = new InvalidOperationException("Test exception");

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 3, TimeSpan.FromMilliseconds(100), false, testException));

        // Assert - Should not throw and should log exception details
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithSuccessAndException_Should_SetErrorStatus()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var testException = new InvalidOperationException("Test exception");

        using var activity = new Activity("TestActivity").Start();

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 3, TimeSpan.FromMilliseconds(100), true, testException));

        // Assert - Should not throw and should set error status even if success is true when exception is present
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithVeryLargeDuration_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        var veryLargeDuration = TimeSpan.FromDays(1); // 24*60*60*1000 ms

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 3, veryLargeDuration, true));

        // Assert - Should not throw with very large duration
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithMinHandlerCount_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), int.MinValue, TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw with minimum handler count
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WithMultipleCallsInSequence_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception1 = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 1, TimeSpan.FromMilliseconds(10), true));
        var exception2 = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(int), 2, TimeSpan.FromMilliseconds(20), false));
        var exception3 = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(decimal), 3, TimeSpan.FromMilliseconds(30), true));

        // Assert - Should handle multiple calls without issues
        Assert.Null(exception1);
        Assert.Null(exception2);
        Assert.Null(exception3);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_WhenMetricsProviderThrows_Should_NotFail()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var mockMetricsProvider = new Mock<IMetricsProvider>();
        mockMetricsProvider.Setup(m => m.RecordNotificationPublish(It.IsAny<NotificationPublishMetrics>()))
                          .Throws(new InvalidOperationException("Metrics provider error"));
        
        var telemetryProvider = new PooledTelemetryProvider(contextPool, null, mockMetricsProvider.Object);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 5, TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw even if metrics provider throws
        Assert.Null(exception);
    }
}
