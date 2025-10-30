# Message Broker Metrics - Usage Examples

This document provides practical examples of using the message broker metrics system.

## Basic Setup

### 1. Configure Services

```csharp
using Relay.MessageBroker.Metrics;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Add message broker metrics
builder.Services.AddMessageBrokerMetrics(options =>
{
    options.Enabled = true;
    options.MeterName = "MyApp.MessageBroker";
    options.EnableConnectionPoolMetrics = true;
    options.BrokerType = "RabbitMQ";
    options.DefaultTenantId = "default";
});

// Configure OpenTelemetry with Prometheus exporter
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMessageBrokerInstrumentation("MyApp.MessageBroker")
        .AddPrometheusExporterForMessageBroker());

var app = builder.Build();

// Enable Prometheus scrape endpoint
app.UsePrometheusScrapingEndpoint("/metrics");

app.Run();
```

## Recording Metrics in Custom Broker

### Example: Custom Message Broker Implementation

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.Metrics;
using System.Diagnostics;

public class CustomMessageBroker : BaseMessageBroker
{
    private readonly MessageBrokerMetrics _metrics;
    private readonly string _brokerType;

    public CustomMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger<CustomMessageBroker> logger,
        MessageBrokerMetrics metrics)
        : base(options, logger)
    {
        _metrics = metrics;
        _brokerType = "Custom";
    }

    protected override async ValueTask PublishInternalAsync<TMessage>(
        TMessage message,
        byte[] serializedMessage,
        PublishOptions? options,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var messageType = typeof(TMessage).Name;
        var tenantId = options?.Headers?.GetValueOrDefault("TenantId")?.ToString();

        try
        {
            // Your publish logic here
            await ActualPublishAsync(serializedMessage, cancellationToken);

            // Record successful publish
            stopwatch.Stop();
            _metrics.RecordPublishLatency(
                stopwatch.Elapsed.TotalMilliseconds,
                messageType,
                _brokerType,
                tenantId);

            _metrics.RecordMessagePublished(
                messageType,
                _brokerType,
                serializedMessage.Length,
                tenantId);
        }
        catch (Exception ex)
        {
            // Record error
            _metrics.RecordPublishError(
                messageType,
                _brokerType,
                ex.GetType().Name,
                tenantId);
            throw;
        }
    }

    protected override async ValueTask SubscribeInternalAsync(
        Type messageType,
        SubscriptionInfo subscriptionInfo,
        CancellationToken cancellationToken)
    {
        // Subscribe and process messages
        await StartConsumingAsync(messageType, async (data, context) =>
        {
            var stopwatch = Stopwatch.StartNew();
            var tenantId = context.Headers?.GetValueOrDefault("TenantId")?.ToString();

            try
            {
                // Process message
                await ProcessMessageAsync(data, messageType, context, cancellationToken);

                // Record successful consume
                stopwatch.Stop();
                _metrics.RecordConsumeLatency(
                    stopwatch.Elapsed.TotalMilliseconds,
                    messageType.Name,
                    _brokerType,
                    tenantId);

                _metrics.RecordMessageConsumed(
                    messageType.Name,
                    _brokerType,
                    data.Length,
                    tenantId);
            }
            catch (Exception ex)
            {
                // Record error
                _metrics.RecordConsumeError(
                    messageType.Name,
                    _brokerType,
                    ex.GetType().Name,
                    tenantId);
                throw;
            }
        });
    }

    // Update connection and queue metrics periodically
    private async Task UpdateMetricsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var activeConnections = GetActiveConnectionCount();
            var queueDepth = GetQueueDepth();

            _metrics.SetActiveConnections(activeConnections);
            _metrics.SetQueueDepth(queueDepth);

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }
}
```

## Connection Pool Metrics Integration

### Example: Connection Pool with Metrics

```csharp
using Relay.MessageBroker.ConnectionPool;
using Relay.MessageBroker.Metrics;

public class MetricsEnabledConnectionPool<TConnection> : IConnectionPool<TConnection>
{
    private readonly ConnectionPoolManager<TConnection> _pool;
    private readonly ConnectionPoolMetricsCollector _metrics;
    private readonly string _brokerType;
    private readonly string _poolName;

    public MetricsEnabledConnectionPool(
        ConnectionPoolManager<TConnection> pool,
        ConnectionPoolMetricsCollector metrics,
        string brokerType,
        string poolName)
    {
        _pool = pool;
        _metrics = metrics;
        _brokerType = brokerType;
        _poolName = poolName;

        // Start background task to update gauge metrics
        _ = UpdateMetricsAsync();
    }

