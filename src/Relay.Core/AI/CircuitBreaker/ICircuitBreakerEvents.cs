using System;

namespace Relay.Core.AI
{
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