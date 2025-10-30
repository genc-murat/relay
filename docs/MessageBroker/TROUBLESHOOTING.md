# Troubleshooting Guide

This guide helps you diagnose and resolve common issues with Relay.MessageBroker enhancements.

## Table of Contents

- [General Issues](#general-issues)
- [Outbox Pattern Issues](#outbox-pattern-issues)
- [Inbox Pattern Issues](#inbox-pattern-issues)
- [Connection Pool Issues](#connection-pool-issues)
- [Batch Processing Issues](#batch-processing-issues)
- [Deduplication Issues](#deduplication-issues)
- [Health Check Issues](#health-check-issues)
- [Metrics and Tracing Issues](#metrics-and-tracing-issues)
- [Security Issues](#security-issues)
- [Rate Limiting Issues](#rate-limiting-issues)
- [Resilience Issues](#resilience-issues)
- [Performance Issues](#performance-issues)
- [Diagnostic Tools](#diagnostic-tools)

## General Issues

### Messages Not Being Published

**Symptoms:**
- Messages are not appearing in the broker
- No errors in logs

**Possible Causes:**

1. **Message broker not started**
   ```csharp
   // Check if broker is started
   var broker = serviceProvider.GetRequiredService<IMessageBroker>();
   if (!broker.IsConnected)
   {
       await broker.StartAsync();
   }
   ```

2. **Incorrect configuration**
   ```csharp
   // Verify configuration
   var options = serviceProvider.GetRequiredService<IOptions<MessageBrokerOptions>>();
   _logger.LogInformation("Broker config: {Config}", JsonSerializer.Serialize(options.Value));
   ```

3. **Circuit breaker open**
   ```csharp
   // Check circuit breaker state
   var circuitBreaker = serviceProvider.GetRequiredService<ICircuitBreaker>();
   if (circuitBreaker.State == CircuitBreakerState.Open)
   {
       _logger.LogWarning("Circuit breaker is open");
       circuitBreaker.Reset(); // Manual reset if needed
   }
   ```

### Messages Not Being Consumed

**Symptoms:**
- Messages are in the queue but not being processed
- Consumer appears to be running

**Possible Causes:**

1. **Subscription not registered**
   ```csharp
   // Verify subscription is registered
   await _messageBroker.SubscribeAsync<OrderCreatedEvent>(handler);
   ```

2. **Prefetch count too low**
   ```csharp
   // Increase prefetch count
   options.PrefetchCount = 100; // Was: 1
   ```

3. **Consumer is blocked**
   ```csharp
   // Check for blocking operations
   await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
       async (message, context, ct) =>
       {
           // ❌ Bad: Blocking call
           Thread.Sleep(1000);
           
           // ✅ Good: Async call
           await Task.Delay(1000, ct);
       });
   ```

## Outbox Pattern Issues

### Outbox Messages Not Being Processed

**Symptoms:**
- Messages stuck in outbox table
- OutboxWorker not processing messages

**Diagnosis:**

```csharp
// Check outbox status
var pendingCount = await _outboxStore.GetPendingAsync(1000).CountAsync();
_logger.LogInformation("Pending outbox messages: {Count}", pendingCount);

// Check worker status
var worker = serviceProvider.GetRequiredService<OutboxWorker>();
_logger.LogInformation("Worker running: {IsRunning}", worker.IsRunning);
```

**Solutions:**

1. **Worker not started**
   ```csharp
   // Ensure hosted service is registered
   builder.Services.AddHostedService<OutboxWorker>();
   ```

2. **Polling interval too long**
   ```csharp
   builder.Services.AddOutboxPattern(options =>
   {
       options.PollingInterval = TimeSpan.FromSeconds(1); // Was: 60
   });
   ```

3. **Database connection issues**
   ```csharp
   // Test database connection
   try
   {
       await _dbContext.Database.CanConnectAsync();
   }
   catch (Exception ex)
   {
       _logger.LogError(ex, "Cannot connect to database");
   }
   ```

### Outbox Messages Failing Repeatedly

**Symptoms:**
- Messages moving to failed status
- Same error repeating

**Diagnosis:**

```csharp
// Check failed messages
var failedMessages = await _outboxStore.GetFailedAsync(100);
foreach (var message in failedMessages)
{
    _logger.LogError(
        "Failed message: {MessageId} Type={MessageType} Error={Error} Retries={RetryCount}",
        message.Id,
        message.MessageType,
        message.LastError,
        message.RetryCount);
}
```

**Solutions:**

1. **Increase retry attempts**
   ```csharp
   builder.Services.AddOutboxPattern(options =>
   {
       options.MaxRetryAttempts = 10; // Was: 3
       options.UseExponentialBackoff = true;
   });
   ```

2. **Fix underlying issue**
   - Check broker connectivity
   - Verify message format
   - Check permissions

3. **Manual reprocessing**
   ```csharp
   // Reset failed messages for reprocessing
   foreach (var message in failedMessages)
   {
       message.Status = OutboxMessageStatus.Pending;
       message.RetryCount = 0;
       await _outboxStore.UpdateAsync(message);
   }
   ```

## Inbox Pattern Issues

### Duplicate Messages Being Processed

**Symptoms:**
- Same message processed multiple times
- Duplicate database entries

**Diagnosis:**

```csharp
// Check if inbox is enabled
var options = serviceProvider.GetRequiredService<IOptions<InboxOptions>>();
_logger.LogInformation("Inbox enabled: {Enabled}", options.Value.Enabled);

// Check inbox entries
var exists = await _inboxStore.ExistsAsync(messageId);
_logger.LogInformation("Message {MessageId} in inbox: {Exists}", messageId, exists);
```

**Solutions:**

1. **Inbox not enabled**
   ```csharp
   builder.Services.AddInboxPattern(options =>
   {
       options.Enabled = true; // Was: false
   });
   ```

2. **Message ID not unique**
   ```csharp
   // Ensure unique message IDs
   await _messageBroker.PublishAsync(
       message,
       new PublishOptions
       {
           MessageId = Guid.NewGuid().ToString() // Unique ID
       });
   ```

3. **Retention period too short**
   ```csharp
   builder.Services.AddInboxPattern(options =>
   {
       options.RetentionPeriod = TimeSpan.FromDays(7); // Was: 1 day
   });
   ```

### Inbox Table Growing Too Large

**Symptoms:**
- Database size increasing rapidly
- Slow inbox queries

**Diagnosis:**

```csharp
// Check inbox size
var count = await _dbContext.InboxMessages.CountAsync();
_logger.LogWarning("Inbox message count: {Count}", count);
```

**Solutions:**

1. **Cleanup not running**
   ```csharp
   // Ensure cleanup worker is registered
   builder.Services.AddHostedService<InboxCleanupWorker>();
   ```

2. **Cleanup interval too long**
   ```csharp
   builder.Services.AddInboxPattern(options =>
   {
       options.CleanupInterval = TimeSpan.FromHours(1); // Was: 24 hours
   });
   ```

3. **Manual cleanup**
   ```csharp
   // Force cleanup
   await _inboxStore.CleanupExpiredAsync(
       TimeSpan.FromDays(7),
       CancellationToken.None);
   ```

## Connection Pool Issues

### Connection Pool Exhaustion

**Symptoms:**
- Timeouts acquiring connections
- "Connection pool exhausted" errors

**Diagnosis:**

```csharp
// Check pool metrics
var metrics = _connectionPool.GetMetrics();
_logger.LogWarning(
    "Pool: Active={Active} Idle={Idle} WaitTime={WaitTime}ms",
    metrics.ActiveConnections,
    metrics.IdleConnections,
    metrics.AverageWaitTime.TotalMilliseconds);
```

**Solutions:**

1. **Increase pool size**
   ```csharp
   builder.Services.AddConnectionPooling<IConnection>(options =>
   {
       options.MaxPoolSize = 100; // Was: 50
   });
   ```

2. **Reduce connection timeout**
   ```csharp
   builder.Services.AddConnectionPooling<IConnection>(options =>
   {
       options.ConnectionTimeout = TimeSpan.FromSeconds(10); // Was: 5
   });
   ```

3. **Fix connection leaks**
   ```csharp
   // ❌ Bad: Not disposing connection
   var connection = await _connectionPool.AcquireAsync();
   // ... use connection ...
   // Missing: await connection.DisposeAsync();
   
   // ✅ Good: Using statement
   await using var connection = await _connectionPool.AcquireAsync();
   // ... use connection ...
   // Automatically disposed
   ```

### Stale Connections

**Symptoms:**
- Connection errors after idle period
- "Connection closed" exceptions

**Solutions:**

1. **Enable connection validation**
   ```csharp
   builder.Services.AddConnectionPooling<IConnection>(options =>
   {
       options.EnableValidation = true;
       options.ValidationInterval = TimeSpan.FromSeconds(30);
   });
   ```

2. **Reduce idle timeout**
   ```csharp
   builder.Services.AddConnectionPooling<IConnection>(options =>
   {
       options.IdleTimeout = TimeSpan.FromMinutes(5); // Was: 30
   });
   ```

## Batch Processing Issues

### Batches Not Being Flushed

**Symptoms:**
- Messages delayed
- Batches not reaching broker

**Diagnosis:**

```csharp
// Check batch processor metrics
var metrics = _batchProcessor.GetMetrics();
_logger.LogInformation(
    "Batch: Size={Size} Pending={Pending} LastFlush={LastFlush}",
    metrics.AverageBatchSize,
    metrics.PendingMessages,
    metrics.LastFlushTime);
```

**Solutions:**

1. **Flush interval too long**
   ```csharp
   builder.Services.AddBatchProcessing(options =>
   {
       options.FlushInterval = TimeSpan.FromMilliseconds(100); // Was: 1000
   });
   ```

2. **Batch size not reached**
   ```csharp
   builder.Services.AddBatchProcessing(options =>
   {
       options.MaxBatchSize = 10; // Was: 1000 (too high for low volume)
   });
   ```

3. **Manual flush**
   ```csharp
   // Force flush on shutdown
   public async Task StopAsync(CancellationToken cancellationToken)
   {
       await _batchProcessor.FlushAsync(cancellationToken);
   }
   ```

### Compression Not Working

**Symptoms:**
- No compression ratio improvement
- Large message sizes

**Diagnosis:**

```csharp
// Check compression statistics
var stats = _batchProcessor.GetMetrics();
_logger.LogInformation(
    "Compression: Ratio={Ratio:P} Enabled={Enabled}",
    stats.CompressionRatio,
    _options.EnableCompression);
```

**Solutions:**

1. **Compression not enabled**
   ```csharp
   builder.Services.AddBatchProcessing(options =>
   {
       options.EnableCompression = true; // Was: false
   });
   ```

2. **Messages too small**
   ```csharp
   // Compression only helps for larger messages
   // Check message sizes
   if (messageSize < 1024) // < 1KB
   {
       _logger.LogInformation("Message too small for compression");
   }
   ```

3. **Wrong compression algorithm**
   ```csharp
   builder.Services.AddBatchProcessing(options =>
   {
       options.CompressionAlgorithm = CompressionAlgorithm.Brotli; // Best ratio
       options.CompressionLevel = 9; // Maximum compression
   });
   ```

## Deduplication Issues

### Duplicates Not Being Detected

**Symptoms:**
- Duplicate messages getting through
- Deduplication not working

**Diagnosis:**

```csharp
// Check deduplication metrics
var metrics = _deduplicationCache.GetMetrics();
_logger.LogInformation(
    "Dedup: Total={Total} Duplicates={Duplicates} HitRate={HitRate:P}",
    metrics.TotalMessages,
    metrics.DuplicatesDetected,
    metrics.HitRate);
```

**Solutions:**

1. **Deduplication not enabled**
   ```csharp
   builder.Services.AddDeduplication(options =>
   {
       options.Enabled = true; // Was: false
   });
   ```

2. **Window too short**
   ```csharp
   builder.Services.AddDeduplication(options =>
   {
       options.Window = TimeSpan.FromMinutes(10); // Was: 1 minute
   });
   ```

3. **Wrong strategy**
   ```csharp
   builder.Services.AddDeduplication(options =>
   {
       // Use ContentHash for content-based deduplication
       options.Strategy = DeduplicationStrategy.ContentHash; // Was: MessageId
   });
   ```

### Cache Growing Too Large

**Symptoms:**
- High memory usage
- Slow deduplication checks

**Solutions:**

1. **Reduce cache size**
   ```csharp
   builder.Services.AddDeduplication(options =>
   {
       options.MaxCacheSize = 50000; // Was: 100000
   });
   ```

2. **Reduce window**
   ```csharp
   builder.Services.AddDeduplication(options =>
   {
       options.Window = TimeSpan.FromMinutes(5); // Was: 60
   });
   ```

## Health Check Issues

### Health Checks Always Unhealthy

**Symptoms:**
- /health endpoint returns Unhealthy
- Kubernetes/Docker restarts container

**Diagnosis:**

```csharp
// Check health check details
var healthReport = await _healthCheckService.CheckHealthAsync();
foreach (var entry in healthReport.Entries)
{
    _logger.LogInformation(
        "Health check: {Name} Status={Status} Description={Description}",
        entry.Key,
        entry.Value.Status,
        entry.Value.Description);
}
```

**Solutions:**

1. **Timeout too short**
   ```csharp
   builder.Services.AddMessageBrokerHealthChecks(options =>
   {
       options.Timeout = TimeSpan.FromSeconds(5); // Was: 2
   });
   ```

2. **Broker not connected**
   ```csharp
   // Ensure broker is started before health checks
   await _messageBroker.StartAsync();
   ```

3. **Circuit breaker open**
   ```csharp
   // Reset circuit breaker if needed
   _circuitBreaker.Reset();
   ```

## Metrics and Tracing Issues

### Metrics Not Appearing in Prometheus

**Symptoms:**
- /metrics endpoint returns no data
- Prometheus not scraping metrics

**Diagnosis:**

```bash
# Test metrics endpoint
curl http://localhost:5000/metrics

# Check Prometheus targets
# Prometheus UI -> Status -> Targets
```

**Solutions:**

1. **Prometheus exporter not enabled**
   ```csharp
   builder.Services.AddMessageBrokerMetrics(options =>
   {
       options.EnablePrometheusExporter = true; // Was: false
   });
   
   app.MapPrometheusScrapingEndpoint("/metrics");
   ```

2. **Wrong endpoint**
   ```csharp
   // Verify endpoint configuration
   app.MapPrometheusScrapingEndpoint("/metrics"); // Not /prometheus
   ```

3. **Firewall blocking**
   ```bash
   # Check if port is accessible
   telnet localhost 5000
   ```

### Traces Not Appearing in Jaeger

**Symptoms:**
- No traces in Jaeger UI
- Tracing not working

**Diagnosis:**

```csharp
// Check if tracing is enabled
var options = serviceProvider.GetRequiredService<IOptions<DistributedTracingOptions>>();
_logger.LogInformation("Tracing enabled: {Enabled}", options.Value.EnableTracing);

// Check sampling rate
_logger.LogInformation("Sampling rate: {Rate}", options.Value.SamplingRate);
```

**Solutions:**

1. **Tracing not enabled**
   ```csharp
   builder.Services.AddDistributedTracing(options =>
   {
       options.EnableTracing = true; // Was: false
   });
   ```

2. **Sampling rate too low**
   ```csharp
   builder.Services.AddDistributedTracing(options =>
   {
       options.SamplingRate = 1.0; // 100% for debugging (Was: 0.01)
   });
   ```

3. **Wrong exporter configuration**
   ```csharp
   builder.Services.AddDistributedTracing(options =>
   {
       options.Exporters = new[] { TracingExporter.Jaeger };
       options.JaegerAgentHost = "localhost"; // Was: wrong host
       options.JaegerAgentPort = 6831;
   });
   ```

## Security Issues

### Encryption Errors

**Symptoms:**
- "Decryption failed" errors
- Messages not being decrypted

**Diagnosis:**

```csharp
// Check encryption configuration
var options = serviceProvider.GetRequiredService<IOptions<SecurityOptions>>();
_logger.LogInformation(
    "Encryption: Enabled={Enabled} Algorithm={Algorithm} Provider={Provider}",
    options.Value.EnableEncryption,
    options.Value.EncryptionAlgorithm,
    options.Value.KeyProvider);
```

**Solutions:**

1. **Wrong encryption key**
   ```csharp
   // Verify key is correct
   var key = Environment.GetEnvironmentVariable("MESSAGE_ENCRYPTION_KEY");
   if (string.IsNullOrEmpty(key))
   {
       _logger.LogError("Encryption key not found");
   }
   ```

2. **Key rotation issue**
   ```csharp
   // Check key version
   var keyVersion = _encryptor.GetKeyVersion();
   _logger.LogInformation("Current key version: {Version}", keyVersion);
   
   // Extend grace period if needed
   builder.Services.AddMessageEncryption(options =>
   {
       options.KeyRotationGracePeriod = TimeSpan.FromHours(48); // Was: 24
   });
   ```

### Authentication Failures

**Symptoms:**
- "Unauthorized" errors
- Authentication token rejected

**Diagnosis:**

```csharp
// Check token
try
{
    var isValid = await _authenticator.ValidateTokenAsync(token);
    _logger.LogInformation("Token valid: {IsValid}", isValid);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Token validation failed");
}
```

**Solutions:**

1. **Token expired**
   ```csharp
   // Check token expiration
   var handler = new JwtSecurityTokenHandler();
   var jwtToken = handler.ReadJwtToken(token);
   _logger.LogInformation("Token expires: {Expiry}", jwtToken.ValidTo);
   ```

2. **Wrong issuer/audience**
   ```csharp
   builder.Services.AddMessageBrokerSecurity(options =>
   {
       options.JwtIssuer = "https://auth.example.com"; // Verify correct
       options.JwtAudience = "message-broker"; // Verify correct
   });
   ```

## Rate Limiting Issues

### Rate Limits Too Restrictive

**Symptoms:**
- Many "Rate limit exceeded" errors
- Legitimate requests being rejected

**Solutions:**

1. **Increase rate limit**
   ```csharp
   builder.Services.AddRateLimiting(options =>
   {
       options.RequestsPerSecond = 10000; // Was: 1000
       options.BurstSize = 1000; // Allow bursts
   });
   ```

2. **Adjust per-tenant limits**
   ```csharp
   builder.Services.AddRateLimiting(options =>
   {
       options.TenantLimits["premium"] = 20000; // Was: 10000
   });
   ```

### Rate Limiting Not Working

**Symptoms:**
- No rate limiting applied
- Requests not being throttled

**Solutions:**

1. **Rate limiting not enabled**
   ```csharp
   builder.Services.AddRateLimiting(options =>
   {
       options.Enabled = true; // Was: false
   });
   ```

2. **Tenant ID not extracted**
   ```csharp
   // Verify tenant ID extraction
   var tenantId = _tenantIdExtractor.Extract(context);
   _logger.LogInformation("Tenant ID: {TenantId}", tenantId);
   ```

## Resilience Issues

### Circuit Breaker Stuck Open

**Symptoms:**
- Circuit breaker remains open
- All requests being rejected

**Solutions:**

1. **Manual reset**
   ```csharp
   _circuitBreaker.Reset();
   ```

2. **Adjust timeout**
   ```csharp
   builder.Services.AddCircuitBreaker(options =>
   {
       options.Timeout = TimeSpan.FromSeconds(30); // Was: 60
   });
   ```

### Poison Messages Accumulating

**Symptoms:**
- Many messages in poison queue
- Same messages failing repeatedly

**Diagnosis:**

```csharp
// Analyze poison messages
var poisonMessages = await _poisonMessageHandler.GetPoisonMessagesAsync();
var errorGroups = poisonMessages
    .GroupBy(m => m.Errors.FirstOrDefault())
    .OrderByDescending(g => g.Count());

foreach (var group in errorGroups)
{
    _logger.LogWarning(
        "Common error: {Error} Count={Count}",
        group.Key,
        group.Count());
}
```

**Solutions:**

1. **Fix underlying issue**
   - Check error messages
   - Fix code bugs
   - Update message schema

2. **Increase failure threshold**
   ```csharp
   builder.Services.AddPoisonMessageHandling(options =>
   {
       options.FailureThreshold = 10; // Was: 5
   });
   ```

3. **Manual reprocessing**
   ```csharp
   // Reprocess after fixing issue
   foreach (var message in poisonMessages)
   {
       await _poisonMessageHandler.ReprocessAsync(message.Id);
   }
   ```

## Performance Issues

### High Latency

**Symptoms:**
- Slow message processing
- High p95/p99 latencies

**Diagnosis:**

```csharp
// Check metrics
var metrics = _messageBrokerMetrics.GetMetrics();
_logger.LogWarning(
    "Latency: P50={P50}ms P95={P95}ms P99={P99}ms",
    metrics.P50Latency.TotalMilliseconds,
    metrics.P95Latency.TotalMilliseconds,
    metrics.P99Latency.TotalMilliseconds);
```

**Solutions:**

1. **Enable connection pooling**
   ```csharp
   builder.Services.AddConnectionPooling<IConnection>(options =>
   {
       options.MinPoolSize = 20;
       options.MaxPoolSize = 100;
   });
   ```

2. **Enable batch processing**
   ```csharp
   builder.Services.AddBatchProcessing(options =>
   {
       options.Enabled = true;
       options.MaxBatchSize = 100;
   });
   ```

3. **Optimize prefetch count**
   ```csharp
   options.PrefetchCount = 100; // Was: 1
   ```

### High Memory Usage

**Symptoms:**
- Memory usage increasing
- Out of memory errors

**Diagnosis:**

```csharp
// Check cache sizes
var dedupMetrics = _deduplicationCache.GetMetrics();
_logger.LogWarning("Dedup cache size: {Size}", dedupMetrics.CacheSize);

var poolMetrics = _connectionPool.GetMetrics();
_logger.LogWarning("Connection pool size: {Size}", poolMetrics.TotalConnections);
```

**Solutions:**

1. **Reduce cache sizes**
   ```csharp
   builder.Services.AddDeduplication(options =>
   {
       options.MaxCacheSize = 50000; // Was: 100000
   });
   ```

2. **Reduce connection pool**
   ```csharp
   builder.Services.AddConnectionPooling<IConnection>(options =>
   {
       options.MaxPoolSize = 50; // Was: 200
   });
   ```

3. **Enable cleanup**
   ```csharp
   // Ensure cleanup workers are running
   builder.Services.AddHostedService<InboxCleanupWorker>();
   ```

## Diagnostic Tools

### Enable Debug Logging

```csharp
builder.Logging.AddFilter("Relay.MessageBroker", LogLevel.Debug);
```

### Dump Configuration

```csharp
var options = serviceProvider.GetRequiredService<IOptions<MessageBrokerOptions>>();
_logger.LogInformation("Configuration: {Config}", 
    JsonSerializer.Serialize(options.Value, new JsonSerializerOptions { WriteIndented = true }));
```

### Check Component Status

```csharp
public class DiagnosticsController : ControllerBase
{
    [HttpGet("/diagnostics")]
    public async Task<IActionResult> GetDiagnostics()
    {
        return Ok(new
        {
            Broker = new
            {
                Connected = _messageBroker.IsConnected,
                Type = _messageBroker.GetType().Name
            },
            CircuitBreaker = new
            {
                State = _circuitBreaker.State,
                Metrics = _circuitBreaker.Metrics
            },
            ConnectionPool = _connectionPool.GetMetrics(),
            Outbox = new
            {
                PendingCount = await _outboxStore.GetPendingAsync(1).CountAsync()
            },
            Inbox = new
            {
                TotalCount = await _dbContext.InboxMessages.CountAsync()
            }
        });
    }
}
```

### Performance Profiling

```csharp
// Use BenchmarkDotNet for performance testing
[MemoryDiagnoser]
public class MessageBrokerBenchmarks
{
    [Benchmark]
    public async Task PublishMessage()
    {
        await _messageBroker.PublishAsync(new OrderCreatedEvent { OrderId = 123 });
    }
}
```

## Getting Help

If you're still experiencing issues:

1. Check the [GitHub Issues](https://github.com/your-org/relay/issues)
2. Review the [Documentation](https://docs.relay.dev)
3. Ask in [Discussions](https://github.com/your-org/relay/discussions)
4. Contact support

## Next Steps

- [Best Practices](./BEST_PRACTICES.md)
- [Configuration Guide](./CONFIGURATION.md)
- [Performance Tuning](./PERFORMANCE_TUNING.md)
