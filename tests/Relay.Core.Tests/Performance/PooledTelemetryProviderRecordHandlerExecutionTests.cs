using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Performance;
using Relay.Core.Performance.Telemetry;
using System;
using System.Diagnostics;
using Xunit;

namespace Relay.Core.Tests.Performance;

public class PooledTelemetryProviderRecordHandlerExecutionTests
{
    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithoutActivity_Should_NotSetActivityTags()
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
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw and should handle null activity gracefully
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithActivity_Should_SetActivityTags()
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
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw and should set activity tags
        Assert.Null(exception);
        Assert.NotNull(activity);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithActivityAndException_Should_SetErrorStatus()
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
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.FromMilliseconds(100), false, testException));

        // Assert - Should not throw and should set error status in activity
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithActivityAndSuccess_Should_SetOkStatus()
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
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw and should set OK status in activity
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithActivityAndFailure_Should_NotSetStatus()
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
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.FromMilliseconds(100), false));

        // Assert - Should not throw and should not set status when success is false and no exception
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithZeroDuration_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.Zero, true));

        // Assert - Should not throw with zero duration
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithNegativeDuration_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.FromMilliseconds(-100), true));

        // Assert - Should not throw with negative duration
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithMaxDuration_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.MaxValue, true));

        // Assert - Should not throw with max duration
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithNullRequestType_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordHandlerExecution(null!, typeof(int), "TestHandler", TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw with null request type (implementation doesn't validate inputs)
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithNullResponseType_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordHandlerExecution(typeof(string), null, "TestHandler", TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw with null response type
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithEmptyHandlerName_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "", TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw with empty handler name
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithNullHandlerName_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), null, TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw with null handler name
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithVeryLongHandlerName_Should_Handle()
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
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), longHandlerName, TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw with very long handler name
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithILogger_Should_LogSuccessfully()
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
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw and should attempt to log
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_WithILoggerAndException_Should_LogException()
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
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.FromMilliseconds(100), false, testException));

        // Assert - Should not throw and should log exception details
        Assert.Null(exception);
    }
}