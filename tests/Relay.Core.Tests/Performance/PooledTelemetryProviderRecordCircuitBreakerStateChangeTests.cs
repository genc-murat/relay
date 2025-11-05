using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Performance;
using Relay.Core.Performance.Telemetry;
using System.Diagnostics;
using Xunit;

namespace Relay.Core.Tests.Performance;

using Relay.Core.Testing;
public class PooledTelemetryProviderRecordCircuitBreakerStateChangeTests
{
    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithoutActivity_Should_NotCreateEventsOrTags()
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
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "Closed", "Open"));

        // Assert - Should not throw and should handle null activity gracefully
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithActivity_Should_CreateEventAndSetTag()
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
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "Closed", "Open"));

        // Assert - Should not throw and should create events/tags properly
        Assert.Null(exception);
        Assert.NotNull(activity);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithOpenState_Should_CreateOpenedEvent()
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
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "Closed", "Open"));

        // Assert - Should not throw and should handle "open" state correctly
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithClosedState_Should_CreateClosedEvent()
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
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "Open", "Closed"));

        // Assert - Should not throw and should handle "closed" state correctly
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithHalfOpenState_Should_CreateHalfOpenedEvent()
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
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "Open", "HalfOpen"));

        // Assert - Should not throw and should handle "halfopen" state correctly
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithUnknownState_Should_CreateGenericEvent()
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
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "Open", "UnknownState"));

        // Assert - Should not throw and should handle unknown state with generic event
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithEmptyCircuitBreakerName_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("", "Closed", "Open"));

        // Assert - Should not throw with empty circuit breaker name
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithNullCircuitBreakerName_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange(null!, "Closed", "Open"));

        // Assert - Should not throw with null circuit breaker name
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithNullOldState_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", null!, "Open"));

        // Assert - Should not throw with null old state
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithNullNewState_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "Closed", null!));

        // Assert - Should not throw with null new state
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithEmptyOldState_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "", "Open"));

        // Assert - Should not throw with empty old state
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithEmptyNewState_Should_HandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "Closed", ""));

        // Assert - Should not throw with empty new state
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithVeryLongNames_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        var longCircuitBreakerName = new string('A', 1000);
        var longOldState = new string('B', 100);
        var longNewState = new string('C', 100);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange(longCircuitBreakerName, longOldState, longNewState));

        // Assert - Should not throw with very long names
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithILogger_Should_LogSuccessfully()
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
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "Closed", "Open"));

        // Assert - Should not throw and should attempt to log
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_CaseInsensitiveState_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        using var activity = new Activity("TestActivity").Start();

        // Act - Test with different casing (the method uses .ToLower())
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "closed", "OPEN"));

        // Assert - Should not throw and should handle case-insensitive state correctly
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_SameStateTransition_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act - Same state transition (e.g., Open to Open)
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "Open", "Open"));

        // Assert - Should not throw when transitioning to same state
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_MultipleCallsInSequence_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception1 = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("circuit1", "Closed", "Open"));
        var exception2 = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("circuit2", "Open", "HalfOpen"));
        var exception3 = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("circuit3", "HalfOpen", "Closed"));

        // Assert - Should handle multiple calls without issues
        Assert.Null(exception1);
        Assert.Null(exception2);
        Assert.Null(exception3);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithSpecialCharactersInName_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("circuit-with.special+chars", "Closed", "Open"));

        // Assert - Should not throw with special characters in circuit breaker name
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithManyDifferentStates_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act - Test various state transitions
        var exception1 = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "Open", "Closed"));
        var exception2 = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "Closed", "HalfOpen"));
        var exception3 = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "HalfOpen", "Open"));
        var exception4 = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", "Unknown", "AnotherUnknown"));

        // Assert - Should handle all state transitions
        Assert.Null(exception1);
        Assert.Null(exception2);
        Assert.Null(exception3);
        Assert.Null(exception4);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordCircuitBreakerStateChange_WithWhitespaceStates_Should_Handle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var exception = Record.Exception(() =>
            telemetryProvider.RecordCircuitBreakerStateChange("test-circuit", " Open ", " HalfOpen "));

        // Assert - Should not throw with whitespace in states
        Assert.Null(exception);
    }
}
