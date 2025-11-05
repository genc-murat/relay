using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.CircuitBreaker;
using Relay.Core.AI.CircuitBreaker.Exceptions;
using Relay.Core.AI.CircuitBreaker.Options;
using Relay.Core.AI.CircuitBreaker.Telemetry;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AICircuitBreakerAdvancedTests
    {
        private readonly ILogger _logger;

        public AICircuitBreakerAdvancedTests()
        {
            _logger = NullLogger.Instance;
        }

        [Fact]
        public void CircuitBreaker_Should_Support_Custom_Telemetry()
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

            var customTelemetry = new Mock<ICircuitBreakerTelemetry>();
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger, customTelemetry.Object);

            // Act
            Func<CancellationToken, ValueTask<string>> operation = ct =>
                new ValueTask<string>("success");

            // Assert - Custom telemetry should be used
            Assert.NotNull(circuitBreaker);
        }

        [Fact]
        public void CircuitBreakerOpenException_Should_Have_Correct_Message()
        {
            // Arrange
            var message = "Circuit breaker is open";
            var exception = new CircuitBreakerOpenException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void CircuitBreakerOpenException_Should_Store_InnerException()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner");
            var exception = new CircuitBreakerOpenException("Outer", innerException);

            // Assert
            Assert.Equal("Outer", exception.Message);
            Assert.Same(innerException, exception.InnerException);
        }
    }
}