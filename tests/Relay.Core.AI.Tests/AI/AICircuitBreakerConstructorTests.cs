using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI.CircuitBreaker;
using Relay.Core.AI.CircuitBreaker.Options;
using System;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AICircuitBreakerConstructorTests
{
    private readonly ILogger _logger;

    public AICircuitBreakerConstructorTests()
    {
        _logger = NullLogger.Instance;
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Valid_Parameters()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 3,
            SuccessThreshold = 2,
            Timeout = TimeSpan.FromSeconds(5),
            BreakDuration = TimeSpan.FromSeconds(10),
            HalfOpenMaxCalls = 1
        };

        // Act
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

        // Assert
        Assert.NotNull(circuitBreaker);
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }

    [Fact]
    public void CircuitBreaker_Should_Use_Custom_Name()
    {
        // Arrange
        var customName = "MyCustomCircuitBreaker";
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 5,
            SuccessThreshold = 3,
            Timeout = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromMinutes(1),
            HalfOpenMaxCalls = 3,
            Name = customName
        };

        // Act
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

        // Assert
        Assert.Equal(customName, circuitBreaker.Name);
    }

    [Fact]
    public void CircuitBreaker_Should_Use_Standard_Strategy_By_Default()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 5,
            SuccessThreshold = 3,
            Timeout = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromMinutes(1),
            HalfOpenMaxCalls = 3
        };

        // Act
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

        // Assert - Default policy is Standard
        Assert.Equal(CircuitBreakerPolicy.Standard, options.Policy);
    }

    [Fact]
    public void CircuitBreaker_Should_Support_Percentage_Based_Policy()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 5,
            SuccessThreshold = 3,
            Timeout = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromMinutes(1),
            HalfOpenMaxCalls = 3,
            Policy = CircuitBreakerPolicy.PercentageBased
        };

        // Act
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

        // Assert
        Assert.Equal(CircuitBreakerPolicy.PercentageBased, options.Policy);
    }

    [Fact]
    public void CircuitBreaker_Should_Support_Adaptive_Policy()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 5,
            SuccessThreshold = 3,
            Timeout = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromMinutes(1),
            HalfOpenMaxCalls = 3,
            Policy = CircuitBreakerPolicy.Adaptive
        };

        // Act
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

        // Assert
        Assert.Equal(CircuitBreakerPolicy.Adaptive, options.Policy);
    }

    [Fact]
    public void CircuitBreaker_Should_Enable_Telemetry_By_Default()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 5,
            SuccessThreshold = 3,
            Timeout = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromMinutes(1),
            HalfOpenMaxCalls = 3
        };

        // Assert
        Assert.True(options.EnableTelemetry);
        Assert.True(options.EnableMetrics);
    }

    [Fact]
    public void CircuitBreaker_Should_Be_Disposable()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 5,
            SuccessThreshold = 3,
            Timeout = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromMinutes(1),
            HalfOpenMaxCalls = 3
        };
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

        // Act & Assert - Should not throw
        circuitBreaker.Dispose();
    }

    [Fact]
    public void CircuitBreaker_Should_Use_NoOp_Telemetry_When_Disabled()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 5,
            SuccessThreshold = 3,
            Timeout = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromMinutes(1),
            HalfOpenMaxCalls = 3,
            EnableTelemetry = false
        };

        // Act
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

        // Assert
        Assert.NotNull(circuitBreaker);
        Assert.False(options.EnableTelemetry);
    }

        [Fact]
        public void CircuitBreaker_Should_Accept_Custom_NoOp_Telemetry()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 3
            };

            // Act
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger, Relay.Core.AI.CircuitBreaker.Telemetry.NoOpCircuitBreakerTelemetry.Instance);

            // Assert
            Assert.NotNull(circuitBreaker);
        }

        #region Validation Tests

        [Fact]
        public void Constructor_WithZeroFailureThreshold_ThrowsArgumentException()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 0, // Invalid
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 3
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new AICircuitBreaker<string>(options, _logger));
            Assert.Contains("FailureThreshold must be greater than 0", ex.Message);
            Assert.Equal("FailureThreshold", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithNegativeFailureThreshold_ThrowsArgumentException()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = -1, // Invalid
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 3
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new AICircuitBreaker<string>(options, _logger));
            Assert.Contains("FailureThreshold must be greater than 0", ex.Message);
            Assert.Equal("FailureThreshold", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithZeroSuccessThreshold_ThrowsArgumentException()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 0, // Invalid
                Timeout = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 3
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new AICircuitBreaker<string>(options, _logger));
            Assert.Contains("SuccessThreshold must be greater than 0", ex.Message);
            Assert.Equal("SuccessThreshold", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithZeroTimeout_ThrowsArgumentException()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 3,
                Timeout = TimeSpan.Zero, // Invalid
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 3
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new AICircuitBreaker<string>(options, _logger));
            Assert.Contains("Timeout must be greater than zero", ex.Message);
            Assert.Equal("Timeout", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithNegativeTimeout_ThrowsArgumentException()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(-1), // Invalid
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 3
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new AICircuitBreaker<string>(options, _logger));
            Assert.Contains("Timeout must be greater than zero", ex.Message);
            Assert.Equal("Timeout", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithZeroBreakDuration_ThrowsArgumentException()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.Zero, // Invalid
                HalfOpenMaxCalls = 3
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new AICircuitBreaker<string>(options, _logger));
            Assert.Contains("BreakDuration must be greater than zero", ex.Message);
            Assert.Equal("BreakDuration", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithZeroHalfOpenMaxCalls_ThrowsArgumentException()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 0 // Invalid
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new AICircuitBreaker<string>(options, _logger));
            Assert.Contains("HalfOpenMaxCalls must be greater than 0", ex.Message);
            Assert.Equal("HalfOpenMaxCalls", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithZeroSlowCallThreshold_ThrowsArgumentException()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 3,
                SlowCallThreshold = 0.0 // Invalid (must be > 0)
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new AICircuitBreaker<string>(options, _logger));
            Assert.Contains("SlowCallThreshold must be between 0 and 1", ex.Message);
            Assert.Equal("SlowCallThreshold", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithSlowCallThresholdGreaterThanOne_ThrowsArgumentException()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 3,
                SlowCallThreshold = 1.5 // Invalid (must be <= 1)
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new AICircuitBreaker<string>(options, _logger));
            Assert.Contains("SlowCallThreshold must be between 0 and 1", ex.Message);
            Assert.Equal("SlowCallThreshold", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithMultipleValidationErrors_ThrowsOnFirstError()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 0, // Invalid
                SuccessThreshold = 0, // Invalid
                Timeout = TimeSpan.Zero, // Invalid
                BreakDuration = TimeSpan.Zero, // Invalid
                HalfOpenMaxCalls = 0, // Invalid
                SlowCallThreshold = 2.0 // Invalid
            };

            // Act & Assert - Should throw on first validation error (FailureThreshold)
            var ex = Assert.Throws<ArgumentException>(() => new AICircuitBreaker<string>(options, _logger));
            Assert.Contains("FailureThreshold must be greater than 0", ex.Message);
        }

        #endregion
}