using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AICircuitBreakerStateTests
    {
        private readonly ILogger _logger;

        public AICircuitBreakerStateTests()
        {
            _logger = NullLogger.Instance;
        }

        [Fact]
        public async Task ExecuteAsync_Should_Transition_To_HalfOpen_After_Break_Duration()
        {
            // Arrange
            var breakDuration = TimeSpan.FromMilliseconds(500);
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 1,
                SuccessThreshold = 1,
                Timeout = TimeSpan.FromSeconds(5),
                BreakDuration = breakDuration,
                HalfOpenMaxCalls = 1
            };
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

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
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 1,
                SuccessThreshold = successThreshold,
                Timeout = TimeSpan.FromSeconds(5),
                BreakDuration = breakDuration,
                HalfOpenMaxCalls = 5
            };
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

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
        public async Task ExecuteAsync_Should_Reject_Calls_When_Open()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 1,
                SuccessThreshold = 1,
                Timeout = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 1
            };
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

            // Open the circuit
            Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
                throw new InvalidOperationException("Test failure");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

            Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

            // Act - Try to execute when open
            Func<CancellationToken, ValueTask<string>> successOperation = ct =>
                new ValueTask<string>("success");

            // Assert
            var exception = await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
                await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None));

            Assert.Contains("Circuit breaker is Open", exception.Message);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Limit_HalfOpen_Calls()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 1,
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromMilliseconds(100),
                HalfOpenMaxCalls = 2
            };
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

            // Open the circuit
            Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
                throw new InvalidOperationException("Test failure");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

            Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

            // Wait for half-open transition
            await Task.Delay(150);

            // Execute first half-open call
            Func<CancellationToken, ValueTask<string>> successOperation = ct =>
                new ValueTask<string>("success");

            await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);
            Assert.Equal(CircuitBreakerState.HalfOpen, circuitBreaker.State);

            // Execute second half-open call
            await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);
            Assert.Equal(CircuitBreakerState.HalfOpen, circuitBreaker.State);

            // Third call should be rejected
            var exception = await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
                await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None));

            Assert.Contains("Circuit breaker is HalfOpen", exception.Message);
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