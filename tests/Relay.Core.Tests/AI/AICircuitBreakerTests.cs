using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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
        public async Task ExecuteAsync_Should_Execute_Successfully_When_Closed()
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
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

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
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = failureThreshold,
                SuccessThreshold = 2,
                Timeout = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromSeconds(10),
                HalfOpenMaxCalls = 1
            };
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

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
        public async Task ExecuteAsync_Should_Timeout_Slow_Operations()
        {
            // Arrange
            var timeout = TimeSpan.FromMilliseconds(100);
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 3,
                SuccessThreshold = 2,
                Timeout = timeout,
                BreakDuration = TimeSpan.FromSeconds(10),
                HalfOpenMaxCalls = 1
            };
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

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
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 10,
                SuccessThreshold = 2,
                Timeout = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromSeconds(10),
                HalfOpenMaxCalls = 1
            };
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

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

        [Fact]
        public async Task ExecuteAsync_Should_Record_Slow_Calls()
        {
            // Arrange
            var timeout = TimeSpan.FromMilliseconds(200);
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 10,
                SuccessThreshold = 2,
                Timeout = timeout,
                BreakDuration = TimeSpan.FromSeconds(10),
                HalfOpenMaxCalls = 1,
                SlowCallThreshold = 0.5 // 50% of timeout = 100ms
            };
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

            // Slow operation (150ms > 100ms threshold)
            Func<CancellationToken, ValueTask<string>> slowOperation = async ct =>
            {
                await Task.Delay(150, ct);
                return "slow";
            };

            // Act
            var result = await circuitBreaker.ExecuteAsync(slowOperation, CancellationToken.None);

            // Assert
            Assert.Equal("slow", result);
            var metrics = circuitBreaker.GetMetrics();
            Assert.Equal(1, metrics.SlowCalls);
            Assert.Equal(1, metrics.TotalCalls);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Handle_External_Cancellation()
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

            using var cts = new CancellationTokenSource();

            Func<CancellationToken, ValueTask<string>> operation = async ct =>
            {
                cts.Cancel(); // Cancel immediately
                await Task.Delay(1000, ct); // This will be cancelled
                return "result";
            };

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await circuitBreaker.ExecuteAsync(operation, cts.Token));

            // Circuit should not count this as a failure
            var metrics = circuitBreaker.GetMetrics();
            Assert.Equal(0, metrics.FailedCalls);
            Assert.Equal(0, metrics.TotalCalls);
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
        public async Task Metrics_Should_Track_State_Timing()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 1,
                SuccessThreshold = 1,
                Timeout = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromMilliseconds(200),
                HalfOpenMaxCalls = 1
            };
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

            // Act - Open circuit
            Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
                throw new InvalidOperationException("Test failure");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

            // Wait a bit in open state
            await Task.Delay(50);

            var metricsAfterOpen = circuitBreaker.GetMetrics();

            // Transition to half-open and close
            await Task.Delay(250); // Wait for break duration

            Func<CancellationToken, ValueTask<string>> successOperation = ct =>
                new ValueTask<string>("success");

            await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);

            var metricsAfterClose = circuitBreaker.GetMetrics();

            // Assert
            Assert.True(metricsAfterOpen.TotalOpenTime > TimeSpan.Zero);
            Assert.True(metricsAfterClose.TotalClosedTime >= metricsAfterOpen.TotalClosedTime);
        }

        [Fact]
        public async Task Metrics_Should_Calculate_Availability()
        {
            // Arrange
            var options = new AICircuitBreakerOptions
            {
                FailureThreshold = 10,
                SuccessThreshold = 2,
                Timeout = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromSeconds(10),
                HalfOpenMaxCalls = 1
            };
            var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

            // Act - Mix of success and failure
            Func<CancellationToken, ValueTask<string>> successOperation = ct =>
                new ValueTask<string>("success");

            Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
                throw new InvalidOperationException("Test failure");

            for (int i = 0; i < 7; i++)
            {
                await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);
            }

            for (int i = 0; i < 3; i++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));
            }

            var metrics = circuitBreaker.GetMetrics();

            // Assert
            Assert.Equal(10, metrics.TotalCalls);
            Assert.Equal(7, metrics.SuccessfulCalls);
            Assert.Equal(3, metrics.FailedCalls);
            Assert.Equal(10, metrics.EffectiveCalls); // Total - Rejected
            Assert.Equal(0.7, metrics.Availability); // 7/10 = 70%
        }

        [Fact]
        public async Task Metrics_Should_Handle_Rejected_Calls_In_Availability()
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

            // Open circuit
            Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
                throw new InvalidOperationException("Test failure");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

            // Act - Make calls that get rejected
            Func<CancellationToken, ValueTask<string>> successOperation = ct =>
                new ValueTask<string>("success");

            for (int i = 0; i < 3; i++)
            {
                await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
                    await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None));
            }

            var metrics = circuitBreaker.GetMetrics();

            // Assert
            Assert.Equal(4, metrics.TotalCalls); // 1 failure + 3 rejected
            Assert.Equal(0, metrics.SuccessfulCalls);
            Assert.Equal(1, metrics.FailedCalls);
            Assert.Equal(3, metrics.RejectedCalls);
            Assert.Equal(1, metrics.EffectiveCalls); // Total - Rejected
            Assert.Equal(0.0, metrics.Availability); // 0/1 = 0%
        }
    }
}
