using System;
using Relay.Core.AI.CircuitBreaker.Metrics;
using Relay.Core.AI.CircuitBreaker.Options;

namespace Relay.Core.AI.CircuitBreaker.Strategies
{
    /// <summary>
    /// Standard circuit breaker strategy that opens the circuit based on consecutive failures
    /// and closes it based on consecutive successes.
    /// </summary>
    public class StandardCircuitBreakerStrategy : ICircuitBreakerStrategy
    {
        /// <inheritdoc />
        public string Name => "Standard";

        /// <inheritdoc />
        public bool ShouldOpen(CircuitBreakerMetrics metrics, AICircuitBreakerOptions options)
        {
            return metrics.ConsecutiveFailures >= options.FailureThreshold;
        }

        /// <inheritdoc />
        public bool ShouldClose(int recentSuccesses, int recentFailures, AICircuitBreakerOptions options)
        {
            return recentSuccesses >= options.SuccessThreshold;
        }
    }
}