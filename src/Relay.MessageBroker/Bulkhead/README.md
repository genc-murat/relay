# Bulkhead Pattern

The Bulkhead pattern provides resource isolation and prevents cascading failures by limiting the number of concurrent operations. This implementation includes separate bulkheads for publish and subscribe operations.

## Features

- **Resource Isolation**: Separate bulkheads for publish and subscribe operations
- **Concurrency Control**: Configurable maximum concurrent operations
- **Operation Queuing**: Queue operations when all slots are busy
- **Metrics**: Track active, queued, rejected, and executed operations
- **Graceful Degradation**: Reject operations when resources are exhausted

## Configuration

```csharp
services.AddMessageBrokerBulkhead(options =>
{
    options.Enabled = true;
    options.MaxConcurrentOperations = 100;
    options.MaxQueuedOperations = 1000;
    options.AcquisitionTimeout = TimeSpan.FromSeconds(30);
});

// Decorate the message broker
services.DecorateMessageBrokerWithBulkhead();
```

## Options

| Option | Description | Default |
|--------|-------------|---------|
| `Enabled` | Enable/disable bulkhead pattern | `false` |
| `MaxConcurrentOperations` | Maximum concurrent operations allowed | `100` |
| `MaxQueuedOperations` | Maximum operations that can be queued | `1000` |
| `AcquisitionTimeout` | Timeout for acquiring a bulkhead slot | `30 seconds` |

## Usage

### Basic Usage

Once configured, the bulkhead pattern is automatically applied to all publish and subscribe operations:

```csharp
// Publish operations are automatically protected by the publish bulkhead
await messageBroker.PublishAsync(new MyMessage { Data = "test" });

// Subscribe handlers are automatically protected by the subscribe bulkhead
await messageBroker.SubscribeAsync<MyMessage>(async (message, context, ct) =>
{
    await ProcessMessageAsync(message);
});
```

### Handling Rejections

When the bulkhead is full, operations are rejected with a `BulkheadRejectedException`:

```csharp
try
{
    await messageBroker.PublishAsync(message);
}
catch (BulkheadRejectedException ex)
{
    // Log the rejection
    logger.LogWarning(
        "Operation rejected. Active: {Active}, Queued: {Queued}",
        ex.ActiveOperations,
        ex.QueuedOperations);
    
    // Implement retry logic or alternative handling
    await Task.Delay(TimeSpan.FromSeconds(1));
    // Retry or handle gracefully
}
```

### Monitoring Metrics

Access bulkhead metrics to monitor resource utilization:

```csharp
// Get the decorator instance
var bulkheadDecorator = serviceProvider.GetService<BulkheadMessageBrokerDecorator>();

// Get publish bulkhead metrics
var publishMetrics = bulkheadDecorator.GetPublishMetrics();
Console.WriteLine($"Active publish operations: {publishMetrics.ActiveOperations}");
Console.WriteLine($"Queued publish operations: {publishMetrics.QueuedOperations}");
Console.WriteLine($"Rejected operations: {publishMetrics.RejectedOperations}");

// Get subscribe bulkhead metrics
var subscribeMetrics = bulkheadDecorator.GetSubscribeMetrics();
Console.WriteLine($"Active subscribe operations: {subscribeMetrics.ActiveOperations}");
```

## How It Works

### Concurrency Control

The bulkhead uses a semaphore to limit concurrent operations:

1. When an operation arrives, it tries to acquire a semaphore slot
2. If a slot is available, the operation executes immediately
3. If all slots are busy, the operation is queued (up to `MaxQueuedOperations`)
4. If the queue is full, the operation is rejected with `BulkheadRejectedException`
5. When an operation completes, the next queued operation is processed

### Separate Bulkheads

The implementation provides separate bulkheads for publish and subscribe operations:

- **Publish Bulkhead**: Protects the message broker from being overwhelmed by publish requests
- **Subscribe Bulkhead**: Protects message handlers from being overwhelmed by incoming messages

This separation ensures that slow consumers don't affect publishers and vice versa.

### Queue Management

Operations are queued when all concurrent slots are busy:

