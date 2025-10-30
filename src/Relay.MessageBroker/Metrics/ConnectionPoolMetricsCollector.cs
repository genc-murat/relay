using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Relay.MessageBroker.Metrics;

/// <summary>
/// Provides OpenTelemetry metrics for connection pool operations.
/// </summary>
public sealed class ConnectionPoolMetricsCollector : IDisposable
{
    private readonly Meter _meter;
    
    // Gauges for connection pool state
    private readonly ObservableGauge<int> _activeConnectionsInPool;
    private readonly ObservableGauge<int> _idleConnectionsInPool;
    
    // Histogram for connection wait time
    private readonly Histogram<double> _connectionWaitTime;
    
    // Counters for connection lifecycle
    private readonly Counter<long> _connectionsCreated;
    private readonly Counter<long> _connectionsDisposed;
    
    // State for observable gauges
    private int _currentActiveConnections;
    private int _currentIdleConnections;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionPoolMetricsCollector"/> class.
    /// </summary>
    /// <param name="meterName">The name of the meter (default: "Relay.MessageBroker.ConnectionPool").</param>
    /// <param name="version">The version of the meter.</param>
    public ConnectionPoolMetricsCollector(string meterName = "Relay.MessageBroker.ConnectionPool", string? version = null)
    {
        _meter = new Meter(meterName, version);
        
        // Initialize observable gauges for pool state
        _activeConnectionsInPool = _meter.CreateObservableGauge(
            name: "connectionpool.connections.active",
            observeValue: () => _currentActiveConnections,
            unit: "connections",
            description: "Number of active connections in the pool");
        
        _idleConnectionsInPool = _meter.CreateObservableGauge(
            name: "connectionpool.connections.idle",
            observeValue: () => _currentIdleConnections,
            unit: "connections",
            description: "Number of idle connections in the pool");
        
        // Initialize histogram for wait time
        _connectionWaitTime = _meter.CreateHistogram<double>(
            name: "connectionpool.connection.wait_time",
            unit: "ms",
            description: "Time spent waiting to acquire a connection from the pool in milliseconds");
        
        // Initialize counters for lifecycle events
        _connectionsCreated = _meter.CreateCounter<long>(
            name: "connectionpool.connections.created",
            unit: "connections",
            description: "Total number of connections created");
        
        _connectionsDisposed = _meter.CreateCounter<long>(
            name: "connectionpool.connections.disposed",
            unit: "connections",
            description: "Total number of connections disposed");
    }

    /// <summary>
    /// Updates the active connections gauge.
    /// </summary>
    /// <param name="count">The current number of active connections.</param>
    public void SetActiveConnections(int count)
    {
        _currentActiveConnections = count;
    }

    /// <summary>
    /// Updates the idle connections gauge.
    /// </summary>
    /// <param name="count">The current number of idle connections.</param>
    public void SetIdleConnections(int count)
    {
        _currentIdleConnections = count;
    }

    /// <summary>
    /// Records the time spent waiting for a connection.
    /// </summary>
    /// <param name="waitTimeMs">The wait time in milliseconds.</param>
    /// <param name="brokerType">The type of message broker.</param>
    /// <param name="poolName">Optional pool name identifier.</param>
    public void RecordConnectionWaitTime(double waitTimeMs, string brokerType, string? poolName = null)
    {
        var tags = new TagList
        {
            { "broker_type", brokerType }
        };
        
        if (!string.IsNullOrEmpty(poolName))
        {
            tags.Add("pool_name", poolName);
        }
        
        _connectionWaitTime.Record(waitTimeMs, tags);
    }

    /// <summary>
    /// Records a connection creation event.
    /// </summary>
    /// <param name="brokerType">The type of message broker.</param>
    /// <param name="poolName">Optional pool name identifier.</param>
    public void RecordConnectionCreated(string brokerType, string? poolName = null)
    {
        var tags = new TagList
        {
            { "broker_type", brokerType }
        };
        
        if (!string.IsNullOrEmpty(poolName))
        {
            tags.Add("pool_name", poolName);
        }
        
        _connectionsCreated.Add(1, tags);
    }

    /// <summary>
    /// Records a connection disposal event.
    /// </summary>
    /// <param name="brokerType">The type of message broker.</param>
    /// <param name="poolName">Optional pool name identifier.</param>
    public void RecordConnectionDisposed(string brokerType, string? poolName = null)
    {
        var tags = new TagList
        {
            { "broker_type", brokerType }
        };
        
        if (!string.IsNullOrEmpty(poolName))
        {
            tags.Add("pool_name", poolName);
        }
        
        _connectionsDisposed.Add(1, tags);
    }

    /// <summary>
    /// Disposes the meter and releases resources.
    /// </summary>
    public void Dispose()
    {
        _meter?.Dispose();
    }
}
