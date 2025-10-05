using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AICircuitBreakerTests
    {
        private readonly ILogger _logger;

        public AICircuitBreakerTests()
        {
            _logger = NullLogger.Instance;
        }

        [Fact]
        public void Constructor_Should_Initialize_With_Valid_Parameters()
        {
            // Arrange & Act
            var circuitBreaker = new AICircuitBreaker<string>(
                failureThreshold: 3,
                successThreshold: 2,
                timeout: TimeSpan.FromSeconds(5),
                breakDuration: TimeSpan.FromSeconds(10),
                halfOpenMaxCalls: 1,
                logger: _logger);

            // Assert
            Assert.NotNull(circuitBreaker);
            Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Execute_Successfully_When_Closed()
        {
            // Arrange
            var circuitBreaker = new AICircuitBreaker<string>(
                failureThreshold: 3,
                successThreshold: 2,
                timeout: TimeSpan.FromSeconds(5),
                breakDuration: TimeSpan.FromSeconds(10),
                halfOpenMaxCalls: 1,
                logger: _logger);

            var executed = false;
            Func<CancellationToken, ValueTask<string>> operation = ct =>
            {
                executed = true;
                return new ValueTask<string>("success");
            };

            // Act
            var result = await circuitBreaker.ExecuteAsync(operation, CancellationToken.None);

            // Assert
            Assert.True(executed);
            Assert.Equal("success", result);
            Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Open_Circuit_After_Threshold_Failures()
        {
            // Arrange
            var failureThreshold = 3;
            var circuitBreaker = new AICircuitBreaker<string>(
                failureThreshold: failureThreshold,
                successThreshold: 2,
                timeout: TimeSpan.FromSeconds(5),
                breakDuration: TimeSpan.FromSeconds(10),
                halfOpenMaxCalls: 1,
                logger: _logger);

            Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
                throw new InvalidOperationException("Test failure");

            // Act - Execute failing operation multiple times
            for (int i = 0; i < failureThreshold; i++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));
            }

            // Assert - Circuit should be open now
            Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

            // Further calls should throw CircuitBreakerOpenException
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
                await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_Should_Transition_To_HalfOpen_After_Break_Duration()
        {
            // Arrange
            var breakDuration = TimeSpan.FromMilliseconds(500);
            var circuitBreaker = new AICircuitBreaker<string>(
                failureThreshold: 1,
                successThreshold: 1,
                timeout: TimeSpan.FromSeconds(5),
                breakDuration: breakDuration,
                halfOpenMaxCalls: 1,
                logger: _logger);

            Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
                throw new InvalidOperationException("Test failure");

            // Act - Fail once to open the circuit
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

            Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

            // Wait for break duration to pass
            await Task.Delay(breakDuration + TimeSpan.FromMilliseconds(100));

            // Execute successful operation - should transition to half-open
            Func<CancellationToken, ValueTask<string>> successOperation = ct =>
                new ValueTask<string>("success");

            var result = await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Close_Circuit_After_Success_Threshold_In_HalfOpen()
        {
            // Arrange
            var successThreshold = 2;
            var breakDuration = TimeSpan.FromMilliseconds(500);
            var circuitBreaker = new AICircuitBreaker<string>(
                failureThreshold: 1,
                successThreshold: successThreshold,
                timeout: TimeSpan.FromSeconds(5),
                breakDuration: breakDuration,
                halfOpenMaxCalls: 5,
                logger: _logger);

            Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
                throw new InvalidOperationException("Test failure");

            // Open the circuit
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

            // Wait for break duration
            await Task.Delay(breakDuration + TimeSpan.FromMilliseconds(100));

            // Execute successful operations
            Func<CancellationToken, ValueTask<string>> successOperation = ct =>
                new ValueTask<string>("success");

            for (int i = 0; i < successThreshold; i++)
            {
                await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);
            }

            // Assert - Should be closed after success threshold
            Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Timeout_Slow_Operations()
        {
            // Arrange
            var timeout = TimeSpan.FromMilliseconds(100);
            var circuitBreaker = new AICircuitBreaker<string>(
                failureThreshold: 3,
                successThreshold: 2,
                timeout: timeout,
                breakDuration: TimeSpan.FromSeconds(10),
                halfOpenMaxCalls: 1,
                logger: _logger);

            Func<CancellationToken, ValueTask<string>> slowOperation = async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
                return "slow";
            };

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutException>(async () =>
                await circuitBreaker.ExecuteAsync(slowOperation, CancellationToken.None));
        }

        [Fact]
        public async Task GetMetrics_Should_Return_Accurate_Counts()
        {
            // Arrange
            var circuitBreaker = new AICircuitBreaker<string>(
                failureThreshold: 10,
                successThreshold: 2,
                timeout: TimeSpan.FromSeconds(5),
                breakDuration: TimeSpan.FromSeconds(10),
                halfOpenMaxCalls: 1,
                logger: _logger);

            Func<CancellationToken, ValueTask<string>> successOperation = ct =>
                new ValueTask<string>("success");

            Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
                throw new InvalidOperationException("Test failure");

            // Act - Execute mix of successful and failing operations
            for (int i = 0; i < 3; i++)
            {
                await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);
            }

            for (int i = 0; i < 2; i++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));
            }

            var metrics = circuitBreaker.GetMetrics();

            // Assert
            Assert.Equal(5, metrics.TotalCalls);
            Assert.Equal(3, metrics.SuccessfulCalls);
            Assert.Equal(2, metrics.FailedCalls);
            Assert.Equal(0.6, metrics.SuccessRate, 2);
            Assert.Equal(0.4, metrics.FailureRate, 2);
        }

        [Fact]
        public void CircuitBreakerMetrics_Should_Calculate_Rates_Correctly()
        {
            // Arrange
            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = 10,
                SuccessfulCalls = 7,
                FailedCalls = 3,
                SlowCalls = 2
            };

            // Assert
            Assert.Equal(0.7, metrics.SuccessRate, 2);
            Assert.Equal(0.3, metrics.FailureRate, 2);
            Assert.Equal(0.2, metrics.SlowCallRate, 2);
        }

        [Fact]
        public void CircuitBreakerMetrics_Should_Handle_Zero_Calls()
        {
            // Arrange
            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = 0,
                SuccessfulCalls = 0,
                FailedCalls = 0,
                SlowCalls = 0
            };

            // Assert
            Assert.Equal(0.0, metrics.SuccessRate);
            Assert.Equal(0.0, metrics.FailureRate);
            Assert.Equal(0.0, metrics.SlowCallRate);
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

        [Theory]
        [InlineData(CircuitBreakerState.Closed)]
        [InlineData(CircuitBreakerState.Open)]
        [InlineData(CircuitBreakerState.HalfOpen)]
        public void CircuitBreakerState_Should_Have_All_States(CircuitBreakerState state)
        {
            // Arrange & Act & Assert
            Assert.True(Enum.IsDefined(typeof(CircuitBreakerState), state));
        }
    }
}
