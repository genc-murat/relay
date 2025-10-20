using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Event arguments for circuit breaker operation failures.
    /// </summary>
    public class CircuitBreakerFailureEventArgs : EventArgs
    {
        /// <summary>
        /// The exception that caused the failure.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// The duration of the failed operation.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Whether the failure was due to a timeout.
        /// </summary>
        public bool IsTimeout { get; }

        /// <summary>
        /// The timestamp when the failure occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        public CircuitBreakerFailureEventArgs(
            Exception exception,
            TimeSpan duration,
            bool isTimeout)
        {
            Exception = exception;
            Duration = duration;
            IsTimeout = isTimeout;
            Timestamp = DateTime.UtcNow;
        }
    }
}