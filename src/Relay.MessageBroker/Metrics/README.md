# Message Broker Metrics

This directory contains OpenTelemetry metrics instrumentation for Relay.MessageBroker, providing comprehensive observability for message broker operations.

## Features

- **Publish/Consume Latency**: Histogram metrics with p50, p95, p99 percentiles
- **Message Counters**: Track published and consumed messages
- **Error Tracking**: Monitor publish and consume errors
- **Connection Monitoring**: Active connections and queue depth gauges
- **Connection Pool Metrics**: Pool state, wait times, and lifecycle events
- **Prometheus Export**: Built-in Prometheus scrape endpoint support

## Quick Start

### 1. Register Metrics

```csharp
using Relay.MessageBroker.Metrics;

// In Program.cs or Startup.cs
builder.Services.AddMessageBrokerMetrics(options =>
{
    options.Enabled = true;
    options.MeterName = "Relay.MessageBroker";
    options.EnableConnectionPoolMetrics = true;
    options.BrokerType = "RabbitMQ";
});
```

### 2. Configure OpenTelemetry

```csharp
using OpenTelemetry.Metrics;

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMessageBrokerInstrumentation()
        .AddPrometheusExporterForMessageBroker());
```

### 3. Enable Prometheus Endpoint

```csharp
// In Program.cs after app is built
app.UsePrometheusScrapingEndpoint("/metrics");
```

## Available Metrics

### Message Broker Metrics

| Metric Name | Type | Unit | Description |
|------------|------|------|-------------|
| `messagebroker.publish.latency` | Histogram | ms | Latency of message publish operations |
| `messagebroker.consume.latency` | Histogram | ms | Latency of message consume operations |
| `messagebroker.messages.published` | Counter | messages | Total messages published |
| `messagebroker.messages.consumed` | Counter | messages | Total messages consumed |
| `messagebroker.publish.errors` | Counter | errors | Total publish errors |
| `messagebroker.consume.errors` | Counter | errors | Total consume errors |
| `messagebroker.connections.active` | Gauge | connections | Active connections |
| `messagebroker.queue.depth` | Gauge | messages | Current queue depth |

### Connection Pool Metrics

| Metric Name | Type | Unit | Description |
|------------|------|------|-------------|
| `connectionpool.connections.active` | Gauge | connections | Active connections in pool |
| `connectionpool.connections.idle` | Gauge | connections | Idle connections in pool |
| `connectionpool.connection.wait_time` | Histogram | ms | Connection acquisition wait time |
| `connectionpool.connections.created` | Counter | connections | Total connections created |
| `connectionpool.connections.disposed` | Counter | connections | Total connections disposed |

## Metric Labels

All metrics support the following labels for filtering and aggregation:

- `message_type`: The type of message (e.g., "OrderCreated", "UserRegistered")
- `broker_type`: The message broker implementation (e.g., "RabbitMQ", "Kafka", "AzureServiceBus")
- `tenant_id`: Optional tenant identifier for multi-tenant scenarios
- `pool_name`: Optional connection pool identifier
- `error_type`: Type of error for error metrics

## Sample Prometheus Queries

### Latency Percentiles

```promql
# P95 publish latency by message type
histogram_quantile(0.95, 
  rate(messagebroker_publish_latency_bucket[5m])
) by (message_type)

# P99 consume latency by broker type
histogram_quantile(0.99, 
  rate(messagebroker_consume_latency_bucket[5m])
) by (broker_type)

# P50 (median) latency across all operations
histogram_quantile(0.50, 
  rate(messagebroker_publish_latency_bucket[5m])
)
```

### Throughput

```promql
# Messages published per second
rate(messagebroker_messages_published_total[1m])

# Messages consumed per second by message type
rate(messagebroker_messages_consumed_total[1m]) by (message_type)

# Total throughput (publish + consume)
sum(rate(messagebroker_messages_published_total[1m])) + 
sum(rate(messagebroker_messages_consumed_total[1m]))
```

### Error Rates

```promql
# Publish error rate
rate(messagebroker_publish_errors_total[5m])

# Error rate by error type
rate(messagebroker_publish_errors_total[5m]) by (error_type)

# Success rate (percentage)
(
  rate(messagebroker_messages_published_total[5m]) - 
  rate(messagebroker_publish_errors_total[5m])
) / rate(messagebroker_messages_published_total[5m]) * 100
```

### Connection Monitoring

```promql
# Active connections
messagebroker_connections_active

# Queue depth
messagebroker_queue_depth

# Connection pool utilization (%)
(connectionpool_connections_active / 
 (connectionpool_connections_active + connectionpool_connections_idle)) * 100

# Average connection wait time
rate(connectionpool_connection_wait_time_sum[5m]) / 
rate(connectionpool_connection_wait_time_count[5m])
```

### Multi-Tenant Queries

```promql
# Messages published per tenant
sum(rate(messagebroker_messages_published_total[5m])) by (tenant_id)

# P95 latency by tenant
histogram_quantile(0.95, 
  rate(messagebroker_publish_latency_bucket[5m])
) by (tenant_id)

# Error rate by tenant
rate(messagebroker_publish_errors_total[5m]) by (tenant_id)
```

## Grafana Dashboard

