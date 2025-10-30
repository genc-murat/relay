# Poison Message Handling

The Poison Message Handling feature automatically detects and isolates messages that repeatedly fail processing, preventing system degradation and allowing for later analysis or reprocessing.

## Overview

A poison message is a message that causes processing failures repeatedly, potentially due to:
- Invalid data format
- Business logic errors
- External service failures
- Serialization issues

This feature tracks message processing failures and automatically moves messages to a poison queue when they exceed a configurable failure threshold.

## Features

- **Automatic Failure Tracking**: Tracks processing failures per message ID
- **Configurable Threshold**: Set the number of failures before a message is considered poisonous
- **Poison Queue**: Isolated storage for poison messages with full diagnostic information
- **Reprocessing API**: Ability to retry poison messages after fixing issues
- **Automatic Cleanup**: Periodic removal of expired poison messages based on retention period
- **Comprehensive Logging**: Full diagnostic information for troubleshooting

## Configuration

### Basic Setup

```csharp
services.AddMessageBroker(options =>
{
    options.PoisonMessage = new PoisonMessageOptions
    {
        Enabled = true,
        FailureThreshold = 5,
        RetentionPeriod = TimeSpan.FromDays(7),
        CleanupInterval = TimeSpan.FromHours(1)
    };
});

// Add poison message handling services
services.AddPoisonMessageHandling(options =>
{
    options.Enabled = true;
    options.FailureThreshold = 5;
    options.RetentionPeriod = TimeSpan.FromDays(7);
});
```

### Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `Enabled` | Enable/disable poison message handling | `false` |
| `FailureThreshold` | Number of failures before message is poisonous | `5` |
| `RetentionPeriod` | How long to keep poison messages | `7 days` |
| `CleanupInterval` | How often to clean up expired messages | `1 hour` |

## Usage

### Automatic Handling

Once configured, poison message handling works automatically:

```csharp
// Subscribe to messages
await messageBroker.SubscribeAsync<OrderCreatedEvent>(async (message, context, ct) =>
{
    // If this handler throws an exception repeatedly,
    // the message will automatically be moved to poison queue
    await ProcessOrder(message);
});
```

### Retrieving Poison Messages

```csharp
public class PoisonMessageController : ControllerBase
{
    private readonly IPoisonMessageHandler _poisonMessageHandler;

    public PoisonMessageController(IPoisonMessageHandler poisonMessageHandler)
    {
        _poisonMessageHandler = poisonMessageHandler;
    }

    [HttpGet("api/poison-messages")]
    public async Task<IActionResult> GetPoisonMessages()
    {
        var messages = await _poisonMessageHandler.GetPoisonMessagesAsync();
        return Ok(messages);
    }
}
```

### Reprocessing Poison Messages

```csharp
[HttpPost("api/poison-messages/{id}/reprocess")]
public async Task<IActionResult> ReprocessMessage(Guid id)
{
    await _poisonMessageHandler.ReprocessAsync(id);
    return Ok();
}
```

## Custom Store Implementation

By default, poison messages are stored in memory. For production use, implement a custom store:

```csharp
public class SqlPoisonMessageStore : IPoisonMessageStore
{
    private readonly DbContext _dbContext;

    public SqlPoisonMessageStore(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask StoreAsync(PoisonMessage message, CancellationToken cancellationToken)
    {
        _dbContext.PoisonMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // Implement other methods...
}

// Register custom store
services.AddPoisonMessageHandling<SqlPoisonMessageStore>(options =>
{
    options.Enabled = true;
    options.FailureThreshold = 5;
});
```

## Monitoring

### Logging

The poison message handler logs important events:

```
[Warning] Message processing failure tracked. MessageId: abc123, MessageType: OrderCreatedEvent, FailureCount: 3/5
[Error] Message exceeded failure threshold. Moving to poison queue. MessageId: abc123, MessageType: OrderCreatedEvent, FailureCount: 5
[Warning] Poison message stored. MessageId: abc123, MessageType: OrderCreatedEvent, FailureCount: 5, Errors: ...
[Information] Cleaned up 10 expired poison messages older than 7.00:00:00
```

### Metrics

Track poison message metrics in your monitoring system:

```csharp
public class PoisonMessageMetrics
{
    private readonly IPoisonMessageHandler _handler;

    public async Task<int> GetPoisonMessageCount()
    {
        var messages = await _handler.GetPoisonMessagesAsync();
        return messages.Count();
    }
}
```

## Best Practices

1. **Set Appropriate Thresholds**: Balance between catching genuine poison messages and allowing for transient failures
2. **Monitor Poison Queue**: Regularly review poison messages to identify systemic issues
3. **Implement Alerting**: Alert when poison messages accumulate
4. **Use Persistent Store**: Use a database-backed store in production
5. **Regular Cleanup**: Ensure cleanup worker is running to prevent unbounded growth
6. **Root Cause Analysis**: Investigate poison messages to fix underlying issues

## Troubleshooting

### Messages Not Moving to Poison Queue

- Verify `PoisonMessage.Enabled` is `true` in both MessageBrokerOptions and PoisonMessageOptions
- Check that `IPoisonMessageHandler` is registered in DI container
- Ensure message context has a valid `MessageId`
- Review logs for any errors in poison message handling

### Poison Messages Not Being Cleaned Up

- Verify `PoisonMessageCleanupWorker` is registered as a hosted service
- Check that `CleanupInterval` is set appropriately
- Review logs for cleanup worker activity

### High Memory Usage

- Implement a persistent store instead of in-memory store
- Reduce `RetentionPeriod` to clean up messages more frequently
- Increase `CleanupInterval` frequency

## Integration with Other Features

### Circuit Breaker

Poison message handling works alongside circuit breaker:
- Circuit breaker prevents cascading failures
- Poison message handling isolates problematic messages

### Retry Policy

Messages are retried according to retry policy before being tracked as failures:
1. Message fails processing
2. Retry policy attempts retries
3. After all retries exhausted, failure is tracked
4. After threshold reached, message moves to poison queue

### Dead Letter Queue

Poison messages are different from dead letter queue messages:
- **Dead Letter Queue**: Messages that couldn't be delivered (routing issues, queue full, etc.)
- **Poison Messages**: Messages that were delivered but failed processing repeatedly

## Requirements Satisfied

This implementation satisfies the following requirements:

- **13.1**: Tracks message processing failures with configurable threshold
- **13.2**: Moves messages to poison queue when threshold exceeded
- **13.3**: Logs poison message events with full diagnostic information
- **13.4**: Provides API to retrieve and reprocess poison messages
- **13.5**: Supports configurable retention periods with automatic cleanup
