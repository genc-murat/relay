namespace Relay.MessageBroker.ConnectionPool;

/// <summary>
/// Metrics for connection pool monitoring.
/// </summary>
public class ConnectionPoolMetrics
{
    /// <summary>
    /// Gets or sets the number of active connections currently in use.
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Gets or sets the number of idle connections available in the pool.
    /// </summary>
    public int IdleConnections { get; set; }

    /// <summary>
    /// Gets or sets the total number of connections in the pool.
    /// </summary>
    public int TotalConnections { get; set; }

    /// <summary>
    /// Gets or sets the number of threads waiting for a connection.
    /// </summary>
    public int WaitingThreads { get; set; }

    /// <summary>
    /// Gets or sets the average wait time for acquiring a connection in milliseconds.
    /// </summary>
    public double AverageWaitTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the total number of connections created.
    /// </summary>
    public long TotalConnectionsCreated { get; set; }

    /// <summary>
    /// Gets or sets the total number of connections disposed.
    /// </summary>
    public long TotalConnectionsDisposed { get; set; }
}
