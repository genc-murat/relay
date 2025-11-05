using System;
using Relay.Core.AI.CircuitBreaker.Options;

namespace Relay.Core.AI.CircuitBreaker.Strategies
{
    /// <summary>
    /// Factory for creating circuit breaker strategies.
    /// </summary>
    public static class CircuitBreakerStrategyFactory
    {
        /// <summary>
        /// Creates a circuit breaker strategy based on the specified policy.
        /// </summary>
        /// <param name="policy">The circuit breaker policy type.</param>
        /// <returns>The appropriate circuit breaker strategy implementation.</returns>
        public static ICircuitBreakerStrategy CreateStrategy(CircuitBreakerPolicy policy)
        {
            return policy switch
            {
                CircuitBreakerPolicy.Standard => new StandardCircuitBreakerStrategy(),
                CircuitBreakerPolicy.PercentageBased => new PercentageBasedCircuitBreakerStrategy(),
                CircuitBreakerPolicy.Adaptive => new AdaptiveCircuitBreakerStrategy(),
                _ => throw new ArgumentException($"Unsupported circuit breaker policy: {policy}", nameof(policy))
            };
        }

        /// <summary>
        /// Creates a custom percentage-based strategy with specified failure rate threshold.
        /// </summary>
        /// <param name="failureRateThreshold">Failure rate threshold (0.0 to 1.0).</param>
        /// <returns>A percentage-based circuit breaker strategy.</returns>
        public static ICircuitBreakerStrategy CreatePercentageBasedStrategy(double failureRateThreshold)
        {
            return new PercentageBasedCircuitBreakerStrategy(failureRateThreshold);
        }

        /// <summary>
        /// Creates a custom adaptive strategy with specified parameters.
        /// </summary>
        /// <param name="baseFailureThreshold">Base failure threshold.</param>
        /// <param name="loadSensitivity">Load sensitivity factor (0.0 to 1.0).</param>
        /// <returns>An adaptive circuit breaker strategy.</returns>
        public static ICircuitBreakerStrategy CreateAdaptiveStrategy(double baseFailureThreshold, double loadSensitivity)
        {
            return new AdaptiveCircuitBreakerStrategy(baseFailureThreshold, loadSensitivity);
        }
    }
}