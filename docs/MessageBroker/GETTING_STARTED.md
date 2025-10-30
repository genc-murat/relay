# Getting Started with Relay.MessageBroker Enhancements

This guide will help you get started with the enterprise-grade enhancements to Relay.MessageBroker, including patterns for reliability, performance, observability, security, and resilience.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Pattern Guides](#pattern-guides)
  - [Outbox Pattern](#outbox-pattern)
  - [Inbox Pattern](#inbox-pattern)
  - [Connection Pooling](#connection-pooling)
  - [Batch Processing](#batch-processing)
  - [Message Deduplication](#message-deduplication)
  - [Health Checks](#health-checks)
  - [Metrics and Telemetry](#metrics-and-telemetry)
  - [Distributed Tracing](#distributed-tracing)
  - [Message Encryption](#message-encryption)
  - [Authentication and Authorization](#authentication-and-authorization)
  - [Rate Limiting](#rate-limiting)
  - [Bulkhead Pattern](#bulkhead-pattern)
  - [Poison Message Handling](#poison-message-handling)
  - [Backpressure Management](#backpressure-management)

## Prerequisites

- .NET 8.0 or later
- One of the supported message brokers:
  - RabbitMQ 3.8+
  - Apache Kafka 2.8+
  - Azure Service Bus
  - AWS SQS/SNS
  - NATS 2.0+
  - Redis 6.0+

## Installation

```bash
dotnet add package Relay.MessageBroker
```

For specific patterns, you may need additional packages:

```bash
# For SQL-based Outbox/Inbox patterns
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# For Azure Key Vault encryption
dotnet add package Azure.Security.KeyVault.Secrets

# For OpenTelemetry
dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore
dotnet add package OpenTelemetry.Exporter.Jaeger
```

## Quick Start

### Basic Setup

```csharp
using Relay.MessageBroker;

var builder = WebApplication.CreateBuilder(args);

// Add RabbitMQ message broker
builder.Services.AddRabbitMQ(options =>
{
    options.HostName = "localhost";
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
});

// Add hosted service for automatic start/stop
builder.Services.AddMessageBrokerHostedService();

var app = builder.Build();
app.Run();
```

### Publishing Messages

```csharp
public class OrderService
{
    private readonly IMessageBroker _messageBroker;

    public OrderService(IMessageBroker messageBroker)
    {
        _messageBroker = messageBroker;
    }

    public async Task CreateOrderAsync(Order order)
    {
        // Publish event
        await _messageBroker.PublishAsync(new OrderCreatedEvent
        {
            OrderId = order.Id,
            Amount = order.Amount,
            CreatedAt = DateTime.UtcNow
        });
    }
}
```

### Subscribing to Messages

```csharp
public class OrderEventHandler : IHostedService
{
    private readonly IMessageBroker _messageBroker;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
            async (message, context, ct) =>
            {
                // Process the message
                await ProcessOrderAsync(message);
                
                // Acknowledge the message
                await context.Acknowledge!();
            },
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

## Pattern Guides

### Outbox Pattern

The Outbox Pattern ensures reliable message publishing by storing messages in a database before sending them to the broker.

**When to use:**
- You need guaranteed message delivery
- You want to avoid dual-write problems
- You need transactional consistency between database and message broker

**Quick Setup:**

```csharp
// Add Outbox pattern with SQL Server
builder.Services.AddOutboxPattern(options =>
{
    options.Enabled = true;
    options.PollingInterval = TimeSpan.FromSeconds(5);
    options.BatchSize = 100;
    options.MaxRetryAttempts = 3;
});

// Configure SQL Server store
builder.Services.AddDbContext<OutboxDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<IOutboxStore, SqlOutboxStore>();
```

**Usage:**

```csharp
// Messages are automatically stored in outbox before publishing
await _messageBroker.PublishAsync(new OrderCreatedEvent { OrderId = 123 });

// Background worker processes outbox and publishes messages
```

[See detailed Outbox Pattern guide →](./patterns/OUTBOX_PATTERN.md)

### Inbox Pattern

The Inbox Pattern ensures idempotent message processing by tracking processed messages.

**When to use:**
- You need to prevent duplicate message processing
- You want at-least-once delivery semantics with idempotency
- You need to handle message redelivery gracefully

**Quick Setup:**

```csharp
// Add Inbox pattern with PostgreSQL
builder.Services.AddInboxPattern(options =>
{
    options.Enabled = true;
    options.RetentionPeriod = TimeSpan.FromDays(7);
    options.CleanupInterval = TimeSpan.FromHours(1);
});

// Configure PostgreSQL store
builder.Services.AddDbContext<InboxDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IInboxStore, SqlInboxStore>();
```

**Usage:**

```csharp
// Messages are automatically checked against inbox before processing
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
    async (message, context, ct) =>
    {
        // This will only execute once per unique message ID
        await ProcessOrderAsync(message);
        await context.Acknowledge!();
    });
```

[See detailed Inbox Pattern guide →](./patterns/INBOX_PATTERN.md)

### Connection Pooling

Connection pooling improves performance by reusing broker connections.

**When to use:**
- You have high message throughput
- You want to reduce connection overhead
- You need better resource utilization

**Quick Setup:**

```csharp
builder.Services.AddConnectionPooling<IConnection>(options =>
{
    options.MinPoolSize = 5;
    options.MaxPoolSize = 50;
    options.ConnectionTimeout = TimeSpan.FromSeconds(5);
    options.ValidationInterval = TimeSpan.FromSeconds(30);
    options.IdleTimeout = TimeSpan.FromMinutes(5);
});
```

**Usage:**

```csharp
// Connections are automatically pooled
// No code changes required - works transparently
```

[See detailed Connection Pooling guide →](./performance/CONNECTION_POOLING.md)

### Batch Processing

Batch processing optimizes throughput by publishing multiple messages together.

**When to use:**
- You have high-volume message scenarios
- You want to reduce network overhead
- You need better throughput

**Quick Setup:**

```csharp
builder.Services.AddBatchProcessing(options =>
{
    options.Enabled = true;
    options.MaxBatchSize = 100;
    options.FlushInterval = TimeSpan.FromMilliseconds(100);
    options.EnableCompression = true;
});
```

**Usage:**

```csharp
// Messages are automatically batched
await _messageBroker.PublishAsync(new OrderCreatedEvent { OrderId = 1 });
await _messageBroker.PublishAsync(new OrderCreatedEvent { OrderId = 2 });
await _messageBroker.PublishAsync(new OrderCreatedEvent { OrderId = 3 });
// All three messages sent in a single batch
```

[See detailed Batch Processing guide →](./performance/BATCH_PROCESSING.md)

### Message Deduplication

Deduplication prevents duplicate messages from being published.

**When to use:**
- You want to prevent duplicate message publishing
- You need content-based deduplication
- You want to reduce unnecessary message processing

**Quick Setup:**

```csharp
builder.Services.AddDeduplication(options =>
{
    options.Enabled = true;
    options.Window = TimeSpan.FromMinutes(5);
    options.MaxCacheSize = 100000;
    options.Strategy = DeduplicationStrategy.ContentHash;
});
```

**Usage:**

```csharp
// Duplicate messages are automatically discarded
await _messageBroker.PublishAsync(new OrderCreatedEvent { OrderId = 123 });
await _messageBroker.PublishAsync(new OrderCreatedEvent { OrderId = 123 }); // Discarded
```

[See detailed Deduplication guide →](./performance/DEDUPLICATION.md)

### Health Checks

Health checks provide operational status monitoring.

**When to use:**
- You need to monitor broker connectivity
- You want integration with Kubernetes/Docker health checks
- You need operational visibility

**Quick Setup:**

```csharp
builder.Services.AddMessageBrokerHealthChecks(options =>
{
    options.CheckInterval = TimeSpan.FromSeconds(30);
    options.Timeout = TimeSpan.FromSeconds(2);
});

// Add health check endpoint
app.MapHealthChecks("/health");
```

**Usage:**

```bash
# Check health status
curl http://localhost:5000/health

# Response:
# {
#   "status": "Healthy",
#   "results": {
#     "message_broker": {
#       "status": "Healthy",
#       "data": {
#         "broker_connected": true,
#         "circuit_breaker_state": "Closed",
#         "pool_active_connections": 5
#       }
#     }
#   }
# }
```

[See detailed Health Checks guide →](./observability/HEALTH_CHECKS.md)

### Metrics and Telemetry

Metrics provide performance and operational insights.

**When to use:**
- You need to monitor message throughput and latency
- You want to track error rates
- You need performance metrics

**Quick Setup:**

```csharp
builder.Services.AddMessageBrokerMetrics(options =>
{
    options.EnableMetrics = true;
    options.EnablePrometheusExporter = true;
});

// Add Prometheus endpoint
app.MapPrometheusScrapingEndpoint("/metrics");
```

**Usage:**

```csharp
// Metrics are automatically collected
// Access via Prometheus endpoint: http://localhost:5000/metrics
```

[See detailed Metrics guide →](./observability/METRICS.md)

### Distributed Tracing

Distributed tracing enables end-to-end request tracking across services.

**When to use:**
- You have microservices architecture
- You need to trace message flows
- You want to debug distributed systems

**Quick Setup:**

```csharp
builder.Services.AddDistributedTracing(options =>
{
    options.ServiceName = "OrderService";
    options.EnableTracing = true;
    options.SamplingRate = 0.1; // 10% sampling
    options.Exporters = new[]
    {
        TracingExporter.Jaeger,
        TracingExporter.Zipkin,
        TracingExporter.OTLP
    };
});
```

**Usage:**

```csharp
// Trace context is automatically propagated
// View traces in Jaeger UI: http://localhost:16686
```

[See detailed Distributed Tracing guide →](./observability/DISTRIBUTED_TRACING.md)

### Message Encryption

Encryption protects sensitive message data.

**When to use:**
- You handle sensitive data (PII, financial data)
- You need compliance (GDPR, HIPAA, PCI-DSS)
- You want end-to-end encryption

**Quick Setup:**

```csharp
builder.Services.AddMessageEncryption(options =>
{
    options.EnableEncryption = true;
    options.EncryptionAlgorithm = "AES256";
    options.KeyProvider = KeyProviderType.AzureKeyVault;
    options.KeyVaultUrl = "https://myvault.vault.azure.net/";
});
```

**Usage:**

```csharp
// Messages are automatically encrypted before publishing
await _messageBroker.PublishAsync(new SensitiveDataEvent
{
    CustomerId = 123,
    CreditCardNumber = "4111111111111111"
});
// Encrypted payload sent to broker

// Messages are automatically decrypted after consuming
await _messageBroker.SubscribeAsync<SensitiveDataEvent>(
    async (message, context, ct) =>
    {
        // message.CreditCardNumber is decrypted
        await ProcessSensitiveDataAsync(message);
    });
```

[See detailed Encryption guide →](./security/ENCRYPTION.md)

### Authentication and Authorization

Authentication and authorization control access to message operations.

**When to use:**
- You need to control who can publish/subscribe
- You want role-based access control
- You need audit trails for security

**Quick Setup:**

```csharp
builder.Services.AddMessageBrokerSecurity(options =>
{
    options.EnableAuthentication = true;
    options.JwtIssuer = "https://auth.example.com";
    options.JwtAudience = "message-broker";
    options.EnableAuthorization = true;
    options.Roles = new Dictionary<string, string[]>
    {
        ["admin"] = new[] { "publish:*", "subscribe:*" },
        ["publisher"] = new[] { "publish:orders.*" },
        ["consumer"] = new[] { "subscribe:orders.*" }
    };
});
```

**Usage:**

```csharp
// Publish with authentication token
await _messageBroker.PublishAsync(
    new OrderCreatedEvent { OrderId = 123 },
    new PublishOptions
    {
        Headers = new Dictionary<string, object>
        {
            ["Authorization"] = $"Bearer {jwtToken}"
        }
    });
```

[See detailed Authentication guide →](./security/AUTHENTICATION.md)

### Rate Limiting

Rate limiting prevents resource exhaustion and ensures fair usage.

**When to use:**
- You need to prevent abuse
- You want to ensure fair resource allocation
- You need per-tenant rate limits

**Quick Setup:**

```csharp
builder.Services.AddRateLimiting(options =>
{
    options.Enabled = true;
    options.RequestsPerSecond = 1000;
    options.Strategy = RateLimitStrategy.TokenBucket;
    options.EnablePerTenantLimits = true;
});
```

**Usage:**

```csharp
// Rate limiting is automatically applied
try
{
    await _messageBroker.PublishAsync(new OrderCreatedEvent { OrderId = 123 });
}
catch (RateLimitExceededException ex)
{
    // Handle rate limit exceeded
    await Task.Delay(ex.RetryAfter);
    // Retry
}
```

[See detailed Rate Limiting guide →](./security/RATE_LIMITING.md)

### Bulkhead Pattern

The Bulkhead Pattern isolates resources to prevent cascading failures.

**When to use:**
- You want to prevent resource exhaustion
- You need to isolate failures
- You want to limit concurrent operations

**Quick Setup:**

```csharp
builder.Services.AddBulkhead(options =>
{
    options.Enabled = true;
    options.MaxConcurrentOperations = 100;
    options.MaxQueuedOperations = 1000;
});
```

**Usage:**

```csharp
// Bulkhead is automatically applied
try
{
    await _messageBroker.PublishAsync(new OrderCreatedEvent { OrderId = 123 });
}
catch (BulkheadRejectedException ex)
{
    // Handle bulkhead full
    _logger.LogWarning("Bulkhead full, operation rejected");
}
```

[See detailed Bulkhead Pattern guide →](./resilience/BULKHEAD.md)

### Poison Message Handling

Poison message handling prevents repeatedly failing messages from blocking the system.

**When to use:**
- You need to handle messages that consistently fail
- You want to prevent message processing loops
- You need to analyze failed messages

**Quick Setup:**

```csharp
builder.Services.AddPoisonMessageHandling(options =>
{
    options.Enabled = true;
    options.FailureThreshold = 5;
    options.RetentionPeriod = TimeSpan.FromDays(7);
});
```

**Usage:**

```csharp
// Poison messages are automatically detected and moved
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
    async (message, context, ct) =>
    {
        // If this fails 5 times, message moves to poison queue
        await ProcessOrderAsync(message);
        await context.Acknowledge!();
    });

// Retrieve poison messages for analysis
var poisonMessages = await _poisonMessageHandler.GetPoisonMessagesAsync();

// Reprocess a poison message
await _poisonMessageHandler.ReprocessAsync(messageId);
```

[See detailed Poison Message Handling guide →](./resilience/POISON_MESSAGES.md)

### Backpressure Management

Backpressure management handles situations when consumers can't keep up with message production.

**When to use:**
- You have variable message processing rates
- You want to prevent consumer overload
- You need automatic throttling

**Quick Setup:**

```csharp
builder.Services.AddBackpressure(options =>
{
    options.Enabled = true;
    options.LatencyThreshold = TimeSpan.FromSeconds(5);
    options.QueueDepthThreshold = 10000;
    options.RecoveryLatencyThreshold = TimeSpan.FromSeconds(2);
});
```

**Usage:**

```csharp
// Backpressure is automatically applied
// Consumption rate is reduced when thresholds are exceeded
// Automatically recovers when conditions improve
```

[See detailed Backpressure Management guide →](./resilience/BACKPRESSURE.md)

## Next Steps

- [Configuration Guide](./CONFIGURATION.md) - Detailed configuration options
- [Best Practices](./BEST_PRACTICES.md) - Production deployment guidelines
- [Troubleshooting](./TROUBLESHOOTING.md) - Common issues and solutions
- [Migration Guide](./MIGRATION.md) - Migrating from existing implementations
- [Code Examples](./examples/) - Complete working examples
- [Sample Applications](../../samples/) - Full sample applications

## Support

- [GitHub Issues](https://github.com/your-org/relay/issues)
- [Documentation](https://docs.relay.dev)
- [Examples](https://github.com/your-org/relay/tree/main/samples)