    public async ValueTask<PooledConnection<TConnection>> AcquireAsync(
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var connection = await _pool.AcquireAsync(cancellationToken);

            // Record wait time
            _metrics.RecordConnectionWaitTime(
                stopwatch.Elapsed.TotalMilliseconds,
                _brokerType,
                _poolName);

            return connection;
        }
        catch
        {
            throw;
        }
    }

    public async ValueTask ReleaseAsync(PooledConnection<TConnection> connection)
    {
        await _pool.ReleaseAsync(connection);
    }

    private async Task UpdateMetricsAsync()
    {
        while (true)
        {
            var poolMetrics = _pool.GetMetrics();

            _metrics.SetActiveConnections(poolMetrics.ActiveConnections);
            _metrics.SetIdleConnections(poolMetrics.IdleConnections);

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    // Track connection lifecycle
    public async ValueTask<TConnection> CreateConnectionAsync(
        CancellationToken cancellationToken)
    {
        var connection = await _pool.CreateConnectionAsync(cancellationToken);

        _metrics.RecordConnectionCreated(_brokerType, _poolName);

        return connection;
    }

    public async ValueTask DisposeConnectionAsync(TConnection connection)
    {
        await _pool.DisposeConnectionAsync(connection);

        _metrics.RecordConnectionDisposed(_brokerType, _poolName);
    }
}
```

## Multi-Tenant Scenario

### Example: Tenant-Aware Metrics

```csharp
public class TenantAwareMessageBroker : BaseMessageBroker
{
    private readonly MessageBrokerMetrics _metrics;
    private readonly ITenantResolver _tenantResolver;

    public TenantAwareMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger<TenantAwareMessageBroker> logger,
        MessageBrokerMetrics metrics,
        ITenantResolver tenantResolver)
        : base(options, logger)
    {
        _metrics = metrics;
        _tenantResolver = tenantResolver;
    }

    protected override async ValueTask PublishInternalAsync<TMessage>(
        TMessage message,
        byte[] serializedMessage,
        PublishOptions? options,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var messageType = typeof(TMessage).Name;
        
        // Resolve tenant from context or message
        var tenantId = _tenantResolver.GetCurrentTenant() 
            ?? options?.Headers?.GetValueOrDefault("TenantId")?.ToString()
            ?? "default";

        try
        {
            await ActualPublishAsync(serializedMessage, cancellationToken);

            stopwatch.Stop();
            
            // Record with tenant ID
            _metrics.RecordPublishLatency(
                stopwatch.Elapsed.TotalMilliseconds,
                messageType,
                "RabbitMQ",
                tenantId);

            _metrics.RecordMessagePublished(
                messageType,
                "RabbitMQ",
                serializedMessage.Length,
                tenantId);
        }
        catch (Exception ex)
        {
            _metrics.RecordPublishError(
                messageType,
                "RabbitMQ",
                ex.GetType().Name,
                tenantId);
            throw;
        }
    }
}

public interface ITenantResolver
{
    string? GetCurrentTenant();
}
```

## Monitoring Dashboard Setup

### Example: Grafana Dashboard JSON

```json
{
  "dashboard": {
    "title": "Message Broker Metrics",
    "panels": [
      {
        "title": "Publish Latency (P95)",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(messagebroker_publish_latency_bucket[5m])) by (message_type)",
            "legendFormat": "{{message_type}}"
          }
        ],
        "type": "graph"
      },
      {
        "title": "Messages Per Second",
        "targets": [
          {
            "expr": "rate(messagebroker_messages_published_total[1m])",
            "legendFormat": "Published"
          },
          {
            "expr": "rate(messagebroker_messages_consumed_total[1m])",
            "legendFormat": "Consumed"
          }
        ],
        "type": "graph"
      },
      {
        "title": "Error Rate",
        "targets": [
          {
            "expr": "rate(messagebroker_publish_errors_total[5m]) by (error_type)",
            "legendFormat": "{{error_type}}"
          }
        ],
        "type": "graph"
      },
      {
        "title": "Connection Pool Status",
        "targets": [
          {
            "expr": "connectionpool_connections_active",
            "legendFormat": "Active"
          },
          {
            "expr": "connectionpool_connections_idle",
            "legendFormat": "Idle"
          }
        ],
        "type": "graph"
      }
    ]
  }
}
```

## Testing Metrics

### Example: Unit Test for Metrics

```csharp
using Xunit;
using Relay.MessageBroker.Metrics;
using System.Diagnostics.Metrics;

