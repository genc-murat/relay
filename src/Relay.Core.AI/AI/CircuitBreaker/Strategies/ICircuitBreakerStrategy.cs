using System;
using Relay.Core.AI.CircuitBreaker.Metrics;
using Relay.Core.AI.CircuitBreaker.Options;

namespace Relay.Core.AI.CircuitBreaker.Strategies
{
    /// <summary>
    /// Interface for circuit breaker strategies that determine when to open/close the circuit.
    /// </summary>
    public interface ICircuitBreakerStrategy
    {
        /// <summary>
        /// Determines whether the circuit should open based on the current metrics and options.
        /// </summary>
        /// <param name="metrics">Current circuit breaker metrics.</param>
        /// <param name="options">Circuit breaker configuration options.</param>
        /// <returns>True if the circuit should open, false otherwise.</returns>
        bool ShouldOpen(CircuitBreakerMetrics metrics, AICircuitBreakerOptions options);

        /// <summary>
        /// Determines whether the circuit should close from half-open state based on recent results.
        /// </summary>
        /// <param name="recentSuccesses">Number of recent consecutive successes.</param>
        /// <param name="recentFailures">Number of recent consecutive failures.</param>
        /// <param name="options">Circuit breaker configuration options.</param>
        /// <returns>True if the circuit should close, false otherwise.</returns>
        bool ShouldClose(int recentSuccesses, int recentFailures, AICircuitBreakerOptions options);

        /// <summary>
        /// Gets the name of the strategy.
        /// </summary>
        string Name { get; }
    }
}