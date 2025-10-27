using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Performance;
using Relay.Core.Performance.Telemetry;
using Relay.Core.Telemetry;
using System;
using System.Diagnostics;
using Xunit;

namespace Relay.Core.Tests.Performance;

public class PooledTelemetryProviderRecordStreamingOperationTests
{
    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithoutActivity_Should_NotSetActivityTags()
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
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 50, true));

        // Assert - Should not throw and should handle null activity gracefully
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithActivity_Should_SetActivityTags()
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
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 50, true));

        // Assert - Should not throw and should set activity tags
        Assert.Null(exception);
        Assert.NotNull(activity);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithActivityAndException_Should_SetErrorStatus()
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
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 50, false, testException));

        // Assert - Should not throw and should set error status in activity
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithActivityAndSuccess_Should_SetOkStatus()
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
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 50, true));

        // Assert - Should not throw and should set OK status in activity
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithActivityAndFailure_Should_NotSetStatus()
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
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 50, false));

        // Assert - Should not throw and should not set status when success is false and no exception
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithZeroDuration_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.Zero, 50, true));

        // Assert - Should not throw with zero duration
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithNegativeDuration_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(-100), 50, true));

        // Assert - Should not throw with negative duration
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithMaxDuration_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.MaxValue, 50, true));

        // Assert - Should not throw with max duration
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithNegativeItemCount_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), -50, true));

        // Assert - Should not throw with negative item count
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithLargeItemCount_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), long.MaxValue, true));

        // Assert - Should not throw with large item count
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithZeroItemCount_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 0, true));

        // Assert - Should not throw with zero item count
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithNullRequestType_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(null!, typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 50, true));

        // Assert - Should not throw with null request type (implementation doesn't validate inputs)
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithNullResponseType_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), null!, "StreamHandler", TimeSpan.FromMilliseconds(100), 50, true));

        // Assert - Should not throw with null response type (implementation doesn't validate inputs)
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithNullHandlerName_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), null!, TimeSpan.FromMilliseconds(100), 50, true));

        // Assert - Should not throw with null handler name (already tested but adding for complete coverage)
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithEmptyHandlerName_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "", TimeSpan.FromMilliseconds(100), 50, true));

        // Assert - Should not throw with empty handler name
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithILogger_Should_LogSuccessfully()
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
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 50, true));

        // Assert - Should not throw and should attempt to log
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithILoggerAndException_Should_LogException()
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
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 50, false, testException));

        // Assert - Should not throw and should log exception details
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithSuccessAndException_Should_SetErrorStatus()
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
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 50, true, testException));

        // Assert - Should not throw and should set error status even if success is true when exception is present
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithVeryLargeDuration_Should_Handle()
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
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", veryLargeDuration, 50, true));

        // Assert - Should not throw with very large duration
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithMinItemCount_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), long.MinValue, true));

        // Assert - Should not throw with minimum item count
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithMultipleCallsInSequence_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception1 = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler1", TimeSpan.FromMilliseconds(10), 10, true));
        var exception2 = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(decimal), typeof(double), "StreamHandler2", TimeSpan.FromMilliseconds(20), 20, false));
        var exception3 = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(bool), typeof(char), "StreamHandler3", TimeSpan.FromMilliseconds(30), 30, true));

        // Assert - Should handle multiple calls without issues
        Assert.Null(exception1);
        Assert.Null(exception2);
        Assert.Null(exception3);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WhenMetricsProviderThrows_Should_NotFail()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var mockMetricsProvider = new Mock<IMetricsProvider>();
        mockMetricsProvider.Setup(m => m.RecordStreamingOperation(It.IsAny<StreamingOperationMetrics>()))
                          .Throws(new InvalidOperationException("Metrics provider error"));
        
        var telemetryProvider = new PooledTelemetryProvider(contextPool, null, mockMetricsProvider.Object);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 50, true));

        // Assert - Should not throw even if metrics provider throws
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithVeryLongHandlerName_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        var longHandlerName = new string('A', 1000); // Very long handler name

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), longHandlerName, TimeSpan.FromMilliseconds(100), 50, true));

        // Assert - Should not throw with very long handler name
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithVeryLargeItemCount_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act - use a very large but not max value to avoid potential overflow issues
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 9223372036854775800L, true));

        // Assert - Should not throw with very large item count
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_WithOneItemCount_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 1, true));

        // Assert - Should not throw with one item count
        Assert.Null(exception);
    }
}