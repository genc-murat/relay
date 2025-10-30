# Connection Pool

The Connection Pool feature provides efficient connection management for message brokers by maintaining a pool of reusable connections. This improves performance by reducing connection overhead and provides better resource utilization.

## Features

- **Connection Reuse**: Maintains a pool of connections that can be reused across operations
- **Configurable Pool Size**: Set minimum and maximum pool sizes based on workload
- **Connection Validation**: Automatically validates and removes invalid connections
- **Idle Timeout**: Removes connections that have been idle for too long
- **Metrics**: Provides detailed metrics on pool usage and performance
- **Graceful Shutdown**: Ensures all connections are properly closed within 10 seconds

## Configuration

```csharp
services.AddMessageBroker(options =>
{
    options.ConnectionPool = new ConnectionPoolOptions
    {
        Enabled = true,
        MinPoolSize = 5,           // Minimum connections to maintain
        MaxPoolSize = 50,          // Maximum connections allowed
        ConnectionTimeout = TimeSpan.FromSeconds(5),    // Timeout for acquiring connection
        ValidationInterval = TimeSpan.FromSeconds(30),  // How often to validate connections
        IdleTimeout = TimeSpan.FromMinutes(5)          // Remove connections idle longer than this
    };
});
```

## Usage

The connection pool is used automatically by broker implementations when enabled. You don't need to interact with it directly in most cases.

### Manual Usage

If you need to use the connection pool directly:

```csharp
var pool = new ConnectionPoolManager<IConnection>(
    connectionFactory: async ct => await CreateConnectionAsync(ct),
    options: new ConnectionPoolOptions { MinPoolSize = 5, MaxPoolSize = 50 },
    logger: logger,
    connectionValidator: async conn => await ValidateConnectionAsync(conn),
    connectionDisposer: async conn => await DisposeConnectionAsync(conn)
);

// Acquire a connection
await using var pooledConnection = await pool.AcquireAsync(cancellationToken);
var connection = pooledConnection.Connection;

// Use the connection
await connection.DoSomethingAsync();

// Connection is automatically released when disposed
```

## Metrics

Monitor pool health using metrics:

```csharp
var metrics = pool.GetMetrics();
Console.WriteLine($"Active: {metrics.ActiveConnections}");
Console.WriteLine($"Idle: {metrics.IdleConnections}");
Console.WriteLine($"Total: {metrics.TotalConnections}");
Console.WriteLine($"Waiting: {metrics.WaitingThreads}");
Console.WriteLine($"Avg Wait Time: {metrics.AverageWaitTimeMs}ms");
```

## Requirements Satisfied

- **3.1**: Pool maintains configurable minimum and maximum sizes
- **3.2**: Returns connections within 5 seconds (configurable timeout)
- **3.3**: Creates new connections when pool is not at maximum
- **3.4**: Validates idle connections every 30 seconds (configurable)
- **3.5**: Gracefully closes all connections within 10 seconds on shutdown

## Performance Considerations

- Set `MinPoolSize` based on baseline load to avoid cold starts
- Set `MaxPoolSize` based on peak load and available resources
- Adjust `IdleTimeout` based on connection cost vs. memory usage
- Monitor `AverageWaitTimeMs` - high values indicate pool is undersized
- Monitor `WaitingThreads` - non-zero values indicate contention
