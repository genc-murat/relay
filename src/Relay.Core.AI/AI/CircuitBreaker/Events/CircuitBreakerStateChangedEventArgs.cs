using System;

namespace Relay.Core.AI.CircuitBreaker.Events;

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