namespace Relay.MessageBroker.ConnectionPool;

/// <summary>
/// Interface for managing a pool of reusable connections.
/// </summary>
/// <typeparam name="TConnection">The type of connection to pool.</typeparam>
public interface IConnectionPool<TConnection> : IAsyncDisposable
{
    /// <summary>
    /// Acquires a connection from the pool.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A pooled connection.</returns>
    ValueTask<PooledConnection<TConnection>> AcquireAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a connection back to the pool.
    /// </summary>
    /// <param name="connection">The connection to release.</param>
    ValueTask ReleaseAsync(PooledConnection<TConnection> connection);

    /// <summary>
    /// Gets the current metrics for the connection pool.
    /// </summary>
    /// <returns>Connection pool metrics.</returns>
    ConnectionPoolMetrics GetMetrics();
}
