using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Event arguments for circuit breaker state transitions.
    /// </summary>
    public class CircuitBreakerStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The previous state of the circuit breaker.
        /// </summary>
        public CircuitBreakerState PreviousState { get; }

        /// <summary>
        /// The new state of the circuit breaker.
        /// </summary>
        public CircuitBreakerState NewState { get; }

        /// <summary>
        /// The timestamp when the state change occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Reason for the state change.
        /// </summary>
        public string Reason { get; }

        public CircuitBreakerStateChangedEventArgs(
            CircuitBreakerState previousState,
            CircuitBreakerState newState,
            string reason)
        {
            PreviousState = previousState;
            NewState = newState;
            Timestamp = DateTime.UtcNow;
            Reason = reason;
        }
    }

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

    /// <summary>
    /// Events interface for circuit breaker telemetry.
    /// </summary>
    public interface ICircuitBreakerEvents
    {
        /// <summary>
        /// Event raised when the circuit breaker state changes.
        /// </summary>
        event EventHandler<CircuitBreakerStateChangedEventArgs>? StateChanged;

        /// <summary>
        /// Event raised when an operation fails.
        /// </summary>
        event EventHandler<CircuitBreakerFailureEventArgs>? OperationFailed;

        /// <summary>
        /// Event raised when an operation succeeds.
        /// </summary>
        event EventHandler<CircuitBreakerSuccessEventArgs>? OperationSucceeded;

        /// <summary>
        /// Event raised when a call is rejected due to circuit being open.
        /// </summary>
        event EventHandler<CircuitBreakerRejectedEventArgs>? CallRejected;
    }
}