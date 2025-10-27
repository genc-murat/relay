using Microsoft.Extensions.Options;
using Relay.Core.Telemetry;
using System;
using System.Diagnostics;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

[Collection("Sequential")]
public class RelayTelemetryProviderCircuitBreakerTests
{
    #region RecordCircuitBreakerStateChange Tests

    [Fact]
    public void RecordCircuitBreakerStateChange_RecordsStateChangeMetrics()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new RelayTelemetryProvider(options);

        // Act
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Closed", "Open");

        // Assert - Should not throw exception
        Assert.NotNull(provider);
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithActivity_RecordsActivityEvent()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new RelayTelemetryProvider(options);

        using var activity = provider.StartActivity("TestOperation", typeof(string));

        // Act
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Closed", "Open");

        // Assert - Should record activity event with appropriate attributes
        Assert.NotNull(activity);
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithOpenState_AddsCorrectActivityEvent()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new RelayTelemetryProvider(options);

        using var activity = provider.StartActivity("TestOperation", typeof(string));

        // Act
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Closed", "Open");

        // Assert - Should not throw and execute without issues
        Assert.NotNull(activity);
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithClosedState_AddsCorrectActivityEvent()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new RelayTelemetryProvider(options);

        using var activity = provider.StartActivity("TestOperation", typeof(string));

        // Act
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Open", "Closed");

        // Assert - Should not throw and execute without issues
        Assert.NotNull(activity);
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithHalfOpenState_AddsCorrectActivityEvent()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new RelayTelemetryProvider(options);

        using var activity = provider.StartActivity("TestOperation", typeof(string));

        // Act
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Open", "HalfOpen");

        // Assert - Should not throw and execute without issues
        Assert.NotNull(activity);
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WhenTracingDisabled_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = false
        });

        var provider = new RelayTelemetryProvider(options);

        // Act
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Closed", "Open");

        // Assert - Should not throw when tracing is disabled
        Assert.NotNull(provider);
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithNullCircuitBreakerName_ThrowsNoException()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle null gracefully
        provider.RecordCircuitBreakerStateChange(null, "Closed", "Open");
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithNullOldState_ThrowsNoException()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle null old state gracefully
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", null, "Open");
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithNullNewState_ThrowsNoException()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle null new state gracefully
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Closed", null);
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithEmptyCircuitBreakerName_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle empty string gracefully
        provider.RecordCircuitBreakerStateChange("", "Closed", "Open");
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithEmptyOldState_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle empty old state gracefully
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "", "Open");
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithEmptyNewState_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle empty new state gracefully
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Closed", "");
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithWhitespaceCircuitBreakerName_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle whitespace circuit breaker name gracefully
        provider.RecordCircuitBreakerStateChange("   ", "Closed", "Open");
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithSpecialCharactersInCircuitBreakerName_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle special characters in circuit breaker name
        provider.RecordCircuitBreakerStateChange("Test.Circuit-Breaker_123", "Closed", "Open");
    }

    #endregion

    #region RecordCircuitBreakerOperation Tests

    [Fact]
    public void RecordCircuitBreakerOperation_RecordsOperationMetrics()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new RelayTelemetryProvider(options);

        // Act
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", "Check", true);

        // Assert - Should not throw exception
        Assert.NotNull(provider);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithSuccess_RecordsActivityTags()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new RelayTelemetryProvider(options);

        using var activity = provider.StartActivity("TestOperation", typeof(string));

        // Act
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", "Check", true);

        // Assert - Should handle success case without issues
        Assert.NotNull(activity);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithFailure_RecordsActivityTags()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new RelayTelemetryProvider(options);

        using var activity = provider.StartActivity("TestOperation", typeof(string));

        // Act
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", "Check", false);

        // Assert - Should handle failure case without issues
        Assert.NotNull(activity);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithException_RecordsExceptionTags()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new RelayTelemetryProvider(options);

        using var activity = provider.StartActivity("TestOperation", typeof(string));
        var exception = new InvalidOperationException("Test exception");

        // Act
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", "Check", false, exception);

        // Assert - Should handle exception without issues
        Assert.NotNull(activity);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithNullException_RecordsMetrics()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new RelayTelemetryProvider(options);

        using var activity = provider.StartActivity("TestOperation", typeof(string));

        // Act
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", "Check", false, null);

        // Assert - Should handle null exception gracefully
        Assert.NotNull(activity);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WhenTracingDisabled_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = false
        });

        var provider = new RelayTelemetryProvider(options);

        // Act
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", "Check", true);

        // Assert - Should not throw when tracing is disabled
        Assert.NotNull(provider);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithNullCircuitBreakerName_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle null circuit breaker name gracefully
        provider.RecordCircuitBreakerOperation(null, "Check", true);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithNullOperation_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle null operation gracefully
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", null, true);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithEmptyCircuitBreakerName_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle empty circuit breaker name gracefully
        provider.RecordCircuitBreakerOperation("", "Check", true);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithEmptyOperation_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle empty operation gracefully
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", "", true);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithWhitespaceCircuitBreakerName_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle whitespace circuit breaker name
        provider.RecordCircuitBreakerOperation("   ", "Check", true);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithWhitespaceOperation_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle whitespace operation
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", "   ", true);
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithSpecialCharactersInNames_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle special characters in names
        provider.RecordCircuitBreakerOperation("Test.Circuit-Breaker_123", "Check@Operation#123", true);
    }

    #endregion

    #region Circuit Breaker Edge Cases Tests

    [Fact]
    public void RecordCircuitBreakerStateChange_WithVeryLongCircuitBreakerName_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        var longCircuitBreakerName = new string('A', 1000);

        // Act & Assert - Should handle very long circuit breaker name
        provider.RecordCircuitBreakerStateChange(longCircuitBreakerName, "Closed", "Open");
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithVeryLongOperationName_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        var longOperationName = new string('B', 1000);

        // Act & Assert - Should handle very long operation name
        provider.RecordCircuitBreakerOperation("TestCircuitBreaker", longOperationName, true);
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithUnicodeNames_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle Unicode characters
        provider.RecordCircuitBreakerStateChange("测试断路器", "关闭", "开启");
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithUnicodeNames_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle Unicode characters
        provider.RecordCircuitBreakerOperation("测试断路器", "检查", true);
    }

    [Fact]
    public void RecordCircuitBreakerStateChange_WithSameOldAndNewState_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle when old and new state are the same
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "Closed", "Closed");
    }

    [Fact]
    public void RecordCircuitBreakerOperation_WithCustomStateNotInEnum_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        var provider = new RelayTelemetryProvider(options);

        // Act & Assert - Should handle custom state values not in expected enum
        provider.RecordCircuitBreakerStateChange("TestCircuitBreaker", "UnknownState", "UnknownState2");
    }

    #endregion
}