using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry.DefaultTelemetryProviderTests;

/// <summary>
using Relay.Core.Testing;
/// Tests for DefaultTelemetryProvider circuit breaker functionality
/// </summary>
public class DefaultTelemetryProviderCircuitBreakerTests
{
    private readonly Mock<ILogger<global::Relay.Core.Telemetry.DefaultTelemetryProvider>> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;

    public DefaultTelemetryProviderCircuitBreakerTests()
    {
        _loggerMock = new Mock<ILogger<DefaultTelemetryProvider>>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithOpenState_AddsCorrectActivityEvent()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        using var activity = new Activity("test").Start();

        // Act
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Closed", "Open");

        // Assert
        Assert.Contains(activity.Events, e => e.Name == "circuit_breaker.opened");
        Assert.Equal("Open", activity.GetTagItem("relay.circuit_breaker.state"));
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithClosedState_AddsCorrectActivityEvent()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        using var activity = new Activity("test").Start();

        // Act
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Open", "Closed");

        // Assert
        Assert.Contains(activity.Events, e => e.Name == "circuit_breaker.closed");
        Assert.Equal("Closed", activity.GetTagItem("relay.circuit_breaker.state"));
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithHalfOpenState_AddsCorrectActivityEvent()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        using var activity = new Activity("test").Start();

        // Act
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Open", "HalfOpen");

        // Assert
        Assert.Contains(activity.Events, e => e.Name == "circuit_breaker.half_opened");
        Assert.Equal("HalfOpen", activity.GetTagItem("relay.circuit_breaker.state"));
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithUnknownState_AddsGenericActivityEvent()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        using var activity = new Activity("test").Start();

        // Act
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Closed", "UnknownState");

        // Assert
        Assert.Contains(activity.Events, e => e.Name == "circuit_breaker.state_changed");
        Assert.Equal("UnknownState", activity.GetTagItem("relay.circuit_breaker.state"));
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithoutActivity_DoesNotThrow()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);

        // Act & Assert - Should not throw
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Closed", "Open");
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_LogsInformation()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);

        // Act
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Closed", "Open");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Circuit breaker 'TestCircuitBreaker' state changed from Closed to Open")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithSuccess_RecordsActivityTags()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        using var activity = new Activity("test").Start();

        // Act
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", "Check", true);

        // Assert
        Assert.Equal("Check", activity.GetTagItem("relay.circuit_breaker.operation"));
        Assert.Equal(true, activity.GetTagItem("relay.success"));
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithFailure_RecordsActivityTags()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        using var activity = new Activity("test").Start();

        // Act
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", "Check", false);

        // Assert
        Assert.Equal("Check", activity.GetTagItem("relay.circuit_breaker.operation"));
        Assert.Equal(false, activity.GetTagItem("relay.success"));
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithException_RecordsExceptionTags()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        using var activity = new Activity("test").Start();
        var exception = new InvalidOperationException("Test exception");

        // Act
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", "Check", false, exception);

        // Assert
        Assert.Equal("System.InvalidOperationException", activity.GetTagItem("error.type"));
        Assert.Equal("Test exception", activity.GetTagItem("error.message"));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Test exception", activity.StatusDescription);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithoutActivity_DoesNotThrow()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);

        // Act & Assert - Should not throw
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", "Check", true);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_LogsDebug()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);

        // Act
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", "Check", true);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Circuit breaker 'TestCircuitBreaker' operation 'Check' (Success: True)")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
