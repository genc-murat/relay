# EF Core Event Store Example

This example demonstrates how to use the EF Core-based Event Store with PostgreSQL for persistent event sourcing.

## Overview

This example shows:
- Setting up EF Core Event Store with PostgreSQL
- Saving events to a persistent database
- Retrieving events from the database
- Handling concurrency conflicts
- Using the event store with Domain-Driven Design patterns

## Prerequisites

- PostgreSQL server running locally or accessible remotely
- .NET 8.0 SDK
- Connection string to PostgreSQL database

## Setup

### 1. Install PostgreSQL

Download and install PostgreSQL from https://www.postgresql.org/download/

### 2. Create Database

```sql
CREATE DATABASE relay_events;
```

### 3. Update Connection String

Update the connection string in the example:

```csharp
services.AddEfCoreEventStore(
    "Host=localhost;Database=relay_events;Username=postgres;Password=YOUR_PASSWORD");
```

## Running the Example

### From the sample file:

```csharp
using Relay.Core.Examples;

await EventStoreExample.RunExampleAsync();
```

### Or integrate into your Program.cs:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.EventSourcing;

var services = new ServiceCollection();

// Add EF Core Event Store
services.AddEfCoreEventStore(
    "Host=localhost;Database=relay_events;Username=postgres;Password=postgres");

var serviceProvider = services.BuildServiceProvider();

// Ensure database is created
await serviceProvider.EnsureEventStoreDatabaseAsync();

// Use the event store
var eventStore = serviceProvider.GetRequiredService<IEventStore>();

// Your code here...
```

## Features Demonstrated

### 1. Event Creation and Storage

```csharp
var events = new List<Event>
{
    new OrderCreatedEvent
    {
        AggregateId = aggregateId,
        AggregateVersion = 0,
        OrderNumber = "ORD-001",
        Amount = 99.99m
    }
};

await eventStore.SaveEventsAsync(aggregateId, events, expectedVersion: -1);
```

### 2. Event Retrieval

```csharp
await foreach (var @event in eventStore.GetEventsAsync(aggregateId))
{
    Console.WriteLine($"{@event.EventType} (v{@event.AggregateVersion})");
}
```

### 3. Version Range Queries

```csharp
await foreach (var @event in eventStore.GetEventsAsync(aggregateId, startVersion: 0, endVersion: 10))
{
    // Process events in version range
}
```

### 4. Concurrency Control

```csharp
try
{
    // This will fail if the expected version doesn't match
    await eventStore.SaveEventsAsync(aggregateId, newEvents, expectedVersion: 5);
}
catch (InvalidOperationException ex)
{
    // Handle concurrency conflict
}
```

## Database Schema

The event store creates a table with the following structure:

```sql
CREATE TABLE "Events" (
    "Id" uuid NOT NULL,
    "AggregateId" uuid NOT NULL,
    "AggregateVersion" integer NOT NULL,
    "EventType" character varying(500) NOT NULL,
    "EventData" text NOT NULL,
    "Timestamp" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Events" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_Events_AggregateId" ON "Events" ("AggregateId");
CREATE UNIQUE INDEX "IX_Events_AggregateId_Version" ON "Events" ("AggregateId", "AggregateVersion");
```

## Key Concepts

### Aggregate ID
Unique identifier for an aggregate root in Domain-Driven Design.

### Aggregate Version
Sequential version number for optimistic concurrency control.

### Event Type
Full type name of the event class for deserialization.

### Event Data
JSON-serialized event data.

### Timestamp
When the event occurred (UTC).

## Production Considerations

1. **Connection Pooling**: EF Core handles connection pooling automatically
2. **Migrations**: Use `dotnet ef migrations` commands for schema changes
3. **Indexing**: The unique index on (AggregateId, Version) ensures data integrity
4. **Backups**: Implement regular PostgreSQL backup strategies
5. **Monitoring**: Monitor database performance and query execution times
6. **Scaling**: Consider read replicas for high-read scenarios

## See Also

- [Event Sourcing README](../../../src/Relay.Core/EventSourcing/README.md)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [EF Core Documentation](https://learn.microsoft.com/ef/core/)
