namespace Relay.MessageBroker.CircuitBreaker;

/// <summary>
/// Circuit breaker states.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Circuit is closed, allowing requests to pass through.
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open, blocking requests.
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open, allowing limited requests to test service health.
    /// </summary>
    HalfOpen
}
