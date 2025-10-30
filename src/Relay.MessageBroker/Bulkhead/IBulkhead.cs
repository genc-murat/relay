namespace Relay.MessageBroker.Bulkhead;

/// <summary>
/// Interface for bulkhead pattern implementation that isolates resources and prevents cascading failures.
/// </summary>
public interface IBulkhead
{
    /// <summary>
    /// Executes an operation within the bulkhead's resource constraints.
    /// </summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="BulkheadRejectedException">Thrown when the bulkhead is full and cannot accept more operations.</exception>
    ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current metrics for this bulkhead.
    /// </summary>
    /// <returns>The bulkhead metrics.</returns>
    BulkheadMetrics GetMetrics();
}
