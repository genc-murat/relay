# Best Practices for Production Deployments

This guide provides best practices and recommendations for deploying Relay.MessageBroker enhancements in production environments.

## Table of Contents

- [General Best Practices](#general-best-practices)
- [Reliability Patterns](#reliability-patterns)
- [Performance Optimization](#performance-optimization)
- [Security](#security)
- [Observability](#observability)
- [Resilience](#resilience)
- [Deployment](#deployment)
- [Monitoring and Alerting](#monitoring-and-alerting)
- [Capacity Planning](#capacity-planning)
- [Disaster Recovery](#disaster-recovery)

## General Best Practices

### 1. Use Configuration Profiles

Separate configuration for different environments:

```csharp
// appsettings.Development.json
{
  "MessageBroker": {
    "Outbox": { "Enabled": true, "PollingInterval": "00:00:01" },
    "Tracing": { "SamplingRate": 1.0, "CaptureMessagePayloads": true }
  }
}

// appsettings.Production.json
{
  "MessageBroker": {
    "Outbox": { "Enabled": true, "PollingInterval": "00:00:05" },
    "Tracing": { "SamplingRate": 0.1, "CaptureMessagePayloads": false }
  }
}
```

### 2. Enable Health Checks

Always enable health checks for production:

```csharp
builder.Services.AddMessageBrokerHealthChecks();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");
```

### 3. Use Structured Logging

Use structured logging for better observability:

```csharp
_logger.LogInformation(
    "Message published: {MessageType} {MessageId} {TenantId}",
    message.GetType().Name,
    messageId,
    tenantId);
```

### 4. Implement Graceful Shutdown

Ensure graceful shutdown to prevent message loss:

```csharp
public class MessageBrokerHostedService : IHostedService
{
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Flush pending batches
        await _batchProcessor.FlushAsync(cancellationToken);
        
        // Process remaining outbox messages
        await _outboxWorker.ProcessRemainingAsync(cancellationToken);
        
        // Close connections
        await _messageBroker.StopAsync(cancellationToken);
    }
}
```

### 5. Use Correlation IDs

Always use correlation IDs for request tracing:

```csharp
await _messageBroker.PublishAsync(
    message,
    new PublishOptions
    {
        Headers = new Dictionary<string, object>
        {
            ["CorrelationId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            ["CausationId"] = messageContext.MessageId
        }
    });
```

## Reliability Patterns

### 1. Outbox Pattern

**Always use Outbox pattern for critical messages:**

```csharp
// ✅ Good: Transactional consistency
using var transaction = await _dbContext.Database.BeginTransactionAsync();
try
{
    // Save entity
    await _dbContext.Orders.AddAsync(order);
    await _dbContext.SaveChangesAsync();
    
    // Store in outbox (same transaction)
    await _outboxStore.StoreAsync(new OutboxMessage
    {
        MessageType = typeof(OrderCreatedEvent).Name,
        Payload = JsonSerializer.SerializeToUtf8Bytes(orderEvent)
    });
    
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

// ❌ Bad: Dual-write problem
await _dbContext.Orders.AddAsync(order);
await _dbContext.SaveChangesAsync();
await _messageBroker.PublishAsync(orderEvent); // Can fail independently
```

**Configure appropriate retry settings:**

```csharp
builder.Services.AddOutboxPattern(options =>
{
    options.MaxRetryAttempts = 5;
    options.RetryDelay = TimeSpan.FromSeconds(2);
    options.UseExponentialBackoff = true; // 2s, 4s, 8s, 16s, 32s
});
```

### 2. Inbox Pattern

**Use Inbox pattern for idempotent processing:**

```csharp
// ✅ Good: Idempotent handler
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
    async (message, context, ct) =>
    {
        // Inbox automatically checks for duplicates
        await ProcessOrderAsync(message);
        await context.Acknowledge!();
    });

// ❌ Bad: Non-idempotent handler
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
    async (message, context, ct) =>
    {
        // This will execute multiple times for duplicate messages
        await _dbContext.Orders.AddAsync(order); // Duplicate!
        await context.Acknowledge!();
    });
```

**Set appropriate retention periods:**

```csharp
builder.Services.AddInboxPattern(options =>
{
    // Retain for longer than max message redelivery window
    options.RetentionPeriod = TimeSpan.FromDays(7);
    
    // Clean up regularly
    options.CleanupInterval = TimeSpan.FromHours(1);
});
```

### 3. Error Handling

**Implement proper error handling:**

```csharp
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
    async (message, context, ct) =>
    {
        try
        {
            await ProcessOrderAsync(message);
            await context.Acknowledge!();
        }
        catch (TransientException ex)
        {
            // Transient error - requeue for retry
            _logger.LogWarning(ex, "Transient error processing order {OrderId}", message.OrderId);
            await context.Reject!(requeue: true);
        }
        catch (ValidationException ex)
        {
            // Permanent error - don't requeue
            _logger.LogError(ex, "Validation error processing order {OrderId}", message.OrderId);
            await context.Reject!(requeue: false);
        }
        catch (Exception ex)
        {
            // Unknown error - log and decide
            _logger.LogError(ex, "Error processing order {OrderId}", message.OrderId);
            
            // Check failure count
            if (context.DeliveryCount >= 5)
            {
                // Too many failures - don't requeue
                await context.Reject!(requeue: false);
            }
            else
            {
                // Retry
                await context.Reject!(requeue: true);
            }
        }
    });
```

## Performance Optimization

### 1. Connection Pooling

**Configure connection pool based on workload:**

```csharp
// High-throughput scenario
builder.Services.AddConnectionPooling<IConnection>(options =>
{
    options.MinPoolSize = 20;
    options.MaxPoolSize = 200;
    options.ConnectionTimeout = TimeSpan.FromSeconds(10);
});

// Low-throughput scenario
builder.Services.AddConnectionPooling<IConnection>(options =>
{
    options.MinPoolSize = 5;
    options.MaxPoolSize = 20;
    options.ConnectionTimeout = TimeSpan.FromSeconds(5);
});
```

**Monitor pool metrics:**

```csharp
var metrics = _connectionPool.GetMetrics();
if (metrics.WaitTime > TimeSpan.FromSeconds(1))
{
    _logger.LogWarning("High connection wait time: {WaitTime}ms", metrics.WaitTime.TotalMilliseconds);
}
```

### 2. Batch Processing

**Use batching for high-volume scenarios:**

```csharp
builder.Services.AddBatchProcessing(options =>
{
    // Optimize batch size based on message size
    options.MaxBatchSize = 500; // For small messages
    // options.MaxBatchSize = 50; // For large messages
    
    // Optimize flush interval based on latency requirements
    options.FlushInterval = TimeSpan.FromMilliseconds(100); // Low latency
    // options.FlushInterval = TimeSpan.FromSeconds(1); // High throughput
    
    // Enable compression for large messages
    options.EnableCompression = true;
    options.CompressionAlgorithm = CompressionAlgorithm.Brotli;
});
```

**Monitor batch metrics:**

```csharp
var metrics = _batchProcessor.GetMetrics();
_logger.LogInformation(
    "Batch stats: Size={AvgSize} Time={AvgTime}ms Compression={CompressionRatio:P}",
    metrics.AverageBatchSize,
    metrics.AverageProcessingTime.TotalMilliseconds,
    metrics.CompressionRatio);
```

### 3. Deduplication

**Configure deduplication window appropriately:**

```csharp
builder.Services.AddDeduplication(options =>
{
    // Set window based on expected duplicate timeframe
    options.Window = TimeSpan.FromMinutes(5); // For immediate duplicates
    // options.Window = TimeSpan.FromHours(1); // For delayed duplicates
    
    // Set cache size based on message volume
    options.MaxCacheSize = 100000; // ~100K messages/5min = 333 msg/sec
});
```

**Monitor deduplication effectiveness:**

```csharp
var metrics = _deduplicationCache.GetMetrics();
var duplicateRate = metrics.DuplicatesDetected / (double)metrics.TotalMessages;
if (duplicateRate > 0.1) // > 10% duplicates
{
    _logger.LogWarning("High duplicate rate: {Rate:P}", duplicateRate);
}
```

### 4. Prefetch Count

**Optimize prefetch count:**

```csharp
// Fast processing (< 100ms per message)
options.PrefetchCount = 100;

// Slow processing (> 1s per message)
options.PrefetchCount = 10;

// Very slow processing (> 10s per message)
options.PrefetchCount = 1;
```

## Security

### 1. Message Encryption

**Always encrypt sensitive data:**

```csharp
builder.Services.AddMessageEncryption(options =>
{
    options.EnableEncryption = true;
    options.EncryptionAlgorithm = "AES256";
    
    // Use Azure Key Vault in production
    options.KeyProvider = KeyProviderType.AzureKeyVault;
    options.KeyVaultUrl = "https://myvault.vault.azure.net/";
    options.KeyName = "message-encryption-key";
    
    // Enable key rotation
    options.EnableKeyRotation = true;
    options.KeyRotationGracePeriod = TimeSpan.FromHours(24);
});
```

**Don't encrypt everything:**

```csharp
// ✅ Good: Encrypt sensitive data only
public class OrderCreatedEvent
{
    public int OrderId { get; set; } // Not encrypted
    
    [Encrypted]
    public string CustomerEmail { get; set; } // Encrypted
    
    [Encrypted]
    public string CreditCardNumber { get; set; } // Encrypted
}

// ❌ Bad: Encrypting everything adds overhead
[Encrypted]
public class OrderCreatedEvent { ... }
```

### 2. Authentication and Authorization

**Implement authentication for production:**

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
        ["service"] = new[] { "publish:orders.*", "subscribe:orders.*" }
    };
});
```

**Use service accounts:**

```csharp
// ✅ Good: Service account with limited permissions
var token = await _identityProvider.GetServiceAccountTokenAsync("order-service");

// ❌ Bad: User account or shared credentials
var token = "hardcoded-token";
```

### 3. Rate Limiting

**Implement rate limiting to prevent abuse:**

```csharp
builder.Services.AddRateLimiting(options =>
{
    options.Enabled = true;
    options.Strategy = RateLimitStrategy.TokenBucket;
    
    // Global limit
    options.RequestsPerSecond = 10000;
    options.BurstSize = 1000;
    
    // Per-tenant limits
    options.EnablePerTenantLimits = true;
    options.TenantLimits = new Dictionary<string, int>
    {
        ["premium"] = 10000,
        ["standard"] = 1000,
        ["free"] = 100
    };
});
```

### 4. Sensitive Data

**Never log sensitive data:**

```csharp
// ✅ Good: Mask sensitive data
_logger.LogInformation(
    "Processing payment for order {OrderId} card ending in {CardLast4}",
    orderId,
    creditCard.Substring(creditCard.Length - 4));

// ❌ Bad: Logging sensitive data
_logger.LogInformation(
    "Processing payment with card {CreditCard}",
    creditCard); // PCI-DSS violation!
```

**Exclude sensitive headers from tracing:**

```csharp
builder.Services.AddDistributedTracing(options =>
{
    options.CaptureMessageHeaders = true;
    options.ExcludedHeaderKeys = new List<string>
    {
        "Authorization",
        "X-API-Key",
        "Password",
        "CreditCard",
        "SSN"
    };
});
```

## Observability

### 1. Metrics

**Collect comprehensive metrics:**

```csharp
builder.Services.AddMessageBrokerMetrics(options =>
{
    options.EnableMetrics = true;
    options.EnablePrometheusExporter = true;
    options.Labels = new Dictionary<string, string>
    {
        ["environment"] = builder.Environment.EnvironmentName,
        ["service"] = "order-service",
        ["version"] = Assembly.GetExecutingAssembly().GetName().Version.ToString()
    };
});
```

**Monitor key metrics:**

- Message throughput (messages/second)
- Processing latency (p50, p95, p99)
- Error rate
- Queue depth
- Connection pool utilization
- Circuit breaker state
- Duplicate message rate

### 2. Distributed Tracing

**Use appropriate sampling:**

```csharp
builder.Services.AddDistributedTracing(options =>
{
    // Development: 100% sampling
    options.SamplingRate = builder.Environment.IsDevelopment() ? 1.0 : 0.1;
    
    // Production: 10% sampling
    // Increase for debugging: 1.0 (100%)
});
```

**Add custom span attributes:**

```csharp
using var activity = Activity.Current;
activity?.SetTag("order.id", orderId);
activity?.SetTag("order.total", totalAmount);
activity?.SetTag("customer.tier", customerTier);
activity?.SetTag("processing.duration_ms", processingTime.TotalMilliseconds);
```

### 3. Logging

**Use appropriate log levels:**

```csharp
// Debug: Detailed information for debugging
_logger.LogDebug("Processing message {MessageId} with payload {Payload}", messageId, payload);

// Information: General informational messages
_logger.LogInformation("Order {OrderId} created successfully", orderId);

// Warning: Potentially harmful situations
_logger.LogWarning("Retry attempt {Attempt} for message {MessageId}", attempt, messageId);

// Error: Error events that might still allow the application to continue
_logger.LogError(ex, "Failed to process order {OrderId}", orderId);

// Critical: Very severe error events that might cause the application to abort
_logger.LogCritical(ex, "Database connection lost");
```

## Resilience

### 1. Circuit Breaker

**Configure circuit breaker appropriately:**

```csharp
builder.Services.AddCircuitBreaker(options =>
{
    options.FailureThreshold = 5; // Open after 5 failures
    options.SuccessThreshold = 2; // Close after 2 successes
    options.Timeout = TimeSpan.FromSeconds(60); // Try again after 60s
    options.FailureRateThreshold = 0.5; // Open if > 50% failures
});
```

**Monitor circuit breaker state:**

```csharp
_circuitBreaker.OnStateChanged += (sender, args) =>
{
    _logger.LogWarning(
        "Circuit breaker state changed: {PreviousState} -> {NewState}. Reason: {Reason}",
        args.PreviousState,
        args.NewState,
        args.Reason);
    
    // Send alert if circuit opens
    if (args.NewState == CircuitBreakerState.Open)
    {
        await _alertService.SendAlertAsync("Circuit breaker opened!");
    }
};
```

### 2. Bulkhead Pattern

**Isolate critical operations:**

```csharp
builder.Services.AddBulkhead(options =>
{
    // Limit concurrent operations to prevent resource exhaustion
    options.MaxConcurrentOperations = 100;
    options.MaxQueuedOperations = 1000;
});
```

**Handle bulkhead rejections:**

```csharp
try
{
    await _messageBroker.PublishAsync(message);
}
catch (BulkheadRejectedException ex)
{
    _logger.LogWarning("Bulkhead full, operation rejected");
    
    // Option 1: Retry after delay
    await Task.Delay(TimeSpan.FromSeconds(1));
    await _messageBroker.PublishAsync(message);
    
    // Option 2: Store in outbox for later processing
    await _outboxStore.StoreAsync(message);
}
```

### 3. Poison Message Handling

**Configure poison message handling:**

```csharp
builder.Services.AddPoisonMessageHandling(options =>
{
    options.Enabled = true;
    options.FailureThreshold = 5; // Move to poison queue after 5 failures
    options.RetentionPeriod = TimeSpan.FromDays(7);
});
```

**Monitor and analyze poison messages:**

```csharp
// Scheduled job to review poison messages
public class PoisonMessageReviewJob : IHostedService
{
    public async Task ExecuteAsync()
    {
        var poisonMessages = await _poisonMessageHandler.GetPoisonMessagesAsync();
        
        foreach (var message in poisonMessages)
        {
            _logger.LogWarning(
                "Poison message: {MessageId} Type={MessageType} Failures={FailureCount}",
                message.Id,
                message.MessageType,
                message.FailureCount);
            
            // Analyze errors
            foreach (var error in message.Errors)
            {
                _logger.LogError("Error: {Error}", error);
            }
        }
        
        // Alert if too many poison messages
        if (poisonMessages.Count() > 100)
        {
            await _alertService.SendAlertAsync($"High poison message count: {poisonMessages.Count()}");
        }
    }
}
```

### 4. Backpressure Management

**Enable backpressure for variable loads:**

```csharp
builder.Services.AddBackpressure(options =>
{
    options.Enabled = true;
    options.LatencyThreshold = TimeSpan.FromSeconds(5);
    options.QueueDepthThreshold = 10000;
    options.RecoveryLatencyThreshold = TimeSpan.FromSeconds(2);
    options.ThrottlePercentage = 0.5; // Reduce by 50%
});
```

**Monitor backpressure events:**

```csharp
_backpressureController.OnBackpressureDetected += (sender, args) =>
{
    _logger.LogWarning(
        "Backpressure detected: Latency={Latency}ms QueueDepth={QueueDepth}",
        args.AverageLatency.TotalMilliseconds,
        args.QueueDepth);
};

_backpressureController.OnBackpressureRecovered += (sender, args) =>
{
    _logger.LogInformation("Backpressure recovered");
};
```

## Deployment

### 1. Blue-Green Deployment

**Support zero-downtime deployments:**

```csharp
// Use consumer groups for parallel processing
builder.Services.AddRabbitMQ(options =>
{
    options.ConsumerTag = $"order-service-{Environment.MachineName}";
});

// Graceful shutdown
public async Task StopAsync(CancellationToken cancellationToken)
{
    // Stop accepting new messages
    await _messageBroker.StopAsync(cancellationToken);
    
    // Wait for in-flight messages to complete
    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
}
```

### 2. Rolling Updates

**Handle version compatibility:**

```csharp
// Add version to message headers
await _messageBroker.PublishAsync(
    message,
    new PublishOptions
    {
        Headers = new Dictionary<string, object>
        {
            ["MessageVersion"] = "2.0",
            ["SchemaVersion"] = "1.0"
        }
    });

// Handle multiple versions in consumer
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
    async (message, context, ct) =>
    {
        var version = context.Headers?["MessageVersion"]?.ToString() ?? "1.0";
        
        if (version == "1.0")
        {
            await ProcessV1Async(message);
        }
        else if (version == "2.0")
        {
            await ProcessV2Async(message);
        }
    });
```

### 3. Feature Flags

**Use feature flags for gradual rollout:**

```csharp
builder.Services.AddMessageBrokerEnhancements(options =>
{
    // Enable features gradually
    options.Batch.Enabled = _featureFlags.IsEnabled("BatchProcessing");
    options.Deduplication.Enabled = _featureFlags.IsEnabled("Deduplication");
    options.Encryption.EnableEncryption = _featureFlags.IsEnabled("Encryption");
});
```

## Monitoring and Alerting

### 1. Key Metrics to Monitor

**Set up alerts for:**

- High error rate (> 1%)
- High latency (p95 > 1s)
- Circuit breaker open
- High queue depth (> 10000)
- Low connection pool availability (< 10%)
- High poison message count (> 100)
- Backpressure active

### 2. Prometheus Alerts

```yaml
groups:
  - name: messagebroker
    rules:
      - alert: HighErrorRate
        expr: rate(relay_messages_failed_total[5m]) > 0.01
        for: 5m
        annotations:
          summary: "High message error rate"
          
      - alert: CircuitBreakerOpen
        expr: relay_circuit_breaker_state == 2
        for: 1m
        annotations:
          summary: "Circuit breaker is open"
          
      - alert: HighQueueDepth
        expr: relay_queue_depth > 10000
        for: 5m
        annotations:
          summary: "High queue depth"
```

### 3. Grafana Dashboards

Create dashboards for:

- Message throughput over time
- Latency percentiles (p50, p95, p99)
- Error rate
- Circuit breaker state
- Connection pool metrics
- Batch processing statistics
- Deduplication effectiveness

## Capacity Planning

### 1. Calculate Required Capacity

```
Messages per second = Peak load * Safety factor
Connection pool size = (Messages per second / Messages per connection) * Safety factor
Batch size = Messages per second * Flush interval
```

Example:
```
Peak load: 1000 msg/s
Safety factor: 2x
Messages per connection: 100 msg/s

Connection pool size = (1000 / 100) * 2 = 20 connections
Batch size = 1000 * 0.1s = 100 messages
```

### 2. Load Testing

**Perform load testing before production:**

```csharp
// Use K6 or similar tool
import http from 'k6/http';
import { check } from 'k6';

export let options = {
  stages: [
    { duration: '2m', target: 100 },  // Ramp up
    { duration: '5m', target: 100 },  // Stay at 100 RPS
    { duration: '2m', target: 200 },  // Ramp up
    { duration: '5m', target: 200 },  // Stay at 200 RPS
    { duration: '2m', target: 0 },    // Ramp down
  ],
};

export default function () {
  let response = http.post('http://api/orders', JSON.stringify({
    orderId: Math.floor(Math.random() * 1000000),
    amount: 99.99
  }));
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'latency < 500ms': (r) => r.timings.duration < 500,
  });
}
```

## Disaster Recovery

### 1. Backup Strategy

**Backup critical data:**

- Outbox messages (for replay)
- Inbox message IDs (for deduplication)
- Poison messages (for analysis)
- Configuration (for recovery)

### 2. Recovery Procedures

**Document recovery procedures:**

1. Restore database from backup
2. Replay outbox messages
3. Verify inbox deduplication
4. Monitor for duplicates
5. Validate message processing

### 3. Testing Recovery

**Regularly test disaster recovery:**

```csharp
// Chaos engineering test
public class DisasterRecoveryTest
{
    [Fact]
    public async Task TestDatabaseFailover()
    {
        // Simulate database failure
        await _database.SimulateFailureAsync();
        
        // Verify outbox continues to queue messages
        await _messageBroker.PublishAsync(message);
        
        // Restore database
        await _database.RestoreAsync();
        
        // Verify messages are processed
        await Task.Delay(TimeSpan.FromSeconds(10));
        Assert.True(await _outboxStore.GetPendingAsync().CountAsync() == 0);
    }
}
```

## Summary

Following these best practices will help you:

- ✅ Ensure reliable message delivery
- ✅ Optimize performance and throughput
- ✅ Secure sensitive data
- ✅ Monitor and troubleshoot effectively
- ✅ Build resilient systems
- ✅ Deploy with confidence
- ✅ Plan for disasters

## Next Steps

- [Troubleshooting Guide](./TROUBLESHOOTING.md)
- [Migration Guide](./MIGRATION.md)
- [Performance Tuning](./PERFORMANCE_TUNING.md)
