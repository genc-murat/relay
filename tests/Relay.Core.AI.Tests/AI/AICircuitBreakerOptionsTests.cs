using System;
using Relay.Core.AI;
using Relay.Core.AI.CircuitBreaker.Options;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AICircuitBreakerOptionsTests
    {
        [Fact]
        public void Options_Should_Validate_Required_Parameters()
        {
            // Arrange & Act & Assert
            var validOptions = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 3
            };

            // Should not throw
            validOptions.Validate();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Options_Should_Throw_On_Invalid_FailureThreshold(int failureThreshold)
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = failureThreshold,
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 3
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Contains("FailureThreshold", exception.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Options_Should_Throw_On_Invalid_SuccessThreshold(int successThreshold)
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = successThreshold,
                Timeout = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 3
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Contains("SuccessThreshold", exception.Message);
        }

        [Fact]
        public void Options_Should_Throw_On_Invalid_Timeout()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 3,
                Timeout = TimeSpan.Zero,
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 3
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Contains("Timeout", exception.Message);
        }

        [Fact]
        public void Options_Should_Throw_On_Invalid_BreakDuration()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.Zero,
                HalfOpenMaxCalls = 3
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Contains("BreakDuration", exception.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Options_Should_Throw_On_Invalid_HalfOpenMaxCalls(int halfOpenMaxCalls)
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = halfOpenMaxCalls
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Contains("HalfOpenMaxCalls", exception.Message);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.5)]
        [InlineData(-0.1)]
        public void Options_Should_Throw_On_Invalid_SlowCallThreshold(double slowCallThreshold)
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 3,
                SlowCallThreshold = slowCallThreshold
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Contains("SlowCallThreshold", exception.Message);
        }
    }
}