### Sample Dashboard Panels

#### 1. Latency Overview
```json
{
  "title": "Message Broker Latency (P95)",
  "targets": [
    {
      "expr": "histogram_quantile(0.95, rate(messagebroker_publish_latency_bucket[5m]))",
      "legendFormat": "Publish P95"
    },
    {
      "expr": "histogram_quantile(0.95, rate(messagebroker_consume_latency_bucket[5m]))",
      "legendFormat": "Consume P95"
    }
  ]
}
```

#### 2. Throughput
```json
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
  ]
}
```

#### 3. Error Rate
```json
{
  "title": "Error Rate",
  "targets": [
    {
      "expr": "rate(messagebroker_publish_errors_total[5m])",
      "legendFormat": "Publish Errors"
    },
    {
      "expr": "rate(messagebroker_consume_errors_total[5m])",
      "legendFormat": "Consume Errors"
    }
  ]
}
```

## Alerting Rules

### Sample Prometheus Alert Rules

```yaml
groups:
  - name: messagebroker_alerts
    interval: 30s
    rules:
      # High latency alert
      - alert: HighMessageBrokerLatency
        expr: histogram_quantile(0.95, rate(messagebroker_publish_latency_bucket[5m])) > 1000
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High message broker latency detected"
          description: "P95 publish latency is {{ $value }}ms"

      # High error rate alert
      - alert: HighMessageBrokerErrorRate
        expr: rate(messagebroker_publish_errors_total[5m]) > 10
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "High message broker error rate"
          description: "Error rate is {{ $value }} errors/sec"

      # Connection pool exhaustion
      - alert: ConnectionPoolExhausted
        expr: connectionpool_connections_idle == 0
        for: 1m
        labels:
          severity: warning
        annotations:
          summary: "Connection pool has no idle connections"
          description: "All connections are in use"

      # Queue depth alert
      - alert: HighQueueDepth
        expr: messagebroker_queue_depth > 10000
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High queue depth detected"
          description: "Queue depth is {{ $value }} messages"
```

## Configuration Options

```csharp
public class MetricsOptions
{
    // Enable/disable metrics collection
    public bool Enabled { get; set; } = true;

    // Meter name for OpenTelemetry
    public string MeterName { get; set; } = "Relay.MessageBroker";

    // Meter version
    public string? MeterVersion { get; set; }

    // Enable connection pool metrics
    public bool EnableConnectionPoolMetrics { get; set; } = true;

    // Enable Prometheus export
    public bool EnablePrometheusExport { get; set; } = false;

    // Prometheus endpoint path
    public string PrometheusEndpointPath { get; set; } = "/metrics";

    // Default tenant ID
    public string? DefaultTenantId { get; set; }

    // Broker type identifier
    public string? BrokerType { get; set; }
}
```

## Best Practices

1. **Use Labels Wisely**: Don't create too many unique label combinations (high cardinality)
2. **Set Appropriate Scrape Intervals**: 15-30 seconds is typical for Prometheus
3. **Monitor Resource Usage**: Metrics collection has minimal overhead but monitor in production
4. **Use Aggregation**: Aggregate metrics at query time rather than storing pre-aggregated values
5. **Set Up Alerts**: Configure alerts for critical metrics like error rates and latency
6. **Dashboard Organization**: Group related metrics together in Grafana dashboards
7. **Retention Policies**: Configure appropriate retention periods based on your needs

## Integration with Existing Telemetry

The metrics system integrates seamlessly with the existing `MessageBrokerTelemetryAdapter`:

```csharp
// Metrics are automatically recorded when using BaseMessageBroker
public class MyMessageBroker : BaseMessageBroker
{
    private readonly MessageBrokerMetrics _metrics;

    public MyMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger<MyMessageBroker> logger,
        MessageBrokerMetrics metrics)
        : base(options, logger)
    {
        _metrics = metrics;
    }

    protected override async ValueTask PublishInternalAsync<TMessage>(
        TMessage message,
        byte[] serializedMessage,
        PublishOptions? options,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Publish logic...
            
            _metrics.RecordPublishLatency(
                stopwatch.Elapsed.TotalMilliseconds,
                typeof(TMessage).Name,
                "MyBroker");
                
            _metrics.RecordMessagePublished(
                typeof(TMessage).Name,
                "MyBroker",
                serializedMessage.Length);
        }
        catch (Exception ex)
        {
            _metrics.RecordPublishError(
                typeof(TMessage).Name,
                "MyBroker",
                ex.GetType().Name);
            throw;
        }
    }
}
```

## Troubleshooting

### Metrics Not Appearing

1. Verify metrics are enabled in configuration
2. Check that OpenTelemetry is properly configured
3. Ensure Prometheus endpoint is accessible
4. Verify meter names match in configuration

### High Cardinality Issues

If you see performance issues:
1. Reduce the number of unique label values
2. Avoid using dynamic values as labels (e.g., message IDs)
3. Use aggregation at query time instead of labels

### Missing Labels

Ensure labels are set when recording metrics:
```csharp
_metrics.RecordPublishLatency(
    latency,
    messageType: "OrderCreated",  // Required
    brokerType: "RabbitMQ",       // Required
    tenantId: "tenant-123"        // Optional
);
```
