namespace Relay.Core.AI.CircuitBreaker
{
    /// <summary>
    /// Circuit breaker states.
    /// </summary>
    public enum CircuitBreakerState
    {
        Closed,
        Open,
        HalfOpen
    }
}
