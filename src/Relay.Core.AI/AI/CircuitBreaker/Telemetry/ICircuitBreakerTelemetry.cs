using System;
using Relay.Core.AI.CircuitBreaker.Metrics;

namespace Relay.Core.AI.CircuitBreaker.Telemetry
{
    /// <summary>
    /// Interface for circuit breaker telemetry and observability.
    /// </summary>
    public interface ICircuitBreakerTelemetry
    {
        /// <summary>
        /// Records a state change event.
        /// </summary>
        void RecordStateChange(CircuitBreakerState previousState, CircuitBreakerState newState, string reason);

        /// <summary>
        /// Records an operation success.
        /// </summary>
        void RecordSuccess(TimeSpan duration, bool isSlowCall);

        /// <summary>
        /// Records an operation failure.
        /// </summary>
        void RecordFailure(Exception exception, TimeSpan duration, bool isTimeout);

        /// <summary>
        /// Records a rejected call.
        /// </summary>
        void RecordRejectedCall(CircuitBreakerState state);

        /// <summary>
        /// Updates current metrics.
        /// </summary>
        void UpdateMetrics(CircuitBreakerMetrics metrics);
    }
}