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

public class PooledTelemetryProviderEdgeCasesTests
{
    [Fact]
    public void PooledTelemetryProvider_RecordHandlerExecution_Should_HandleNullResponseType()
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

        // Assert - Should not throw
        Assert.Null(exception);
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

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), null, TimeSpan.FromMilliseconds(100), true));

        // Assert - Should not throw
        Assert.Null(exception);
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

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), null, TimeSpan.FromMilliseconds(100), 50, true));

        // Assert - Should not throw
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_Should_HandleNegativeDuration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "Handler", TimeSpan.FromMilliseconds(-100), true));

        // Assert - Should not throw even with negative duration
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_Should_HandleEmptyHandlerName()
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
    public void PooledTelemetryProvider_Should_HandleVeryLargeItemCount()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "Handler", TimeSpan.FromMilliseconds(100), long.MaxValue, true));

        // Assert - Should not throw with very large item count
        Assert.Null(exception);
    }
}
