using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Relay.MessageBroker.ConnectionPool;

/// <summary>
/// Manages a pool of reusable connections with lifecycle management.
/// </summary>
/// <typeparam name="TConnection">The type of connection to pool.</typeparam>
public class ConnectionPoolManager<TConnection> : IConnectionPool<TConnection>
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentBag<PooledConnection<TConnection>> _availableConnections;
    private readonly ConcurrentDictionary<Guid, PooledConnection<TConnection>> _allConnections;
    private readonly Func<CancellationToken, ValueTask<TConnection>> _connectionFactory;
    private readonly Func<TConnection, ValueTask<bool>>? _connectionValidator;
    private readonly Func<TConnection, ValueTask>? _connectionDisposer;
    private readonly ConnectionPoolOptions _options;
    private readonly ILogger<ConnectionPoolManager<TConnection>>? _logger;
    private readonly Timer _validationTimer;
    private readonly ConcurrentQueue<double> _waitTimes;
    private long _totalConnectionsCreated;
    private long _totalConnectionsDisposed;
    private bool _disposed;
    private readonly object _initLock = new();
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionPoolManager{TConnection}"/> class.
    /// </summary>
    /// <param name="connectionFactory">Factory function to create new connections.</param>
    /// <param name="options">Connection pool options.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="connectionValidator">Optional validator function to check connection health.</param>
    /// <param name="connectionDisposer">Optional disposer function to clean up connections.</param>
    public ConnectionPoolManager(
        Func<CancellationToken, ValueTask<TConnection>> connectionFactory,
        ConnectionPoolOptions options,
        ILogger<ConnectionPoolManager<TConnection>>? logger = null,
        Func<TConnection, ValueTask<bool>>? connectionValidator = null,
        Func<TConnection, ValueTask>? connectionDisposer = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _connectionValidator = connectionValidator;
        _connectionDisposer = connectionDisposer;

        if (_options.MinPoolSize < 0)
        {
            throw new ArgumentException("MinPoolSize cannot be negative.", nameof(options));
        }

        if (_options.MaxPoolSize < _options.MinPoolSize)
        {
            throw new ArgumentException("MaxPoolSize must be greater than or equal to MinPoolSize.", nameof(options));
        }

        _semaphore = new SemaphoreSlim(_options.MaxPoolSize, _options.MaxPoolSize);
        _availableConnections = new ConcurrentBag<PooledConnection<TConnection>>();
        _allConnections = new ConcurrentDictionary<Guid, PooledConnection<TConnection>>();
        _waitTimes = new ConcurrentQueue<double>();

        // Start validation timer
        _validationTimer = new Timer(
            ValidateConnectionsCallback,
            null,
            _options.ValidationInterval,
            _options.ValidationInterval);
    }

    /// <summary>
    /// Acquires a connection from the pool.
    /// </summary>
    public async ValueTask<PooledConnection<TConnection>> AcquireAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Initialize minimum connections on first use
        await EnsureMinimumConnectionsAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Wait for available slot with timeout
            var acquired = await _semaphore.WaitAsync(_options.ConnectionTimeout, cancellationToken);
            if (!acquired)
            {
                throw new TimeoutException($"Failed to acquire connection within {_options.ConnectionTimeout.TotalSeconds} seconds.");
            }

            stopwatch.Stop();
            RecordWaitTime(stopwatch.Elapsed.TotalMilliseconds);

            // Try to get an existing valid connection
            while (_availableConnections.TryTake(out var pooledConnection))
            {
                if (await IsConnectionValidAsync(pooledConnection, cancellationToken))
                {
                    pooledConnection.LastUsedAt = DateTimeOffset.UtcNow;
                    _logger?.LogDebug("Acquired existing connection {ConnectionId} from pool", pooledConnection.Id);
                    return pooledConnection;
                }

                // Connection is invalid, remove and dispose it
                _logger?.LogDebug("Removing invalid connection {ConnectionId} from pool", pooledConnection.Id);
                await RemoveConnectionAsync(pooledConnection, cancellationToken);
            }

            // No valid connections available, create a new one
            var newConnection = await CreateConnectionAsync(cancellationToken);
            _logger?.LogDebug("Created new connection {ConnectionId}", newConnection.Id);
            return newConnection;
        }
        catch
        {
            // Release semaphore if we failed to acquire a connection
            _semaphore.Release();
            throw;
        }
    }

    /// <summary>
    /// Releases a connection back to the pool.
    /// </summary>
    public ValueTask ReleaseAsync(PooledConnection<TConnection> connection)
    {
        if (connection == null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        if (_disposed)
        {
            // Pool is disposed, just dispose the connection
            return DisposeConnectionAsync(connection, CancellationToken.None);
        }

        // Check if connection is still valid
        if (connection.IsValid && _allConnections.ContainsKey(connection.Id))
        {
            connection.LastUsedAt = DateTimeOffset.UtcNow;
            _availableConnections.Add(connection);
            _logger?.LogDebug("Released connection {ConnectionId} back to pool", connection.Id);
        }
        else
        {
            // Connection is invalid or not tracked, dispose it
            _logger?.LogDebug("Disposing invalid connection {ConnectionId}", connection.Id);
            return DisposeConnectionAsync(connection, CancellationToken.None);
        }

        _semaphore.Release();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Gets the current metrics for the connection pool.
    /// </summary>
    public ConnectionPoolMetrics GetMetrics()
    {
        var totalConnections = _allConnections.Count;
        var idleConnections = _availableConnections.Count;
        var activeConnections = totalConnections - idleConnections;
        var waitingThreads = _options.MaxPoolSize - _semaphore.CurrentCount;

        var avgWaitTime = 0.0;
        if (_waitTimes.Count > 0)
        {
            avgWaitTime = _waitTimes.Average();
        }

        return new ConnectionPoolMetrics
        {
            ActiveConnections = activeConnections,
            IdleConnections = idleConnections,
            TotalConnections = totalConnections,
            WaitingThreads = waitingThreads,
            AverageWaitTimeMs = avgWaitTime,
            TotalConnectionsCreated = Interlocked.Read(ref _totalConnectionsCreated),
            TotalConnectionsDisposed = Interlocked.Read(ref _totalConnectionsDisposed)
        };
    }

    /// <summary>
    /// Disposes the connection pool and all connections.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _logger?.LogInformation("Disposing connection pool with {Count} connections", _allConnections.Count);

        // Stop validation timer
        await _validationTimer.DisposeAsync();

        // Create a task to dispose all connections with timeout
        var disposeTask = DisposeAllConnectionsAsync();
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));

        var completedTask = await Task.WhenAny(disposeTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            _logger?.LogWarning("Connection pool disposal timed out after 10 seconds");
        }

        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task EnsureMinimumConnectionsAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        lock (_initLock)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
        }

        _logger?.LogInformation("Initializing connection pool with {MinPoolSize} minimum connections", _options.MinPoolSize);

        var tasks = new List<Task>(_options.MinPoolSize);
        for (var i = 0; i < _options.MinPoolSize; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var connection = await CreateConnectionAsync(cancellationToken);
                    _availableConnections.Add(connection);
                    _semaphore.Release();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to create initial connection");
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private async ValueTask<PooledConnection<TConnection>> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = await _connectionFactory(cancellationToken);
        var pooledConnection = new PooledConnection<TConnection>(connection, this);
        _allConnections.TryAdd(pooledConnection.Id, pooledConnection);
        Interlocked.Increment(ref _totalConnectionsCreated);
        return pooledConnection;
    }

    private async ValueTask<bool> IsConnectionValidAsync(PooledConnection<TConnection> connection, CancellationToken cancellationToken)
    {
        if (!connection.IsValid)
        {
            return false;
        }

        // Check idle timeout
        var idleTime = DateTimeOffset.UtcNow - connection.LastUsedAt;
        if (idleTime > _options.IdleTimeout)
        {
            _logger?.LogDebug("Connection {ConnectionId} exceeded idle timeout ({IdleTime})", connection.Id, idleTime);
            return false;
        }

        // Use custom validator if provided
        if (_connectionValidator != null)
        {
            try
            {
                return await _connectionValidator(connection.Connection);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Connection validation failed for {ConnectionId}", connection.Id);
                return false;
            }
        }

        return true;
    }

    private async ValueTask RemoveConnectionAsync(PooledConnection<TConnection> connection, CancellationToken cancellationToken)
    {
        _allConnections.TryRemove(connection.Id, out _);
        await DisposeConnectionAsync(connection, cancellationToken);
    }

    private async ValueTask DisposeConnectionAsync(PooledConnection<TConnection> connection, CancellationToken cancellationToken)
    {
        try
        {
            if (_connectionDisposer != null)
            {
                await _connectionDisposer(connection.Connection);
            }
            else if (connection.Connection is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (connection.Connection is IDisposable disposable)
            {
                disposable.Dispose();
            }

            Interlocked.Increment(ref _totalConnectionsDisposed);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error disposing connection {ConnectionId}", connection.Id);
        }
    }

    private async Task DisposeAllConnectionsAsync()
    {
        var connections = _allConnections.Values.ToList();
        var tasks = connections.Select(c => DisposeConnectionAsync(c, CancellationToken.None).AsTask());
        await Task.WhenAll(tasks);
        _allConnections.Clear();
    }

    private void ValidateConnectionsCallback(object? state)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await ValidateConnectionsAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during connection validation");
            }
        });
    }

    private async Task ValidateConnectionsAsync()
    {
        _logger?.LogDebug("Starting connection validation");

        var connectionsToValidate = _availableConnections.ToList();
        var invalidConnections = new List<PooledConnection<TConnection>>();

        foreach (var connection in connectionsToValidate)
        {
            if (!await IsConnectionValidAsync(connection, CancellationToken.None))
            {
                invalidConnections.Add(connection);
            }
        }

        foreach (var connection in invalidConnections)
        {
            // Try to remove from available connections
            var tempBag = new ConcurrentBag<PooledConnection<TConnection>>();
            while (_availableConnections.TryTake(out var conn))
            {
                if (conn.Id != connection.Id)
                {
                    tempBag.Add(conn);
                }
            }

            // Put back the valid connections
            foreach (var conn in tempBag)
            {
                _availableConnections.Add(conn);
            }

            await RemoveConnectionAsync(connection, CancellationToken.None);
            _semaphore.Release();
        }

        if (invalidConnections.Count > 0)
        {
            _logger?.LogInformation("Removed {Count} invalid connections during validation", invalidConnections.Count);
        }
    }

    private void RecordWaitTime(double milliseconds)
    {
        _waitTimes.Enqueue(milliseconds);

        // Keep only last 100 wait times
        while (_waitTimes.Count > 100)
        {
            _waitTimes.TryDequeue(out _);
        }
    }
}
