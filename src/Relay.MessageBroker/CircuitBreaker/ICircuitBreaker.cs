namespace Relay.MessageBroker.CircuitBreaker;

/// <summary>
/// Circuit breaker interface for protecting message broker operations.
/// </summary>
public interface ICircuitBreaker
{
    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    CircuitBreakerState State { get; }

    /// <summary>
    /// Gets the current metrics.
    /// </summary>
    CircuitBreakerMetrics Metrics { get; }

    /// <summary>
    /// Executes an operation with circuit breaker protection.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation with circuit breaker protection.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask ExecuteAsync(
        Func<CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually resets the circuit breaker to closed state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Manually opens the circuit breaker.
    /// </summary>
    void Isolate();
}
