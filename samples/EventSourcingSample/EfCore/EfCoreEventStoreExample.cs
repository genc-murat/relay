using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.EventSourcing;

namespace Relay.Core.Examples;

/// <summary>
/// Example event for demonstration.
/// </summary>
public class OrderCreatedEvent : Event
{
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// Example event for demonstration.
/// </summary>
public class OrderShippedEvent : Event
{
    public string TrackingNumber { get; set; } = string.Empty;
}

/// <summary>
/// Example usage of EF Core Event Store.
/// </summary>
public static class EventStoreExample
{
    /// <summary>
    /// Demonstrates basic event store operations.
    /// </summary>
    public static async Task RunExampleAsync()
    {
        // 1. Setup DI container
        var services = new ServiceCollection();
        
        // Add EF Core Event Store with PostgreSQL
        services.AddEfCoreEventStore(
            "Host=localhost;Database=relay_events;Username=postgres;Password=postgres");

        var serviceProvider = services.BuildServiceProvider();

        // 2. Ensure database is created
        await serviceProvider.EnsureEventStoreDatabaseAsync();

        // 3. Get event store instance
        var eventStore = serviceProvider.GetRequiredService<IEventStore>();

        // 4. Create some events
        var aggregateId = Guid.NewGuid();
        var events = new List<Event>
        {
            new OrderCreatedEvent
            {
                AggregateId = aggregateId,
                AggregateVersion = 0,
                OrderNumber = "ORD-001",
                Amount = 99.99m
            },
            new OrderShippedEvent
            {
                AggregateId = aggregateId,
                AggregateVersion = 1,
                TrackingNumber = "TRK-123456"
            }
        };

        // 5. Save events
        await eventStore.SaveEventsAsync(aggregateId, events, expectedVersion: -1);
        Console.WriteLine($"Saved {events.Count} events for aggregate {aggregateId}");

        // 6. Retrieve events
        Console.WriteLine("\nRetrieving events:");
        await foreach (var @event in eventStore.GetEventsAsync(aggregateId))
        {
            Console.WriteLine($"  - {@event.EventType} (v{@event.AggregateVersion}) at {@event.Timestamp}");
            
            if (@event is OrderCreatedEvent created)
            {
                Console.WriteLine($"    Order: {created.OrderNumber}, Amount: {created.Amount}");
            }
            else if (@event is OrderShippedEvent shipped)
            {
                Console.WriteLine($"    Tracking: {shipped.TrackingNumber}");
            }
        }

        // 7. Retrieve events by version range
        Console.WriteLine("\nRetrieving events by version (0-1):");
        await foreach (var @event in eventStore.GetEventsAsync(aggregateId, 0, 1))
        {
            Console.WriteLine($"  - {@event.EventType} (v{@event.AggregateVersion})");
        }

        // 8. Demonstrate concurrency conflict
        try
        {
            var conflictingEvent = new OrderShippedEvent
            {
                AggregateId = aggregateId,
                AggregateVersion = 2,
                TrackingNumber = "TRK-999999"
            };

            // This will fail because we expect version 0 but actual version is 1
            await eventStore.SaveEventsAsync(
                aggregateId, 
                new[] { conflictingEvent }, 
                expectedVersion: 0);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"\nExpected concurrency conflict: {ex.Message}");
        }

        Console.WriteLine("\nExample completed successfully!");
    }
}
