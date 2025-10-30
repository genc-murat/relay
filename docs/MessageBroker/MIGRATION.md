# Migration Guide

This guide helps you migrate from existing message broker implementations to Relay.MessageBroker with enhancements.

## Table of Contents

- [Migration Overview](#migration-overview)
- [Pre-Migration Checklist](#pre-migration-checklist)
- [Migration Strategies](#migration-strategies)
- [Step-by-Step Migration](#step-by-step-migration)
- [Feature-Specific Migration](#feature-specific-migration)
- [Testing Migration](#testing-migration)
- [Rollback Plan](#rollback-plan)
- [Post-Migration](#post-migration)

## Migration Overview

### What's New

The enhanced Relay.MessageBroker adds:

- **Reliability**: Outbox and Inbox patterns
- **Performance**: Connection pooling, batching, deduplication
- **Observability**: Health checks, metrics, distributed tracing
- **Security**: Encryption, authentication, authorization, rate limiting
- **Resilience**: Bulkhead, poison message handling, backpressure

### Compatibility

- ✅ Backward compatible with existing Relay.MessageBroker code
- ✅ All new features are opt-in
- ✅ No breaking changes to existing APIs
- ✅ Gradual migration supported

## Pre-Migration Checklist

### 1. Assess Current Implementation

```csharp
// Document current setup
- [ ] Message broker type (RabbitMQ, Kafka, etc.)
- [ ] Message volume (messages/second)
- [ ] Message size (average, max)
- [ ] Number of publishers
- [ ] Number of consumers
- [ ] Current reliability guarantees
- [ ] Current monitoring setup
- [ ] Current security measures
```

### 2. Identify Requirements

```csharp
// Determine which features you need
- [ ] Outbox pattern (transactional messaging)
- [ ] Inbox pattern (idempotency)
- [ ] Connection pooling (performance)
- [ ] Batch processing (high throughput)
- [ ] Deduplication (duplicate prevention)
- [ ] Health checks (monitoring)
- [ ] Metrics (observability)
- [ ] Distributed tracing (debugging)
- [ ] Encryption (security)
- [ ] Authentication (access control)
- [ ] Rate limiting (protection)
- [ ] Bulkhead (isolation)
- [ ] Poison message handling (resilience)
- [ ] Backpressure (load management)
```

### 3. Plan Infrastructure

```csharp
// Infrastructure requirements
- [ ] Database for Outbox/Inbox (SQL Server, PostgreSQL)
- [ ] Metrics collection (Prometheus)
- [ ] Tracing backend (Jaeger, Zipkin)
- [ ] Key management (Azure Key Vault)
- [ ] Identity provider (Azure AD, OAuth2)
```

### 4. Backup Current System

```bash
# Backup configuration
cp appsettings.json appsettings.json.backup

# Backup database
pg_dump -h localhost -U user -d database > backup.sql

# Document current message schemas
# Document current routing rules
# Document current error handling
```

## Migration Strategies

### Strategy 1: Big Bang Migration

**When to use:**
- Small system
- Low message volume
- Can afford downtime
- Simple setup

**Steps:**
1. Stop current system
2. Deploy new version with enhancements
3. Start new system
4. Verify functionality

**Pros:**
- Simple and fast
- Clean cutover

**Cons:**
- Requires downtime
- Higher risk
- No gradual rollout

### Strategy 2: Gradual Migration (Recommended)

**When to use:**
- Production system
- High message volume
- Zero-downtime requirement
- Complex setup

**Steps:**
1. Deploy new version alongside old
2. Enable features one by one
3. Monitor and validate
4. Gradually shift traffic
5. Decommission old system

**Pros:**
- Zero downtime
- Lower risk
- Easy rollback
- Gradual validation

**Cons:**
- More complex
- Longer migration period
- Requires parallel running

### Strategy 3: Feature-by-Feature Migration

**When to use:**
- Want to test each feature
- Have time for gradual rollout
- Need to validate each enhancement

**Steps:**
1. Enable one feature at a time
2. Monitor and validate
3. Move to next feature
4. Complete when all features enabled

**Pros:**
- Very low risk
- Easy to identify issues
- Thorough validation

**Cons:**
- Longest migration period
- Most complex

## Step-by-Step Migration

### Phase 1: Preparation (Week 1)

#### 1.1 Update Dependencies

```bash
# Update to latest version
dotnet add package Relay.MessageBroker --version 2.0.0

# Add required dependencies
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore
```

#### 1.2 Update Configuration

```csharp
// appsettings.json
{
  "MessageBroker": {
    "BrokerType": "RabbitMQ", // Keep existing
    "RabbitMQ": {
      // Keep existing configuration
      "HostName": "localhost",
      "Port": 5672
    },
    // Add new sections (disabled initially)
    "Outbox": {
      "Enabled": false
    },
    "Inbox": {
      "Enabled": false
    },
    "Metrics": {
      "EnableMetrics": false
    }
  }
}
```

#### 1.3 Deploy Without Enabling Features

```csharp
// Program.cs - No changes needed yet
builder.Services.AddRabbitMQ(options =>
{
    options.HostName = "localhost";
    options.Port = 5672;
});

// Deploy and verify existing functionality works
```

### Phase 2: Enable Observability (Week 2)

#### 2.1 Enable Health Checks

```csharp
// Program.cs
builder.Services.AddMessageBrokerHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/health");
```

**Validation:**
```bash
# Test health endpoint
curl http://localhost:5000/health

# Expected: {"status":"Healthy"}
```

#### 2.2 Enable Metrics

```csharp
// Program.cs
builder.Services.AddMessageBrokerMetrics(options =>
{
    options.EnableMetrics = true;
    options.EnablePrometheusExporter = true;
});

app.MapPrometheusScrapingEndpoint("/metrics");
```

**Validation:**
```bash
# Test metrics endpoint
curl http://localhost:5000/metrics

# Expected: Prometheus metrics output
```

#### 2.3 Enable Distributed Tracing

```csharp
// Program.cs
builder.Services.AddDistributedTracing(options =>
{
    options.ServiceName = "OrderService";
    options.EnableTracing = true;
    options.SamplingRate = 0.1; // Start with 10%
    options.Exporters = new[] { TracingExporter.Jaeger };
    options.JaegerAgentHost = "localhost";
});
```

**Validation:**
- Check Jaeger UI for traces
- Verify trace propagation across services

### Phase 3: Enable Performance Features (Week 3)

#### 3.1 Enable Connection Pooling

```csharp
// Program.cs
builder.Services.AddConnectionPooling<IConnection>(options =>
{
    options.MinPoolSize = 5;
    options.MaxPoolSize = 50;
});
```

**Validation:**
- Monitor connection pool metrics
- Verify performance improvement
- Check for connection leaks

#### 3.2 Enable Batch Processing

```csharp
// Program.cs
builder.Services.AddBatchProcessing(options =>
{
    options.Enabled = true;
    options.MaxBatchSize = 100;
    options.FlushInterval = TimeSpan.FromMilliseconds(100);
    options.EnableCompression = true;
});
```

**Validation:**
- Monitor batch metrics
- Verify throughput improvement
- Check compression ratio

#### 3.3 Enable Deduplication

```csharp
// Program.cs
builder.Services.AddDeduplication(options =>
{
    options.Enabled = true;
    options.Window = TimeSpan.FromMinutes(5);
    options.Strategy = DeduplicationStrategy.ContentHash;
});
```

**Validation:**
- Monitor deduplication metrics
- Verify duplicates are detected
- Check cache size

### Phase 4: Enable Reliability Features (Week 4)

#### 4.1 Setup Database

```sql
-- Create database
CREATE DATABASE MessageBroker;

-- Run migrations
dotnet ef database update --context OutboxDbContext
dotnet ef database update --context InboxDbContext
```

#### 4.2 Enable Outbox Pattern

```csharp
// Program.cs
builder.Services.AddDbContext<OutboxDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddOutboxPattern(options =>
{
    options.Enabled = true;
    options.PollingInterval = TimeSpan.FromSeconds(5);
    options.BatchSize = 100;
});

builder.Services.AddScoped<IOutboxStore, SqlOutboxStore>();
builder.Services.AddHostedService<OutboxWorker>();
```

**Validation:**
- Publish messages and verify they go through outbox
- Check outbox table for pending messages
- Verify messages are processed
- Test database transaction rollback

#### 4.3 Enable Inbox Pattern

```csharp
// Program.cs
builder.Services.AddDbContext<InboxDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddInboxPattern(options =>
{
    options.Enabled = true;
    options.RetentionPeriod = TimeSpan.FromDays(7);
});

builder.Services.AddScoped<IInboxStore, SqlInboxStore>();
builder.Services.AddHostedService<InboxCleanupWorker>();
```

**Validation:**
- Send duplicate messages
- Verify only one is processed
- Check inbox table for entries
- Verify cleanup works

### Phase 5: Enable Security Features (Week 5)

#### 5.1 Enable Encryption

```csharp
// Program.cs
builder.Services.AddMessageEncryption(options =>
{
    options.EnableEncryption = true;
    options.EncryptionAlgorithm = "AES256";
    options.KeyProvider = KeyProviderType.EnvironmentVariable;
    options.KeyEnvironmentVariable = "MESSAGE_ENCRYPTION_KEY";
});
```

**Validation:**
- Verify messages are encrypted in broker
- Verify messages are decrypted correctly
- Test key rotation

#### 5.2 Enable Authentication

```csharp
// Program.cs
builder.Services.AddMessageBrokerSecurity(options =>
{
    options.EnableAuthentication = true;
    options.JwtIssuer = "https://auth.example.com";
    options.JwtAudience = "message-broker";
    options.EnableAuthorization = true;
    options.Roles = new Dictionary<string, string[]>
    {
        ["admin"] = new[] { "publish:*", "subscribe:*" },
        ["service"] = new[] { "publish:orders.*", "subscribe:orders.*" }
    };
});
```

**Validation:**
- Test with valid token
- Test with invalid token
- Test with insufficient permissions
- Verify audit logs

#### 5.3 Enable Rate Limiting

```csharp
// Program.cs
builder.Services.AddRateLimiting(options =>
{
    options.Enabled = true;
    options.RequestsPerSecond = 1000;
    options.Strategy = RateLimitStrategy.TokenBucket;
});
```

**Validation:**
- Send messages at high rate
- Verify rate limiting kicks in
- Check rate limit metrics

### Phase 6: Enable Resilience Features (Week 6)

#### 6.1 Enable Bulkhead

```csharp
// Program.cs
builder.Services.AddBulkhead(options =>
{
    options.Enabled = true;
    options.MaxConcurrentOperations = 100;
    options.MaxQueuedOperations = 1000;
});
```

**Validation:**
- Send many concurrent messages
- Verify bulkhead limits are enforced
- Check bulkhead metrics

#### 6.2 Enable Poison Message Handling

```csharp
// Program.cs
builder.Services.AddPoisonMessageHandling(options =>
{
    options.Enabled = true;
    options.FailureThreshold = 5;
    options.RetentionPeriod = TimeSpan.FromDays(7);
});
```

**Validation:**
- Send messages that fail processing
- Verify they move to poison queue after threshold
- Test reprocessing

#### 6.3 Enable Backpressure

```csharp
// Program.cs
builder.Services.AddBackpressure(options =>
{
    options.Enabled = true;
    options.LatencyThreshold = TimeSpan.FromSeconds(5);
    options.QueueDepthThreshold = 10000;
});
```

**Validation:**
- Simulate slow consumer
- Verify backpressure is detected
- Verify consumption rate is reduced
- Verify recovery when conditions improve

## Feature-Specific Migration

### Migrating from Manual Outbox Implementation

**Before:**
```csharp
// Manual outbox implementation
using var transaction = await _dbContext.Database.BeginTransactionAsync();
try
{
    await _dbContext.Orders.AddAsync(order);
    await _dbContext.OutboxMessages.AddAsync(new OutboxMessage
    {
        Payload = JsonSerializer.Serialize(orderEvent)
    });
    await _dbContext.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
}

// Separate worker to process outbox
public class CustomOutboxWorker : BackgroundService
{
    // Custom implementation
}
```

**After:**
```csharp
// Use built-in outbox pattern
builder.Services.AddOutboxPattern(options =>
{
    options.Enabled = true;
});

// Just publish - outbox is automatic
await _messageBroker.PublishAsync(orderEvent);
```

### Migrating from Manual Deduplication

**Before:**
```csharp
// Manual deduplication
private readonly ConcurrentDictionary<string, DateTime> _processedMessages = new();

public async Task ProcessMessageAsync(OrderCreatedEvent message)
{
    var hash = ComputeHash(message);
    
    if (_processedMessages.TryGetValue(hash, out var timestamp))
    {
        if (DateTime.UtcNow - timestamp < TimeSpan.FromMinutes(5))
        {
            return; // Duplicate
        }
    }
    
    _processedMessages[hash] = DateTime.UtcNow;
    await ProcessOrderAsync(message);
}
```

**After:**
```csharp
// Use built-in deduplication
builder.Services.AddDeduplication(options =>
{
    options.Enabled = true;
    options.Window = TimeSpan.FromMinutes(5);
});

// Deduplication is automatic
await _messageBroker.PublishAsync(orderEvent);
```

### Migrating from Custom Metrics

**Before:**
```csharp
// Custom metrics
private readonly Counter _messagesPublished;
private readonly Histogram _publishLatency;

public async Task PublishAsync<T>(T message)
{
    var sw = Stopwatch.StartNew();
    try
    {
        await _broker.PublishAsync(message);
        _messagesPublished.Inc();
    }
    finally
    {
        _publishLatency.Observe(sw.Elapsed.TotalSeconds);
    }
}
```

**After:**
```csharp
// Use built-in metrics
builder.Services.AddMessageBrokerMetrics(options =>
{
    options.EnableMetrics = true;
});

// Metrics are automatic
await _messageBroker.PublishAsync(message);
```

## Testing Migration

### Unit Tests

```csharp
public class MigrationTests
{
    [Fact]
    public async Task OutboxPattern_StoresAndProcessesMessages()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOutboxPattern(options => options.Enabled = true);
        services.AddSingleton<IOutboxStore, InMemoryOutboxStore>();
        var provider = services.BuildServiceProvider();
        
        // Act
        await _messageBroker.PublishAsync(new OrderCreatedEvent { OrderId = 123 });
        
        // Assert
        var outboxStore = provider.GetRequiredService<IOutboxStore>();
        var pending = await outboxStore.GetPendingAsync(10);
        Assert.Single(pending);
    }
    
    [Fact]
    public async Task InboxPattern_PreventsDuplicates()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInboxPattern(options => options.Enabled = true);
        services.AddSingleton<IInboxStore, InMemoryInboxStore>();
        var provider = services.BuildServiceProvider();
        
        var processCount = 0;
        await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
            async (message, context, ct) =>
            {
                processCount++;
                await context.Acknowledge!();
            });
        
        // Act
        var messageId = Guid.NewGuid().ToString();
        await PublishWithIdAsync(messageId);
        await PublishWithIdAsync(messageId); // Duplicate
        
        // Assert
        Assert.Equal(1, processCount);
    }
}
```

### Integration Tests

```csharp
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task EndToEnd_WithAllFeatures()
    {
        // Arrange
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Enable all features
                    services.AddOutboxPattern(options => options.Enabled = true);
                    services.AddInboxPattern(options => options.Enabled = true);
                    services.AddBatchProcessing(options => options.Enabled = true);
                    services.AddDeduplication(options => options.Enabled = true);
                });
            });
        
        var client = factory.CreateClient();
        
        // Act
        var response = await client.PostAsync("/orders", new StringContent(
            JsonSerializer.Serialize(new { orderId = 123 }),
            Encoding.UTF8,
            "application/json"));
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Verify message was processed
        await Task.Delay(TimeSpan.FromSeconds(5));
        var order = await _dbContext.Orders.FindAsync(123);
        Assert.NotNull(order);
    }
}
```

### Load Tests

```javascript
// k6 load test
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '2m', target: 100 },
    { duration: '5m', target: 100 },
    { duration: '2m', target: 200 },
    { duration: '5m', target: 200 },
    { duration: '2m', target: 0 },
  ],
};

export default function () {
  let response = http.post('http://localhost:5000/orders', JSON.stringify({
    orderId: Math.floor(Math.random() * 1000000),
    amount: 99.99
  }), {
    headers: { 'Content-Type': 'application/json' },
  });
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'latency < 500ms': (r) => r.timings.duration < 500,
  });
  
  sleep(1);
}
```

## Rollback Plan

### Immediate Rollback

If issues are detected immediately after deployment:

```bash
# 1. Revert to previous version
kubectl rollout undo deployment/order-service

# 2. Disable new features
kubectl set env deployment/order-service \
  MESSAGEBROKER__OUTBOX__ENABLED=false \
  MESSAGEBROKER__INBOX__ENABLED=false

# 3. Verify old version is working
curl http://localhost:5000/health
```

### Gradual Rollback

If issues are detected after some time:

```csharp
// 1. Disable features one by one
builder.Services.AddOutboxPattern(options =>
{
    options.Enabled = false; // Disable outbox
});

// 2. Monitor and verify
// 3. Continue disabling features as needed
```

### Data Rollback

If database changes need to be reverted:

```sql
-- Backup current state
pg_dump -h localhost -U user -d database > backup_after_migration.sql

-- Restore previous state
psql -h localhost -U user -d database < backup_before_migration.sql

-- Or drop new tables
DROP TABLE OutboxMessages;
DROP TABLE InboxMessages;
```

## Post-Migration

### 1. Monitoring

Set up monitoring for:
- Message throughput
- Processing latency
- Error rates
- Queue depths
- Resource utilization

### 2. Optimization

Tune configuration based on metrics:
- Adjust connection pool sizes
- Optimize batch sizes
- Tune rate limits
- Adjust bulkhead limits

### 3. Documentation

Update documentation:
- Architecture diagrams
- Configuration guide
- Runbooks
- Troubleshooting guide

### 4. Training

Train team on:
- New features
- Configuration options
- Monitoring dashboards
- Troubleshooting procedures

### 5. Cleanup

Remove old code:
- Delete custom outbox implementation
- Remove manual deduplication
- Clean up custom metrics
- Remove temporary migration code

## Migration Checklist

```
Pre-Migration:
- [ ] Assess current implementation
- [ ] Identify requirements
- [ ] Plan infrastructure
- [ ] Backup current system
- [ ] Update dependencies
- [ ] Update configuration

Phase 1 - Observability:
- [ ] Enable health checks
- [ ] Enable metrics
- [ ] Enable distributed tracing
- [ ] Verify monitoring

Phase 2 - Performance:
- [ ] Enable connection pooling
- [ ] Enable batch processing
- [ ] Enable deduplication
- [ ] Verify performance improvement

Phase 3 - Reliability:
- [ ] Setup database
- [ ] Enable outbox pattern
- [ ] Enable inbox pattern
- [ ] Verify reliability

Phase 4 - Security:
- [ ] Enable encryption
- [ ] Enable authentication
- [ ] Enable rate limiting
- [ ] Verify security

Phase 5 - Resilience:
- [ ] Enable bulkhead
- [ ] Enable poison message handling
- [ ] Enable backpressure
- [ ] Verify resilience

Post-Migration:
- [ ] Setup monitoring
- [ ] Optimize configuration
- [ ] Update documentation
- [ ] Train team
- [ ] Cleanup old code
- [ ] Celebrate! 🎉
```

## Support

Need help with migration?

- [GitHub Issues](https://github.com/your-org/relay/issues)
- [Documentation](https://docs.relay.dev)
- [Migration Examples](./examples/migration/)
- Contact support

## Next Steps

- [Configuration Guide](./CONFIGURATION.md)
- [Best Practices](./BEST_PRACTICES.md)
- [Troubleshooting](./TROUBLESHOOTING.md)
