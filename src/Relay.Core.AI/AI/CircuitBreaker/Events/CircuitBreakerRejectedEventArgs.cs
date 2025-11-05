using System;

namespace Relay.Core.AI.CircuitBreaker.Events
{
    /// <summary>
    /// Event arguments for circuit breaker call rejections.
    /// </summary>
    public class CircuitBreakerRejectedEventArgs : EventArgs
    {
        /// <summary>
        /// The current state of the circuit breaker.
        /// </summary>
        public CircuitBreakerState State { get; }

        /// <summary>
        /// The timestamp when the call was rejected.
        /// </summary>
        public DateTime Timestamp { get; }

        public CircuitBreakerRejectedEventArgs(CircuitBreakerState state)
        {
            State = state;
            Timestamp = DateTime.UtcNow;
        }
    }
}