public class MessageBrokerMetricsTests
{
    [Fact]
    public void RecordPublishLatency_ShouldRecordMetric()
    {
        // Arrange
        var metrics = new MessageBrokerMetrics("Test.MessageBroker");
        var listener = new MeterListener();
        var measurements = new List<double>();

        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "messagebroker.publish.latency")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            measurements.Add(measurement);
        });

        listener.Start();

        // Act
        metrics.RecordPublishLatency(100.5, "TestMessage", "TestBroker");

        // Assert
        Assert.Single(measurements);
        Assert.Equal(100.5, measurements[0]);

        listener.Dispose();
        metrics.Dispose();
    }

    [Fact]
    public void RecordMessagePublished_ShouldIncrementCounter()
    {
        // Arrange
        var metrics = new MessageBrokerMetrics("Test.MessageBroker");
        var listener = new MeterListener();
        var count = 0L;

        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "messagebroker.messages.published")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            count += measurement;
        });

        listener.Start();

        // Act
        metrics.RecordMessagePublished("TestMessage", "TestBroker", 1024);
        metrics.RecordMessagePublished("TestMessage", "TestBroker", 2048);

        // Assert
        Assert.Equal(2, count);

        listener.Dispose();
        metrics.Dispose();
    }
}
```

## Performance Considerations

### Example: Conditional Metrics Recording

```csharp
public class OptimizedMessageBroker : BaseMessageBroker
{
    private readonly MessageBrokerMetrics? _metrics;
    private readonly bool _metricsEnabled;

    public OptimizedMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger<OptimizedMessageBroker> logger,
        IOptions<MetricsOptions> metricsOptions,
        MessageBrokerMetrics? metrics = null)
        : base(options, logger)
    {
        _metricsEnabled = metricsOptions.Value.Enabled;
        _metrics = _metricsEnabled ? metrics : null;
    }

    protected override async ValueTask PublishInternalAsync<TMessage>(
        TMessage message,
        byte[] serializedMessage,
        PublishOptions? options,
        CancellationToken cancellationToken)
    {
        Stopwatch? stopwatch = _metricsEnabled ? Stopwatch.StartNew() : null;

        try
        {
            await ActualPublishAsync(serializedMessage, cancellationToken);

            // Only record metrics if enabled
            if (_metricsEnabled && _metrics != null)
            {
                stopwatch!.Stop();
                _metrics.RecordPublishLatency(
                    stopwatch.Elapsed.TotalMilliseconds,
                    typeof(TMessage).Name,
                    "Custom");
                _metrics.RecordMessagePublished(
                    typeof(TMessage).Name,
                    "Custom",
                    serializedMessage.Length);
            }
        }
        catch (Exception ex)
        {
            _metrics?.RecordPublishError(
                typeof(TMessage).Name,
                "Custom",
                ex.GetType().Name);
            throw;
        }
    }
}
```

## Complete Application Example

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.Metrics;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Configure message broker with metrics
builder.Services.AddMessageBrokerMetrics(options =>
{
    options.Enabled = true;
    options.MeterName = "MyApp.MessageBroker";
    options.EnableConnectionPoolMetrics = true;
    options.BrokerType = "RabbitMQ";
});

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMessageBrokerInstrumentation("MyApp.MessageBroker")
        .AddPrometheusExporterForMessageBroker()
        .AddRuntimeInstrumentation()
        .AddAspNetCoreInstrumentation());

// Add message broker
builder.Services.AddRabbitMQMessageBroker(options =>
{
    options.HostName = "localhost";
    options.Port = 5672;
});

var app = builder.Build();

// Enable Prometheus endpoint
app.UsePrometheusScrapingEndpoint("/metrics");

// Example endpoint that publishes a message
app.MapPost("/orders", async (Order order, IMessageBroker broker) =>
{
    await broker.PublishAsync(new OrderCreatedEvent
    {
        OrderId = order.Id,
        CustomerId = order.CustomerId,
        Total = order.Total
    });

    return Results.Ok();
});

app.Run();

public record Order(string Id, string CustomerId, decimal Total);
public record OrderCreatedEvent
{
    public string OrderId { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public decimal Total { get; init; }
}
```
