namespace Relay.MessageBroker.CircuitBreaker;

/// <summary>
/// Event arguments for circuit breaker state changed event.
/// </summary>
public sealed class CircuitBreakerStateChangedEventArgs
{
    /// <summary>
    /// Gets the previous state.
    /// </summary>
    public CircuitBreakerState PreviousState { get; init; }

    /// <summary>
    /// Gets the new state.
    /// </summary>
    public CircuitBreakerState NewState { get; init; }

    /// <summary>
    /// Gets the reason for state change.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets the timestamp of state change.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the metrics at the time of state change.
    /// </summary>
    public CircuitBreakerMetrics? Metrics { get; init; }
}
