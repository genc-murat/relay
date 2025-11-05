using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.CircuitBreaker;
using Relay.Core.AI.CircuitBreaker.Options;
using Relay.Core.AI.CircuitBreaker.Events;
using Xunit;
using Relay.Core.AI.CircuitBreaker.Exceptions;

namespace Relay.Core.Tests.AI
{
    public class AICircuitBreakerEventsTests
    {
        private readonly ILogger _logger;

        public AICircuitBreakerEventsTests()
        {
            _logger = NullLogger.Instance;
        }

        [Fact]
        public async Task ExecuteAsync_Should_Fire_Success_Event()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 1
            };
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

            CircuitBreakerSuccessEventArgs? successEvent = null;
            circuitBreaker.OperationSucceeded += (sender, args) => successEvent = args;

            Func<CancellationToken, ValueTask<string>> operation = ct =>
                new ValueTask<string>("success");

            // Act
            await circuitBreaker.ExecuteAsync(operation, CancellationToken.None);

            // Assert
            Assert.NotNull(successEvent);
            Assert.False(successEvent.IsSlowCall);
            Assert.True(successEvent.Duration > TimeSpan.Zero);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Fire_Failure_Event()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 5,
                SuccessThreshold = 3,
                Timeout = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromMinutes(1),
                HalfOpenMaxCalls = 1
            };
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

            CircuitBreakerFailureEventArgs? failureEvent = null;
            circuitBreaker.OperationFailed += (sender, args) => failureEvent = args;

            var testException = new InvalidOperationException("Test failure");
            Func<CancellationToken, ValueTask<string>> operation = ct =>
                throw testException;

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await circuitBreaker.ExecuteAsync(operation, CancellationToken.None));

            // Assert
            Assert.NotNull(failureEvent);
            Assert.Same(testException, failureEvent.Exception);
            Assert.False(failureEvent.IsTimeout);
            Assert.True(failureEvent.Duration > TimeSpan.Zero);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Fire_Rejected_Event()
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

            CircuitBreakerRejectedEventArgs? rejectedEvent = null;
            circuitBreaker.CallRejected += (sender, args) => rejectedEvent = args;

            // Open the circuit
            Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
                throw new InvalidOperationException("Test failure");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

            // Act - Try rejected call
            Func<CancellationToken, ValueTask<string>> successOperation = ct =>
                new ValueTask<string>("success");

            await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
                await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None));

            // Assert
            Assert.NotNull(rejectedEvent);
            Assert.Equal(CircuitBreakerState.Open, rejectedEvent.State);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Fire_State_Changed_Event()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 1,
                SuccessThreshold = 1,
                Timeout = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromMilliseconds(100),
                HalfOpenMaxCalls = 1
            };
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

            var stateChanges = new List<CircuitBreakerStateChangedEventArgs>();
            circuitBreaker.StateChanged += (sender, args) => stateChanges.Add(args);

            // Act - Open circuit
            Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
                throw new InvalidOperationException("Test failure");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

            // Wait for half-open transition
            await Task.Delay(150);

            // Close circuit
            Func<CancellationToken, ValueTask<string>> successOperation = ct =>
                new ValueTask<string>("success");

            await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);

            // Assert
            Assert.Equal(3, stateChanges.Count);
            Assert.Equal(CircuitBreakerState.Closed, stateChanges[0].PreviousState);
            Assert.Equal(CircuitBreakerState.Open, stateChanges[0].NewState);
            Assert.Equal(CircuitBreakerState.Open, stateChanges[1].PreviousState);
            Assert.Equal(CircuitBreakerState.HalfOpen, stateChanges[1].NewState);
            Assert.Equal(CircuitBreakerState.HalfOpen, stateChanges[2].PreviousState);
            Assert.Equal(CircuitBreakerState.Closed, stateChanges[2].NewState);
        }
    }
}