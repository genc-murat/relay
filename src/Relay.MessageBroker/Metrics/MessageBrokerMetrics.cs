using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Relay.MessageBroker.Metrics;

/// <summary>
/// Provides OpenTelemetry metrics for message broker operations.
/// </summary>
public sealed class MessageBrokerMetrics : IDisposable
{
    private readonly Meter _meter;
    
    // Histograms for latency tracking
    private readonly Histogram<double> _publishLatency;
    private readonly Histogram<double> _consumeLatency;
    
    // Counters for message tracking
    private readonly Counter<long> _messagesPublished;
    private readonly Counter<long> _messagesConsumed;
    private readonly Counter<long> _publishErrors;
    private readonly Counter<long> _consumeErrors;
    
    // Gauges for connection and queue monitoring
    private readonly ObservableGauge<int> _activeConnections;
    private readonly ObservableGauge<int> _queueDepth;
    
    // State for observable gauges
    private int _currentActiveConnections;
    private int _currentQueueDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBrokerMetrics"/> class.
    /// </summary>
    /// <param name="meterName">The name of the meter (default: "Relay.MessageBroker").</param>
    /// <param name="version">The version of the meter.</param>
    public MessageBrokerMetrics(string meterName = "Relay.MessageBroker", string? version = null)
    {
        _meter = new Meter(meterName, version);
        
        // Initialize histograms for latency with millisecond unit
        _publishLatency = _meter.CreateHistogram<double>(
            name: "messagebroker.publish.latency",
            unit: "ms",
            description: "Latency of message publish operations in milliseconds");
        
        _consumeLatency = _meter.CreateHistogram<double>(
            name: "messagebroker.consume.latency",
            unit: "ms",
            description: "Latency of message consume operations in milliseconds");
        
        // Initialize counters for message tracking
        _messagesPublished = _meter.CreateCounter<long>(
            name: "messagebroker.messages.published",
            unit: "messages",
            description: "Total number of messages published");
        
        _messagesConsumed = _meter.CreateCounter<long>(
            name: "messagebroker.messages.consumed",
            unit: "messages",
            description: "Total number of messages consumed");
        
        _publishErrors = _meter.CreateCounter<long>(
            name: "messagebroker.publish.errors",
            unit: "errors",
            description: "Total number of publish errors");
        
        _consumeErrors = _meter.CreateCounter<long>(
            name: "messagebroker.consume.errors",
            unit: "errors",
            description: "Total number of consume errors");
        
        // Initialize observable gauges
        _activeConnections = _meter.CreateObservableGauge(
            name: "messagebroker.connections.active",
            observeValue: () => _currentActiveConnections,
            unit: "connections",
            description: "Number of active connections");
        
        _queueDepth = _meter.CreateObservableGauge(
            name: "messagebroker.queue.depth",
            observeValue: () => _currentQueueDepth,
            unit: "messages",
            description: "Current queue depth");
    }

    /// <summary>
    /// Records the latency of a publish operation.
    /// </summary>
    /// <param name="latencyMs">The latency in milliseconds.</param>
    /// <param name="messageType">The type of message published.</param>
    /// <param name="brokerType">The type of message broker.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    public void RecordPublishLatency(double latencyMs, string messageType, string brokerType, string? tenantId = null)
    {
        var tags = new TagList
        {
            { "message_type", messageType },
            { "broker_type", brokerType }
        };
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add("tenant_id", tenantId);
        }
        
        _publishLatency.Record(latencyMs, tags);
    }

    /// <summary>
    /// Records the latency of a consume operation.
    /// </summary>
    /// <param name="latencyMs">The latency in milliseconds.</param>
    /// <param name="messageType">The type of message consumed.</param>
    /// <param name="brokerType">The type of message broker.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    public void RecordConsumeLatency(double latencyMs, string messageType, string brokerType, string? tenantId = null)
    {
        var tags = new TagList
        {
            { "message_type", messageType },
            { "broker_type", brokerType }
        };
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add("tenant_id", tenantId);
        }
        
        _consumeLatency.Record(latencyMs, tags);
    }

    /// <summary>
    /// Records a message published event.
    /// </summary>
    /// <param name="messageType">The type of message published.</param>
    /// <param name="brokerType">The type of message broker.</param>
    /// <param name="messageSize">The size of the message in bytes.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    public void RecordMessagePublished(string messageType, string brokerType, long messageSize = 0, string? tenantId = null)
    {
        var tags = new TagList
        {
            { "message_type", messageType },
            { "broker_type", brokerType }
        };
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add("tenant_id", tenantId);
        }
        
        if (messageSize > 0)
        {
            tags.Add("message_size_bytes", messageSize);
        }
        
        _messagesPublished.Add(1, tags);
    }

    /// <summary>
    /// Records a message consumed event.
    /// </summary>
    /// <param name="messageType">The type of message consumed.</param>
    /// <param name="brokerType">The type of message broker.</param>
    /// <param name="messageSize">The size of the message in bytes.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    public void RecordMessageConsumed(string messageType, string brokerType, long messageSize = 0, string? tenantId = null)
    {
        var tags = new TagList
        {
            { "message_type", messageType },
            { "broker_type", brokerType }
        };
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add("tenant_id", tenantId);
        }
        
        if (messageSize > 0)
        {
            tags.Add("message_size_bytes", messageSize);
        }
        
        _messagesConsumed.Add(1, tags);
    }

    /// <summary>
    /// Records a publish error event.
    /// </summary>
    /// <param name="messageType">The type of message that failed to publish.</param>
    /// <param name="brokerType">The type of message broker.</param>
    /// <param name="errorType">The type of error that occurred.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    public void RecordPublishError(string messageType, string brokerType, string errorType, string? tenantId = null)
    {
        var tags = new TagList
        {
            { "message_type", messageType },
            { "broker_type", brokerType },
            { "error_type", errorType }
        };
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add("tenant_id", tenantId);
        }
        
        _publishErrors.Add(1, tags);
    }

    /// <summary>
    /// Records a consume error event.
    /// </summary>
    /// <param name="messageType">The type of message that failed to consume.</param>
    /// <param name="brokerType">The type of message broker.</param>
    /// <param name="errorType">The type of error that occurred.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    public void RecordConsumeError(string messageType, string brokerType, string errorType, string? tenantId = null)
    {
        var tags = new TagList
        {
            { "message_type", messageType },
            { "broker_type", brokerType },
            { "error_type", errorType }
        };
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add("tenant_id", tenantId);
        }
        
        _consumeErrors.Add(1, tags);
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
    /// Updates the queue depth gauge.
    /// </summary>
    /// <param name="depth">The current queue depth.</param>
    public void SetQueueDepth(int depth)
    {
        _currentQueueDepth = depth;
    }

    /// <summary>
    /// Disposes the meter and releases resources.
    /// </summary>
    public void Dispose()
    {
        _meter?.Dispose();
    }
}
