using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Percentage-based circuit breaker strategy that opens the circuit based on failure rate
    /// and closes it based on consecutive successes.
    /// </summary>
    public class PercentageBasedCircuitBreakerStrategy : ICircuitBreakerStrategy
    {
        private readonly double _failureRateThreshold;

        /// <summary>
        /// Initializes a new instance of the PercentageBasedCircuitBreakerStrategy.
        /// </summary>
        /// <param name="failureRateThreshold">Failure rate threshold (0.0 to 1.0) to trigger circuit opening. Default is 0.5 (50%).</param>
        public PercentageBasedCircuitBreakerStrategy(double failureRateThreshold = 0.5)
        {
            if (failureRateThreshold < 0 || failureRateThreshold > 1)
                throw new ArgumentException("Failure rate threshold must be between 0 and 1", nameof(failureRateThreshold));

            _failureRateThreshold = failureRateThreshold;
        }

        /// <inheritdoc />
        public string Name => "PercentageBased";

        /// <inheritdoc />
        public bool ShouldOpen(CircuitBreakerMetrics metrics, AICircuitBreakerOptions options)
        {
            // Need minimum number of calls to make a statistically meaningful decision
            const int minimumCalls = 10;
            if (metrics.EffectiveCalls < minimumCalls)
                return false;

            return metrics.FailureRate >= _failureRateThreshold;
        }

        /// <inheritdoc />
        public bool ShouldClose(int recentSuccesses, int recentFailures, AICircuitBreakerOptions options)
        {
            return recentSuccesses >= options.SuccessThreshold;
        }
    }
}