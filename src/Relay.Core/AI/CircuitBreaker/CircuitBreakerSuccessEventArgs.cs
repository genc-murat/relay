using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Event arguments for circuit breaker operation successes.
    /// </summary>
    public class CircuitBreakerSuccessEventArgs : EventArgs
    {
        /// <summary>
        /// The duration of the successful operation.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Whether the operation was considered slow.
        /// </summary>
        public bool IsSlowCall { get; }

        /// <summary>
        /// The timestamp when the success occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        public CircuitBreakerSuccessEventArgs(TimeSpan duration, bool isSlowCall)
        {
            Duration = duration;
            IsSlowCall = isSlowCall;
            Timestamp = DateTime.UtcNow;
        }
    }
}