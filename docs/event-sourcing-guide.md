# Event Sourcing

Relay provides built-in support for event sourcing. This feature allows you to implement event-sourced aggregates and persist events to an event store.

## 🚀 Quick Start

### 1. Enable Event Sourcing

To enable event sourcing, call `AddRelayEventSourcing()` when configuring services:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRelay();
builder.Services.AddRelayEventSourcing(); // Enable event sourcing
```

### 2. Define Events

Create events by inheriting from the `Event` base class:

```csharp
public class UserCreatedEvent : Event
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public class UserUpdatedEvent : Event
{
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### 3. Define Aggregate Roots

Create aggregate roots by inheriting from the `AggregateRoot<TId>` base class:

```csharp
public class UserAggregate : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Email { get; private set; }

    public static UserAggregate Create(Guid id, string name, string email)
    {
        var user = new UserAggregate { Id = id };
        user.Apply(new UserCreatedEvent
        {
            Name = name,
            Email = email
        });
        return user;
    }

    public void Update(string name, string email)
    {
        Apply(new UserUpdatedEvent
        {
            Name = name,
            Email = email
        });
    }

    private void Apply(UserCreatedEvent @event)
    {
        Name = @event.Name;
        Email = @event.Email;
    }

    private void Apply(UserUpdatedEvent @event)
    {
        Name = @event.Name;
        Email = @event.Email;
    }
}
```

### 4. Use Event Sourcing

Use the event sourcing repository to save and load aggregates:

```csharp
// Get the event sourcing repository
var repository = serviceProvider.GetRequiredService<IEventSourcedRepository<UserAggregate, Guid>>();

// Create and save an aggregate
var user = UserAggregate.Create(Guid.NewGuid(), "John Doe", "john.doe@example.com");
await repository.SaveAsync(user);

// Load an aggregate
var loadedUser = await repository.GetByIdAsync(user.Id);
```

## 🎯 Key Features

### Event Base Class

The `Event` base class provides common properties for all events:

```csharp
public abstract class Event
{
    public Guid Id { get; }
    public DateTime Timestamp { get; }
    public string EventType { get; }
    public int AggregateVersion { get; set; }
    public Guid AggregateId { get; set; }
}
```

### Aggregate Root Base Class

The `AggregateRoot<TId>` base class provides functionality for event-sourced aggregates:

```csharp
public abstract class AggregateRoot<TId>
{
    public TId Id { get; protected set; }
    public int Version { get; private set; }
    public IReadOnlyList<Event> UncommittedEvents { get; }
    public void ClearUncommittedEvents();
    protected void Apply(Event @event);
    public void LoadFromHistory(IEnumerable<Event> events);
}
```

### Event Store Interface

The `IEventStore` interface provides methods for persisting and retrieving events:

```csharp
public interface IEventStore
{
    ValueTask SaveEventsAsync(Guid aggregateId, IEnumerable<Event> events, int expectedVersion, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Event> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Event> GetEventsAsync(Guid aggregateId, int startVersion, int endVersion, CancellationToken cancellationToken = default);
}
```

### Event Sourced Repository

The `IEventSourcedRepository<TAggregate, TId>` interface provides methods for working with event-sourced aggregates:

```csharp
public interface IEventSourcedRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>, new()
{
    ValueTask<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    ValueTask SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}
```

## 🛠️ Advanced Configuration

### Event Store Implementation

Register a custom event store implementation:

```csharp
services.AddTransient<IEventStore, CustomEventStore>();
```

### Event Sourcing Options

Configure event sourcing options:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultEventSourcingOptions.EnableEventSourcing = true;
    options.DefaultEventSourcingOptions.SnapshotInterval = 50;
});
```

### Concurrency Handling

Configure concurrency conflict handling:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultEventSourcingOptions.ThrowOnConcurrencyConflict = false;
});
```

## ⚡ Performance

Event sourcing is designed to be efficient and scalable:

- **In-Memory Implementation**: Fast in-memory event store for development
- **Concurrency Control**: Optimistic concurrency control with version checking
- **Event Streaming**: Efficient event streaming with async enumerables
- **Snapshot Support**: Future support for snapshots to improve load performance

## 🧪 Testing

Event-sourced aggregates can be tested by applying events and verifying state changes.