using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
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
    }
}