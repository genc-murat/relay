namespace Relay.MessageBroker.ConnectionPool;

/// <summary>
/// Represents a connection managed by a connection pool.
/// </summary>
/// <typeparam name="TConnection">The type of connection.</typeparam>
public class PooledConnection<TConnection> : IAsyncDisposable
{
    private readonly IConnectionPool<TConnection> _pool;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledConnection{TConnection}"/> class.
    /// </summary>
    /// <param name="connection">The underlying connection.</param>
    /// <param name="pool">The connection pool that owns this connection.</param>
    public PooledConnection(TConnection connection, IConnectionPool<TConnection> pool)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
        LastUsedAt = DateTimeOffset.UtcNow;
        IsValid = true;
    }

    /// <summary>
    /// Gets the underlying connection.
    /// </summary>
    public TConnection Connection { get; }

    /// <summary>
    /// Gets the unique identifier for this pooled connection.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the timestamp when this connection was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets or sets the timestamp when this connection was last used.
    /// </summary>
    public DateTimeOffset LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this connection is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Releases the connection back to the pool.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _pool.ReleaseAsync(this);
        GC.SuppressFinalize(this);
    }
}
