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

public class PooledTelemetryProviderRecordingTests
{
    [Fact]
    public void PooledTelemetryProvider_Should_RecordOperationsSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        var requestType = typeof(string);
        var responseType = typeof(int);
        var duration = TimeSpan.FromMilliseconds(100);

        // Act - Record various operations
        var handlerExecutionResult = Record.Exception(() =>
            telemetryProvider.RecordHandlerExecution(requestType, responseType, "TestHandler", duration, true));
        var notificationPublishResult = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(requestType, 3, duration, true));
        var streamingOperationResult = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(requestType, responseType, "StreamHandler", duration, 100, true));

        // Assert - All operations should complete without throwing exceptions
        Assert.Null(handlerExecutionResult);
        Assert.Null(notificationPublishResult);
        Assert.Null(streamingOperationResult);
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

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 0, TimeSpan.FromMilliseconds(50), true));

        // Assert - Should not throw
        Assert.Null(exception);
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

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(50), 0, true));

        // Assert - Should not throw
        Assert.Null(exception);
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

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "Handler", TimeSpan.FromMilliseconds(100), false));

        // Assert - Should not throw
        Assert.Null(exception);
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
        var testException = new InvalidOperationException("Test exception");

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "Handler", TimeSpan.FromMilliseconds(100), false, testException));

        // Assert - Should not throw
        Assert.Null(exception);
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
        var testException = new InvalidOperationException("Test exception");

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordNotificationPublish(typeof(string), 5, TimeSpan.FromMilliseconds(100), false, testException));

        // Assert - Should not throw
        Assert.Null(exception);
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
        var testException = new InvalidOperationException("Test exception");

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 50, false, testException));

        // Assert - Should not throw
        Assert.Null(exception);
    }
}
