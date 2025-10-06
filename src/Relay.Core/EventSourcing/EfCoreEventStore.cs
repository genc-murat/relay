using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Relay.Core.EventSourcing;

/// <summary>
/// EF Core implementation of IEventStore using PostgreSQL for persistence.
/// </summary>
public class EfCoreEventStore : IEventStore
{
    private readonly EventStoreDbContext _context;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreEventStore"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfCoreEventStore(EventStoreDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async ValueTask SaveEventsAsync(Guid aggregateId, IEnumerable<Event> events, int expectedVersion, CancellationToken cancellationToken = default)
    {
        var eventsList = events.ToList();
        if (!eventsList.Any())
        {
            return;
        }

        // Check for concurrency conflicts
        var lastVersion = await _context.Events
            .Where(e => e.AggregateId == aggregateId)
            .OrderByDescending(e => e.AggregateVersion)
            .Select(e => (int?)e.AggregateVersion)
            .FirstOrDefaultAsync(cancellationToken);

        var actualVersion = lastVersion ?? -1;

        if (expectedVersion != actualVersion)
        {
            throw new InvalidOperationException($"Concurrency conflict: expected version {expectedVersion} does not match actual version {actualVersion}.");
        }

        // Convert events to entities
        var entities = eventsList.Select(e => new EventEntity
        {
            Id = e.Id,
            AggregateId = e.AggregateId,
            AggregateVersion = e.AggregateVersion,
            EventType = e.EventType,
            EventData = JsonSerializer.Serialize(e, e.GetType(), _jsonOptions),
            Timestamp = e.Timestamp
        }).ToList();

        // Add entities to context
        await _context.Events.AddRangeAsync(entities, cancellationToken);

        // Save changes
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<Event> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        return GetEventsAsyncImpl(aggregateId, cancellationToken);

        async IAsyncEnumerable<Event> GetEventsAsyncImpl(Guid aggregateId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var entities = _context.Events
                .Where(e => e.AggregateId == aggregateId)
                .OrderBy(e => e.AggregateVersion)
                .AsAsyncEnumerable();

            await foreach (var entity in entities.WithCancellation(cancellationToken))
            {
                var eventType = FindEventType(entity.EventType);
                if (eventType == null)
                {
                    throw new InvalidOperationException($"Event type '{entity.EventType}' not found.");
                }

                var @event = JsonSerializer.Deserialize(entity.EventData, eventType, _jsonOptions) as Event;
                if (@event == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize event of type '{entity.EventType}'.");
                }

                yield return @event;
            }
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<Event> GetEventsAsync(Guid aggregateId, int startVersion, int endVersion, CancellationToken cancellationToken = default)
    {
        return GetEventsAsyncImpl(aggregateId, startVersion, endVersion, cancellationToken);

        async IAsyncEnumerable<Event> GetEventsAsyncImpl(Guid aggregateId, int startVersion, int endVersion, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var entities = _context.Events
                .Where(e => e.AggregateId == aggregateId &&
                           e.AggregateVersion >= startVersion &&
                           e.AggregateVersion <= endVersion)
                .OrderBy(e => e.AggregateVersion)
                .AsAsyncEnumerable();

            await foreach (var entity in entities.WithCancellation(cancellationToken))
            {
                var eventType = FindEventType(entity.EventType);
                if (eventType == null)
                {
                    throw new InvalidOperationException($"Event type '{entity.EventType}' not found.");
                }

                var @event = JsonSerializer.Deserialize(entity.EventData, eventType, _jsonOptions) as Event;
                if (@event == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize event of type '{entity.EventType}'.");
                }

                yield return @event;
            }
        }
    }

    /// <summary>
    /// Finds an event type by name, searching in loaded assemblies.
    /// </summary>
    /// <param name="typeName">The type name to find.</param>
    /// <returns>The event type if found; otherwise, null.</returns>
    private static Type? FindEventType(string typeName)
    {
        // Try to get the type directly first
        var type = Type.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        // Search through all loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }
}
