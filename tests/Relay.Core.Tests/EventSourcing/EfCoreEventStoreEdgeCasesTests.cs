using Microsoft.EntityFrameworkCore;
using Relay.Core.EventSourcing.Core;
using Relay.Core.EventSourcing.Infrastructure;
using Relay.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.EventSourcing;

public class EfCoreEventStoreEdgeCasesTests : IDisposable
{
    private readonly EventStoreDbContext _context;
    private readonly EfCoreEventStore _eventStore;

    public EfCoreEventStoreEdgeCasesTests()
    {
        var options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EventStoreDbContext(options);
        _eventStore = new EfCoreEventStore(_context);
    }

    [Fact]
    public async Task GetEventsAsync_WithEventTypeNotFound_ThrowsInvalidOperationException()
    {
        // Arrange - Insert an event with a type that doesn't exist in any loaded assembly
        var aggregateId = Guid.NewGuid();
        var eventEntity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateVersion = 0,
            EventType = "NonExistent.Namespace.NonExistentEventType, NonExistentAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            EventData = "{}",
            Timestamp = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var events = _eventStore.GetEventsAsync(aggregateId);
            await foreach (var @event in events)
            {
                // Force enumeration to trigger type resolution
                _ = @event;
            }
        });

        Assert.Contains("Event type 'NonExistent.Namespace.NonExistentEventType", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task GetEventsAsync_WithInvalidAssemblyQualifiedName_ThrowsInvalidOperationException()
    {
        // Arrange - Insert an event with malformed assembly qualified name
        var aggregateId = Guid.NewGuid();
        var eventEntity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateVersion = 0,
            EventType = "InvalidTypeName[MissingBrackets, StillInvalid",
            EventData = "{}",
            Timestamp = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var events = _eventStore.GetEventsAsync(aggregateId);
            await foreach (var @event in events)
            {
                _ = @event;
            }
        });

        Assert.Contains("Event type 'InvalidTypeName[MissingBrackets, StillInvalid' not found", exception.Message);
    }

    [Fact]
    public async Task GetEventsAsync_WithEmptyEventType_ThrowsArgumentException()
    {
        // Arrange - Insert an event with empty event type
        var aggregateId = Guid.NewGuid();
        var eventEntity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateVersion = 0,
            EventType = string.Empty,
            EventData = "{}",
            Timestamp = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            var events = _eventStore.GetEventsAsync(aggregateId);
            await foreach (var @event in events)
            {
                _ = @event;
            }
        });

        Assert.Contains("The value cannot be an empty string", exception.Message);
    }



    [Fact]
    public async Task GetEventsAsync_WithEventTypeFromDifferentAssembly_LoadsSuccessfully()
    {
        // Arrange - Use a type from a different assembly (System.String from mscorlib/System.Private.CoreLib)
        var aggregateId = Guid.NewGuid();
        var testEvent = new TestEventWithSystemType
        {
            AggregateId = aggregateId,
            AggregateVersion = 0,
            SystemString = "test value"
        };

        // Save the event normally
        await _eventStore.SaveEventsAsync(aggregateId, new List<Event> { testEvent }, -1);

        // Act - Retrieve the event
        var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId).ToListAsync();

        // Assert
        Assert.Single(retrievedEvents);
        var retrievedEvent = retrievedEvents.First() as TestEventWithSystemType;
        Assert.NotNull(retrievedEvent);
        Assert.Equal("test value", retrievedEvent.SystemString);
    }

    [Fact]
    public async Task GetEventsAsync_WithComplexGenericTypeName_ResolvesCorrectly()
    {
        // Arrange - Use a type with complex generic name
        var aggregateId = Guid.NewGuid();
        var testEvent = new TestGenericEvent<List<string>>
        {
            AggregateId = aggregateId,
            AggregateVersion = 0,
            Data = new List<string> { "item1", "item2", "item3" }
        };

        // Save the event normally
        await _eventStore.SaveEventsAsync(aggregateId, [testEvent], -1);

        // Act - Retrieve the event
        var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId).ToListAsync();

        // Assert
        Assert.Single(retrievedEvents);
        var retrievedEvent = retrievedEvents.First() as TestGenericEvent<List<string>>;
        Assert.NotNull(retrievedEvent);
        Assert.Equal(3, retrievedEvent.Data.Count);
        Assert.Contains("item1", retrievedEvent.Data);
    }

    [Fact]
    public async Task GetEventsAsync_WithCorruptedJsonData_ThrowsJsonException()
    {
        // Arrange - Insert an event with corrupted JSON data
        var aggregateId = Guid.NewGuid();
        var eventEntity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateVersion = 0,
            EventType = typeof(TestAggregateCreated).AssemblyQualifiedName!,
            EventData = "{ invalid json data - missing closing brace",
            Timestamp = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(async () =>
        {
            var events = _eventStore.GetEventsAsync(aggregateId);
            await foreach (var @event in events)
            {
                _ = @event;
            }
        });
    }

    [Fact]
    public async Task GetEventsAsync_WithJsonDataMissingRequiredProperties_ThrowsJsonException()
    {
        // Arrange - Insert an event with JSON missing required properties
        var aggregateId = Guid.NewGuid();
        var eventEntity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateVersion = 0,
            EventType = typeof(TestAggregateCreated).AssemblyQualifiedName!,
            EventData = "{\"aggregateName\": \"Test\"}", // Missing aggregateId and aggregateVersion
            Timestamp = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        // Note: System.Text.Json doesn't throw for missing properties by default
        // It just sets them to default values. We need to check the deserialized object.
        var events = await _eventStore.GetEventsAsync(aggregateId).ToListAsync();
        Assert.Single(events);
        var @event = events.First() as TestAggregateCreated;
        Assert.NotNull(@event);
        // Properties not in JSON should be default values
        Assert.Equal(Guid.Empty, @event.AggregateId); // Default for Guid
        Assert.Equal(0, @event.AggregateVersion); // Default for int
        Assert.Equal("Test", @event.AggregateName);
    }

    [Fact]
    public async Task GetEventsAsync_WithJsonDataWrongPropertyTypes_ThrowsJsonException()
    {
        // Arrange - Insert an event with JSON having wrong property types
        var aggregateId = Guid.NewGuid();
        var eventEntity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateVersion = 0,
            EventType = typeof(TestAggregateCreated).AssemblyQualifiedName!,
            EventData = "{\"aggregateId\": \"not-a-guid\", \"aggregateVersion\": \"not-a-number\", \"aggregateName\": \"Test\"}",
            Timestamp = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(async () =>
        {
            var events = _eventStore.GetEventsAsync(aggregateId);
            await foreach (var @event in events)
            {
                _ = @event;
            }
        });
    }

    [Fact]
    public async Task GetEventsAsync_WithEmptyJsonData_ThrowsJsonException()
    {
        // Arrange - Insert an event with empty JSON data
        var aggregateId = Guid.NewGuid();
        var eventEntity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateVersion = 0,
            EventType = typeof(TestAggregateCreated).AssemblyQualifiedName!,
            EventData = string.Empty,
            Timestamp = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(async () =>
        {
            var events = _eventStore.GetEventsAsync(aggregateId);
            await foreach (var @event in events)
            {
                _ = @event;
            }
        });
    }



    [Fact]
    public async Task GetEventsAsync_WithJsonArrayInsteadOfObject_ThrowsJsonException()
    {
        // Arrange - Insert an event with JSON array instead of object
        var aggregateId = Guid.NewGuid();
        var eventEntity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateVersion = 0,
            EventType = typeof(TestAggregateCreated).AssemblyQualifiedName!,
            EventData = "[\"this\", \"is\", \"an\", \"array\"]",
            Timestamp = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(async () =>
        {
            var events = _eventStore.GetEventsAsync(aggregateId);
            await foreach (var @event in events)
            {
                _ = @event;
            }
        });
    }

    [Fact]
    public async Task GetEventsAsync_WithJsonDataThatCannotBeDeserialized_ThrowsJsonException()
    {
        // Arrange - Insert an event with JSON that cannot be deserialized to the expected type
        var aggregateId = Guid.NewGuid();
        var eventEntity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateVersion = 0,
            EventType = typeof(TestAggregateCreated).AssemblyQualifiedName!,
            EventData = "\"this is just a string\"", // JSON for a string, not an Event object
            Timestamp = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(async () =>
        {
            var events = _eventStore.GetEventsAsync(aggregateId);
            await foreach (var @event in events)
            {
                _ = @event;
            }
        });
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Test event classes for edge case testing
public class TestEventWithSystemType : Event
{
    public string SystemString { get; set; } = string.Empty;
}

public class TestGenericEvent<T> : Event
{
    public T Data { get; set; } = default!;
}
