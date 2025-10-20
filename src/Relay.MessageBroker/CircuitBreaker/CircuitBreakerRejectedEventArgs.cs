namespace Relay.MessageBroker.CircuitBreaker;

/// <summary>
/// Event arguments for circuit breaker rejected event.
/// </summary>
public sealed class CircuitBreakerRejectedEventArgs
{
    /// <summary>
    /// Gets the current state.
    /// </summary>
    public CircuitBreakerState CurrentState { get; init; }

    /// <summary>
    /// Gets the timestamp of rejection.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the operation name that was rejected.
    /// </summary>
    public string? OperationName { get; init; }
}
