# Outbox Pattern Implementation

This directory contains the implementation of the Outbox pattern for reliable message publishing in Relay.MessageBroker.

## Overview

The Outbox pattern ensures reliable message publishing by storing messages in a persistent store before attempting to send them to the message broker. This guarantees that messages are not lost even if the broker is temporarily unavailable.

## Components

### Core Interfaces and Models

- **`IOutboxStore`**: Interface for storing and retrieving outbox messages
- **`OutboxMessage`**: Entity representing a message in the outbox
- **`OutboxMessageStatus`**: Enum for message status (Pending, Published, Failed)
- **`OutboxOptions`**: Configuration options for the Outbox pattern

### Implementations

- **`InMemoryOutboxStore`**: In-memory implementation for testing purposes
- **`SqlOutboxStore`**: SQL-based implementation using Entity Framework Core
- **`OutboxDbContext`**: EF Core database context for outbox messages

### Background Processing

- **`OutboxWorker`**: Background service that polls the outbox and publishes pending messages
  - Configurable polling interval (minimum 100ms)
  - Batch processing of pending messages
  - Exponential backoff retry logic
  - Automatic failure handling after max retry attempts

### Decorator

- **`OutboxMessageBrokerDecorator`**: Decorator that wraps `IMessageBroker` to intercept publish operations
  - Stores messages in outbox instead of publishing directly
  - Can be enabled/disabled via configuration
  - Transparent to consumers

## Usage

### Basic Setup with In-Memory Store

```csharp
services.AddOutboxPattern(options =>
{
    options.Enabled = true;
    options.PollingInterval = TimeSpan.FromSeconds(5);
    options.BatchSize = 100;
    options.MaxRetryAttempts = 3;
});

// Decorate the message broker
services.DecorateMessageBrokerWithOutbox();
```

### Setup with SQL Store

```csharp
services.AddOutboxPatternWithSql(
    dbOptions => dbOptions.UseSqlServer(connectionString),
    options =>
    {
        options.Enabled = true;
        options.PollingInterval = TimeSpan.FromSeconds(5);
        options.BatchSize = 100;
        options.MaxRetryAttempts = 3;
    });

// Decorate the message broker
services.DecorateMessageBrokerWithOutbox();
```

## Features

✅ **Reliable Publishing**: Messages are stored before publishing, ensuring no loss
✅ **Automatic Retry**: Failed messages are retried with exponential backoff
✅ **Batch Processing**: Processes multiple messages in batches for efficiency
✅ **Configurable**: Polling interval, batch size, and retry attempts are configurable
✅ **Multiple Storage Options**: In-memory for testing, SQL for production
✅ **Transparent Integration**: Works seamlessly with existing message broker implementations

## Requirements Satisfied

- ✅ 1.1: Store messages in persistent outbox before sending
- ✅ 1.2: Background worker polls and publishes pending messages
- ✅ 1.3: Mark messages as published with timestamp
- ✅ 1.4: Configurable polling interval (minimum 100ms)
- ✅ 1.5: Move failed messages after max retry attempts

## Database Schema

The `OutboxMessages` table includes:
- `Id` (Guid, Primary Key)
- `MessageType` (string)
- `Payload` (byte[])
- `Headers` (JSON)
- `CreatedAt` (DateTimeOffset)
- `PublishedAt` (DateTimeOffset, nullable)
- `Status` (enum: Pending, Published, Failed)
- `RetryCount` (int)
- `LastError` (string, nullable)
- `RoutingKey` (string, nullable)
- `Exchange` (string, nullable)

Indexes on `Status` and `CreatedAt` for efficient querying.
