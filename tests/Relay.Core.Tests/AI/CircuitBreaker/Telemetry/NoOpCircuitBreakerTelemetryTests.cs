using Relay.Core.AI.CircuitBreaker;
using Relay.Core.AI.CircuitBreaker.Metrics;
using Relay.Core.AI.CircuitBreaker.Telemetry;
using System;
using Xunit;

namespace Relay.Core.Tests.AI.CircuitBreaker.Telemetry;

public class NoOpCircuitBreakerTelemetryTests
{
    [Fact]
    public void Instance_Should_Be_Singleton()
    {
        // Act
        var instance1 = NoOpCircuitBreakerTelemetry.Instance;
        var instance2 = NoOpCircuitBreakerTelemetry.Instance;

        // Assert
        Assert.NotNull(instance1);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void RecordStateChange_Should_Not_Throw()
    {
        // Arrange
        var telemetry = NoOpCircuitBreakerTelemetry.Instance;

        // Act & Assert - Should not throw
        telemetry.RecordStateChange(CircuitBreakerState.Closed, CircuitBreakerState.Open, "Test reason");
    }

    [Fact]
    public void RecordSuccess_Should_Not_Throw()
    {
        // Arrange
        var telemetry = NoOpCircuitBreakerTelemetry.Instance;
        var duration = TimeSpan.FromMilliseconds(100);

        // Act & Assert - Should not throw
        telemetry.RecordSuccess(duration, false);
        telemetry.RecordSuccess(duration, true);
    }

    [Fact]
    public void RecordFailure_Should_Not_Throw()
    {
        // Arrange
        var telemetry = NoOpCircuitBreakerTelemetry.Instance;
        var exception = new Exception("Test exception");
        var duration = TimeSpan.FromMilliseconds(100);

        // Act & Assert - Should not throw
        telemetry.RecordFailure(exception, duration, false);
        telemetry.RecordFailure(exception, duration, true);
    }

    [Fact]
    public void RecordRejectedCall_Should_Not_Throw()
    {
        // Arrange
        var telemetry = NoOpCircuitBreakerTelemetry.Instance;

        // Act & Assert - Should not throw
        telemetry.RecordRejectedCall(CircuitBreakerState.Open);
    }

    [Fact]
    public void UpdateMetrics_Should_Not_Throw()
    {
        // Arrange
        var telemetry = NoOpCircuitBreakerTelemetry.Instance;
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 10,
            SuccessfulCalls = 8,
            FailedCalls = 2,
            RejectedCalls = 0,
            AverageResponseTimeMs = 50.0,
            LastStateChange = DateTime.UtcNow
        };

        // Act & Assert - Should not throw
        telemetry.UpdateMetrics(metrics);
    }
}