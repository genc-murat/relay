# Message Deduplication

The Message Deduplication feature provides automatic detection and prevention of duplicate message publishing in distributed systems.

## Overview

Message deduplication helps prevent processing duplicate messages by maintaining a cache of recently published messages and checking for duplicates before publishing. This is particularly useful in distributed systems where network issues or retries can lead to duplicate message delivery.

## Features

- **Multiple Deduplication Strategies**:
  - Content-based hash (SHA256)
  - Message ID-based
  - Custom hash function
- **Configurable Time Window**: Set the duplicate detection window from 1 minute to 24 hours
- **LRU Cache Eviction**: Automatic cache size management with Least Recently Used eviction
- **Automatic Cleanup**: Periodic removal of expired entries
- **Comprehensive Metrics**: Track duplicate detection rate, cache hit rate, and evictions

## Configuration

### Basic Setup

```csharp
services.AddMessageDeduplication(options =>
{
    options.Enabled = true;
    options.Window = TimeSpan.FromMinutes(5);
    options.MaxCacheSize = 100_000;
    options.Strategy = DeduplicationStrategy.ContentHash;
});

// Decorate the message broker with deduplication
services.DecorateWithDeduplication();
```

### Deduplication Strategies

#### 1. Content Hash (Default)

Uses SHA256 hash of the entire message content:

```csharp
options.Strategy = DeduplicationStrategy.ContentHash;
```

#### 2. Message ID

Uses a message ID from the publish options headers:

```csharp
options.Strategy = DeduplicationStrategy.MessageId;

// When publishing, include MessageId in headers
await broker.PublishAsync(message, new PublishOptions
{
    Headers = new Dictionary<string, object>
    {
        ["MessageId"] = Guid.NewGuid().ToString()
    }
});
```

#### 3. Custom Hash Function

Provide your own hash generation logic:

```csharp
options.Strategy = DeduplicationStrategy.Custom;
options.CustomHashFunction = (data) =>
{
    // Custom logic to generate hash from message data
    return MyCustomHashFunction(data);
};
```

## Usage Example

```csharp
// Configure services
services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    // ... other broker options
});

services.AddMessageDeduplication(options =>
{
    options.Enabled = true;
    options.Window = TimeSpan.FromMinutes(10);
    options.MaxCacheSize = 50_000;
    options.Strategy = DeduplicationStrategy.ContentHash;
});

services.DecorateWithDeduplication();

// Use the broker
var broker = serviceProvider.GetRequiredService<IMessageBroker>();

// First publish - will succeed
await broker.PublishAsync(new OrderCreatedEvent { OrderId = 123 });

// Duplicate publish within window - will be discarded
await broker.PublishAsync(new OrderCreatedEvent { OrderId = 123 });
```

## Metrics

Access deduplication metrics to monitor performance:

```csharp
var decorator = serviceProvider.GetRequiredService<IMessageBroker>() 
    as DeduplicationMessageBrokerDecorator;

var metrics = decorator?.GetMetrics();

Console.WriteLine($"Cache Size: {metrics.CurrentCacheSize}");
Console.WriteLine($"Duplicate Detection Rate: {metrics.DuplicateDetectionRate:P2}");
Console.WriteLine($"Cache Hit Rate: {metrics.CacheHitRate:P2}");
Console.WriteLine($"Total Duplicates Detected: {metrics.TotalDuplicatesDetected}");
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | false | Enable/disable deduplication |
| `Window` | TimeSpan | 5 minutes | Time window for duplicate detection (1 min - 24 hrs) |
| `MaxCacheSize` | int | 100,000 | Maximum number of entries in cache |
| `Strategy` | DeduplicationStrategy | ContentHash | Deduplication strategy to use |
| `CustomHashFunction` | Func<byte[], string> | null | Custom hash function (required for Custom strategy) |

## How It Works

1. **Message Publishing**: When a message is published, the decorator generates a hash based on the configured strategy
2. **Duplicate Check**: The hash is checked against the deduplication cache
3. **Discard or Publish**: 
   - If duplicate: Message is discarded and logged
   - If unique: Message is published and hash is added to cache
4. **Cache Management**: 
   - Expired entries are automatically cleaned up every minute
   - LRU eviction occurs when cache size exceeds maximum
5. **Metrics Tracking**: All operations are tracked for monitoring

## Best Practices

1. **Choose the Right Strategy**:
   - Use `ContentHash` for complete message deduplication
   - Use `MessageId` when you control message IDs
   - Use `Custom` for domain-specific deduplication logic

2. **Set Appropriate Window**:
   - Longer windows provide better duplicate detection
   - Shorter windows reduce memory usage
   - Consider your retry policies and network conditions

3. **Monitor Metrics**:
   - Track duplicate detection rate to understand duplicate frequency
   - Monitor cache size to ensure it stays within limits
   - Watch eviction count to tune MaxCacheSize

4. **Cache Size Tuning**:
   - Set MaxCacheSize based on expected message volume
   - Each entry uses approximately 100-200 bytes
   - Consider memory constraints of your deployment

## Performance Considerations

- **Memory Usage**: Each cache entry uses ~100-200 bytes. A cache of 100,000 entries uses ~10-20 MB
- **Hash Generation**: SHA256 hashing adds ~1-2ms overhead per message
- **Cache Lookup**: O(1) lookup time using ConcurrentDictionary
- **Cleanup**: Runs every minute, minimal impact on performance

## Thread Safety

All deduplication components are thread-safe and can be used in concurrent scenarios without additional synchronization.

## Limitations

- Deduplication only works within a single application instance
- For distributed deduplication across multiple instances, consider using a distributed cache (Redis, etc.)
- The cache is in-memory and will be lost on application restart
