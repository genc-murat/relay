namespace Relay.Core.Resilience.CircuitBreaker;

/// <summary>
/// Circuit breaker states.
/// </summary>
public enum CircuitState
{
    Closed,    // Normal operation
    Open,      // Circuit is open, rejecting requests
    HalfOpen   // Testing if the circuit can be closed
}
