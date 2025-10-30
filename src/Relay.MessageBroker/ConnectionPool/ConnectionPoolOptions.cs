namespace Relay.MessageBroker.ConnectionPool;

/// <summary>
/// Configuration options for connection pooling.
/// </summary>
public class ConnectionPoolOptions
{
    /// <summary>
    /// Gets or sets the minimum number of connections to maintain in the pool.
    /// Default is 5.
    /// </summary>
    public int MinPoolSize { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of connections allowed in the pool.
    /// Default is 50.
    /// </summary>
    public int MaxPoolSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the timeout for acquiring a connection from the pool.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the interval for validating idle connections.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan ValidationInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the timeout for idle connections before they are removed.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets whether connection pooling is enabled.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
