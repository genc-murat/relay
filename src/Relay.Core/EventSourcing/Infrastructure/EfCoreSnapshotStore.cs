using Microsoft.EntityFrameworkCore;
using Relay.Core.EventSourcing.Core;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.EventSourcing.Infrastructure;

/// <summary>
/// EF Core implementation of ISnapshotStore using database persistence.
/// </summary>
public class EfCoreSnapshotStore : ISnapshotStore
{
    private readonly EventStoreDbContext _context;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreSnapshotStore"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfCoreSnapshotStore(EventStoreDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async ValueTask SaveSnapshotAsync<TAggregate>(Guid aggregateId, TAggregate snapshot, int version, CancellationToken cancellationToken = default)
        where TAggregate : class
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var entity = new SnapshotEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            Version = version,
            AggregateType = typeof(TAggregate).AssemblyQualifiedName ?? typeof(TAggregate).FullName ?? typeof(TAggregate).Name,
            SnapshotData = JsonSerializer.Serialize(snapshot, _jsonOptions),
            Timestamp = DateTime.UtcNow
        };

        await _context.Snapshots.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<(TAggregate? Snapshot, int Version)?> GetSnapshotAsync<TAggregate>(Guid aggregateId, CancellationToken cancellationToken = default)
        where TAggregate : class
    {
        var latestSnapshot = await _context.Snapshots
            .Where(s => s.AggregateId == aggregateId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSnapshot == null)
        {
            return null;
        }

        var snapshot = JsonSerializer.Deserialize<TAggregate>(latestSnapshot.SnapshotData, _jsonOptions);
        if (snapshot == null)
        {
            throw new InvalidOperationException($"Failed to deserialize snapshot for aggregate {aggregateId} at version {latestSnapshot.Version}.");
        }

        return (snapshot, latestSnapshot.Version);
    }

    /// <inheritdoc />
    public async ValueTask DeleteOldSnapshotsAsync(Guid aggregateId, int olderThanVersion, CancellationToken cancellationToken = default)
    {
        var oldSnapshots = await _context.Snapshots
            .Where(s => s.AggregateId == aggregateId && s.Version < olderThanVersion)
            .ToListAsync(cancellationToken);

        if (oldSnapshots.Any())
        {
            _context.Snapshots.RemoveRange(oldSnapshots);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
