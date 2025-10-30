# Inbox Pattern

The Inbox pattern ensures idempotent message processing by tracking processed messages. This prevents duplicate message handling in distributed systems where messages may be delivered more than once.

## Overview

The Inbox pattern works by:
1. Checking if a message ID exists in the inbox before processing
2. Skipping processing if the message has already been handled
3. Storing the message ID in the inbox after successful processing
4. Periodically cleaning up expired inbox entries

## Features

- **Idempotent Processing**: Prevents duplicate message handling
- **Multiple Storage Options**: In-memory (testing) and SQL (production)
- **Automatic Cleanup**: Background worker removes expired entries
- **Configurable Retention**: Control how long processed messages are tracked
- **Consumer Tracking**: Track which consumer processed each message
- **Metrics**: Built-in metrics for cleanup operations

## Usage

### Basic Setup (In-Memory)

```csharp
services.AddInboxPattern(options =>
{
    options.Enabled = true;
    options.RetentionPeriod = TimeSpan.FromDays(7);
    options.CleanupInterval = TimeSpan.FromHours(1);
    options.ConsumerName = "MyService";
});

// Decorate your message broker
services.DecorateMessageBrokerWithInbox();
```

### SQL Storage Setup

```csharp
services.AddInboxPatternWithSql(
    dbOptions => dbOptions.UseSqlServer(connectionString),
    options =>
    {
        options.Enabled = true;
        options.RetentionPeriod = TimeSpan.FromDays(7);
        options.CleanupInterval = TimeSpan.FromHours(1);
    });

// Decorate your message broker
services.DecorateMessageBrokerWithInbox();
```

### PostgreSQL Setup

```csharp
services.AddInboxPatternWithSql(
    dbOptions => dbOptions.UseNpgsql(connectionString),
    options =>
    {
        options.Enabled = true;
        options.RetentionPeriod = TimeSpan.FromDays(7);
    });

services.DecorateMessageBrokerWithInbox();
```

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `Enabled` | `false` | Enable/disable the inbox pattern |
| `RetentionPeriod` | 7 days | How long to keep processed message IDs (minimum 24 hours) |
| `CleanupInterval` | 1 hour | How often to clean up expired entries (minimum 1 hour) |
| `ConsumerName` | Machine name | Name of the consumer for tracking |

## Database Schema

### InboxMessages Table

```sql
CREATE TABLE InboxMessages (
    MessageId NVARCHAR(500) PRIMARY KEY,
    MessageType NVARCHAR(500) NOT NULL,
    ProcessedAt DATETIMEOFFSET NOT NULL,
    ConsumerName NVARCHAR(500) NULL
);

CREATE INDEX IX_InboxMessages_ProcessedAt ON InboxMessages(ProcessedAt);
CREATE INDEX IX_InboxMessages_MessageType ON InboxMessages(MessageType);
```

## How It Works

### Message Processing Flow

```
1. Message arrives → Extract MessageId from context
2. Check if MessageId exists in inbox
3. If exists → Skip processing, acknowledge message
4. If not exists → Process message
5. After successful processing → Store MessageId in inbox
6. If processing fails → Don't store in inbox (allow retry)
```

### Cleanup Process

The `InboxCleanupWorker` runs in the background:
- Executes at the configured `CleanupInterval`
- Removes entries older than `RetentionPeriod`
- Logs metrics (entries removed, duration)
- Tracks total operations and average duration

## Message ID Requirements

The inbox pattern requires messages to have a unique `MessageId` in the `MessageContext`. Most message brokers automatically provide this:

- **RabbitMQ**: Uses message ID from properties
- **Kafka**: Uses offset or custom message ID
- **Azure Service Bus**: Uses message ID
- **AWS SQS**: Uses message ID

If your messages don't have IDs, the inbox pattern will log a warning and process the message without deduplication.

## Best Practices

1. **Set Appropriate Retention**: Balance between memory/storage and duplicate detection window
2. **Monitor Cleanup Metrics**: Track cleanup operations to ensure they complete successfully
3. **Use SQL in Production**: In-memory storage is only for testing
4. **Consumer Names**: Use meaningful names to track which services process messages
5. **Database Indexes**: Ensure indexes on `ProcessedAt` for efficient cleanup

## Combining with Outbox Pattern

You can use both patterns together for end-to-end reliability:

```csharp
// Publisher side: Outbox pattern
services.AddOutboxPatternWithSql(dbOptions => dbOptions.UseSqlServer(connectionString));
services.DecorateMessageBrokerWithOutbox();

// Consumer side: Inbox pattern
services.AddInboxPatternWithSql(dbOptions => dbOptions.UseSqlServer(connectionString));
services.DecorateMessageBrokerWithInbox();
```

## Metrics

The `InboxCleanupWorker` exposes metrics:
- `TotalEntriesRemoved`: Total number of expired entries removed
- `TotalCleanupOperations`: Total number of cleanup operations performed
- `AverageCleanupDurationMs`: Average duration of cleanup operations

## Troubleshooting

### Messages Being Processed Multiple Times

- Check that `MessageId` is present in `MessageContext`
- Verify inbox pattern is enabled (`Enabled = true`)
- Ensure decorator is registered after the broker

### Inbox Growing Too Large

- Reduce `RetentionPeriod` if appropriate
- Increase `CleanupInterval` frequency
- Check cleanup worker logs for errors

### Performance Issues

- Add database indexes on `ProcessedAt` and `MessageType`
- Consider partitioning the inbox table for high-volume scenarios
- Monitor cleanup operation duration

## Example: Complete Setup

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register message broker
    services.AddRabbitMQMessageBroker(options =>
    {
        options.HostName = "localhost";
    });

    // Add inbox pattern with SQL storage
    services.AddInboxPatternWithSql(
        dbOptions => dbOptions.UseSqlServer(
            "Server=localhost;Database=MessageBroker;Trusted_Connection=True;"),
        options =>
        {
            options.Enabled = true;
            options.RetentionPeriod = TimeSpan.FromDays(7);
            options.CleanupInterval = TimeSpan.FromHours(1);
            options.ConsumerName = "OrderService";
        });

    // Decorate the broker
    services.DecorateMessageBrokerWithInbox();
}
```

## Migration

To migrate from a system without inbox pattern:

1. Deploy the inbox pattern with `Enabled = false`
2. Run database migrations to create the inbox table
3. Enable the pattern (`Enabled = true`)
4. Monitor for duplicate processing (should decrease)
5. Adjust `RetentionPeriod` based on your duplicate detection needs