```
┌─────────────────────────────────────┐
│  Bulkhead (Max Concurrent: 3)       │
├─────────────────────────────────────┤
│  [Active] [Active] [Active]         │  ← Executing operations
├─────────────────────────────────────┤
│  Queue (Max: 5)                     │
│  [Queued] [Queued] [Queued]         │  ← Waiting operations
├─────────────────────────────────────┤
│  New Operation → Rejected           │  ← Queue full
└─────────────────────────────────────┘
```

## Best Practices

### 1. Size Bulkheads Appropriately

Size bulkheads based on your system's capacity:

```csharp
// For high-throughput systems
options.MaxConcurrentOperations = 500;
options.MaxQueuedOperations = 5000;

// For resource-constrained systems
options.MaxConcurrentOperations = 50;
options.MaxQueuedOperations = 500;
```

### 2. Monitor Metrics

Regularly monitor bulkhead metrics to detect resource exhaustion:

```csharp
// Set up periodic monitoring
var timer = new Timer(_ =>
{
    var metrics = bulkheadDecorator.GetPublishMetrics();
    
    if (metrics.RejectedOperations > threshold)
    {
        logger.LogWarning("High rejection rate detected");
        // Alert or scale resources
    }
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
```

### 3. Implement Retry Logic

Implement exponential backoff for rejected operations:

```csharp
async Task PublishWithRetryAsync<TMessage>(TMessage message, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await messageBroker.PublishAsync(message);
            return;
        }
        catch (BulkheadRejectedException)
        {
            if (i == maxRetries - 1) throw;
            
            var delay = TimeSpan.FromMilliseconds(Math.Pow(2, i) * 100);
            await Task.Delay(delay);
        }
    }
}
```

### 4. Use Different Bulkhead Sizes

Configure different sizes for publish and subscribe based on workload:

```csharp
// If you have more publishers than consumers
services.AddMessageBrokerBulkhead(options =>
{
    options.MaxConcurrentOperations = 200; // Higher for publish
});

// Or use separate configurations if needed
```

### 5. Handle Rejections Gracefully

Don't let rejections crash your application:

```csharp
await messageBroker.SubscribeAsync<MyMessage>(async (message, context, ct) =>
{
    try
    {
        await ProcessMessageAsync(message);
    }
    catch (BulkheadRejectedException)
    {
        // Reject the message without requeue
        // It will be retried by the broker's retry mechanism
        await context.Reject(requeue: false);
    }
});
```

## Integration with Other Patterns

### With Circuit Breaker

Bulkhead works well with circuit breaker to provide comprehensive resilience:

```csharp
services.AddMessageBrokerCircuitBreaker(/* ... */);
services.AddMessageBrokerBulkhead(/* ... */);

services.DecorateMessageBrokerWithCircuitBreaker();
services.DecorateMessageBrokerWithBulkhead();
```

### With Rate Limiting

Combine with rate limiting for complete resource protection:

```csharp
services.AddMessageBrokerRateLimit(/* ... */);
services.AddMessageBrokerBulkhead(/* ... */);

services.DecorateMessageBrokerWithRateLimit();
services.DecorateMessageBrokerWithBulkhead();
```

## Troubleshooting

### High Rejection Rate

If you see many rejections:

1. Increase `MaxConcurrentOperations`
2. Increase `MaxQueuedOperations`
3. Optimize message processing to reduce operation duration
4. Scale horizontally by adding more instances

### Queue Always Full

If the queue is always full:

1. Check if consumers are keeping up with producers
2. Implement backpressure at the producer level
3. Consider increasing processing capacity
4. Review message processing logic for bottlenecks

### Memory Issues

If you experience memory issues:

1. Reduce `MaxQueuedOperations` to limit memory usage
2. Implement message size limits
3. Monitor queue depth metrics
4. Consider using external queuing mechanisms

## Performance Considerations

- **Overhead**: Minimal overhead (~1-2ms per operation)
- **Memory**: Queue memory usage = `MaxQueuedOperations * average_message_size`
- **Throughput**: Can handle 10,000+ operations/second with proper sizing
- **Latency**: Queued operations experience additional latency based on queue depth

## See Also

- [Circuit Breaker Pattern](../CircuitBreaker/README.md)
- [Rate Limiting](../RateLimit/README.md)
- [Backpressure Management](../Backpressure/README.md)
