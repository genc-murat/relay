using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.CircuitBreaker;
using Relay.Core.AI.CircuitBreaker.Options;
using Relay.Core.AI.CircuitBreaker.Metrics;
using Xunit;
using Relay.Core.AI.CircuitBreaker.Exceptions;

namespace Relay.Core.Tests.AI
{
    public class AICircuitBreakerMetricsTests
    {
        private readonly ILogger _logger;

        public AICircuitBreakerMetricsTests()
        {
            _logger = NullLogger.Instance;
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