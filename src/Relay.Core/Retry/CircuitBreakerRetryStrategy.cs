using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Retry
{
    /// <summary>
    /// Circuit breaker retry strategy that stops retrying after a certain number of consecutive failures.
    /// </summary>
    public class CircuitBreakerRetryStrategy : IRetryStrategy
    {
        private readonly IRetryStrategy _innerStrategy;
        private readonly int _failureThreshold;
        private int _consecutiveFailures;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerRetryStrategy"/> class.
        /// </summary>
        /// <param name="innerStrategy">The inner retry strategy to use when the circuit is closed.</param>
        /// <param name="failureThreshold">The number of consecutive failures before the circuit opens.</param>
        public CircuitBreakerRetryStrategy(IRetryStrategy innerStrategy, int failureThreshold = 5)
        {
            _innerStrategy = innerStrategy ?? throw new ArgumentNullException(nameof(innerStrategy));

            if (failureThreshold <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(failureThreshold), "Failure threshold must be a positive number.");
            }

            _failureThreshold = failureThreshold;
        }

        /// <inheritdoc />
        public async ValueTask<bool> ShouldRetryAsync(int attempt, Exception exception, CancellationToken cancellationToken = default)
        {
            // If we've exceeded the failure threshold, don't retry
            if (_consecutiveFailures >= _failureThreshold)
            {
                return false;
            }

            // Check with the inner strategy
            var shouldRetry = await _innerStrategy.ShouldRetryAsync(attempt, exception, cancellationToken);
            
            // Update consecutive failures count
            if (shouldRetry)
            {
                _consecutiveFailures++;
            }
            else
            {
                _consecutiveFailures = 0;
            }

            return shouldRetry;
        }

        /// <inheritdoc />
        public async ValueTask<TimeSpan> GetRetryDelayAsync(int attempt, Exception exception, CancellationToken cancellationToken = default)
        {
            return await _innerStrategy.GetRetryDelayAsync(attempt, exception, cancellationToken);
        }

        /// <summary>
        /// Resets the circuit breaker.
        /// </summary>
        public void Reset()
        {
            _consecutiveFailures = 0;
        }
    }
}