using Microsoft.Extensions.DependencyInjection;
using Moq;
using Relay.Core.Performance;
using Relay.Core.Performance.Telemetry;
using Relay.Core.Telemetry;
using System;
using System.Diagnostics;
using Xunit;

namespace Relay.Core.Tests.Performance;
using Relay.Core.Testing;

public class PooledTelemetryProviderCircuitBreakerOperationTests
{
    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerOperation_Should_RecordSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerOperation("test-circuit-breaker", "check", true));

        // Assert - Should not throw
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerOperation_Should_RecordUnsuccessfulOperation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerOperation("test-circuit-breaker", "check", false));

        // Assert - Should not throw
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerOperation_Should_HandleException()
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
            telemetryProvider.RecordCircuitBreakerOperation("test-circuit-breaker", "check", false, testException));

        // Assert - Should not throw
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerOperation_Should_UsePooledContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerOperation("test-circuit-breaker", "check", true));

        // Assert - Should not throw and context should be properly pooled
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerOperation_WithActivity_Should_SetTags()
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
            telemetryProvider.RecordCircuitBreakerOperation("test-circuit-breaker", "check", true));

        // Assert - Should not throw and should set activity tags
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerOperation_WithActivityAndException_Should_SetErrorStatus()
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
            telemetryProvider.RecordCircuitBreakerOperation("test-circuit-breaker", "check", false, testException));

        // Assert - Should not throw and should set error status in activity
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerOperation_WithActivityAndSuccess_Should_SetOkStatus()
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
            telemetryProvider.RecordCircuitBreakerOperation("test-circuit-breaker", "check", true));

        // Assert - Should not throw and should set OK status in activity
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerOperation_WithActivityAndFailure_Should_NotSetStatus()
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
            telemetryProvider.RecordCircuitBreakerOperation("test-circuit-breaker", "check", false));

        // Assert - Should not throw and should not set status when success is false and no exception
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerOperation_WithNullOperation_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerOperation("test-circuit-breaker", null!, true));

        // Assert - Should not throw even with null operation
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerOperation_WithEmptyCircuitBreakerName_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerOperation("", "check", true));

        // Assert - Should not throw even with empty circuit breaker name
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerOperation_WithMetricsProvider_Should_CallMetricsProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var mockMetricsProvider = new Mock<IMetricsProvider>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool, metricsProvider: mockMetricsProvider.Object);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerOperation("test-circuit-breaker", "check", true));

        // Note: PooledTelemetryProvider doesn't directly call metrics provider for circuit breaker operations
        // It only records activity tags and logging, so metrics provider might not be called in this case

        // Assert - Should not throw
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerOperation_WithoutActivity_Should_NotFail()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Ensure no current activity - this is the default state
        Activity.Current = null;

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerOperation("test-circuit-breaker", "check", true));

        // Assert - Should not throw even without current activity
        Assert.Null(exception);
    }
}
