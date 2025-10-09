using Relay.Core.EventSourcing.Core;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.EventSourcing.Stores;

/// <summary>
/// In-memory implementation of ISnapshotStore for testing and development.
/// </summary>
public class InMemorySnapshotStore : ISnapshotStore
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<int, object>> _snapshots = new();

    /// <inheritdoc />
    public ValueTask SaveSnapshotAsync<TAggregate>(Guid aggregateId, TAggregate snapshot, int version, CancellationToken cancellationToken = default)
        where TAggregate : class
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var aggregateSnapshots = _snapshots.GetOrAdd(aggregateId, _ => new ConcurrentDictionary<int, object>());
        aggregateSnapshots[version] = snapshot;

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<(TAggregate? Snapshot, int Version)?> GetSnapshotAsync<TAggregate>(Guid aggregateId, CancellationToken cancellationToken = default)
        where TAggregate : class
    {
        if (!_snapshots.TryGetValue(aggregateId, out var aggregateSnapshots) || aggregateSnapshots.IsEmpty)
        {
            return ValueTask.FromResult<(TAggregate? Snapshot, int Version)?>(null);
        }

        // Get the latest snapshot (highest version)
        var latestSnapshot = aggregateSnapshots
            .OrderByDescending(kvp => kvp.Key)
            .FirstOrDefault();

        if (latestSnapshot.Value is TAggregate typedSnapshot)
        {
            return ValueTask.FromResult<(TAggregate? Snapshot, int Version)?>((typedSnapshot, latestSnapshot.Key));
        }

        return ValueTask.FromResult<(TAggregate? Snapshot, int Version)?>(null);
    }

    /// <inheritdoc />
    public ValueTask DeleteOldSnapshotsAsync(Guid aggregateId, int olderThanVersion, CancellationToken cancellationToken = default)
    {
        if (_snapshots.TryGetValue(aggregateId, out var aggregateSnapshots))
        {
            var versionsToDelete = aggregateSnapshots.Keys.Where(v => v < olderThanVersion).ToList();
            foreach (var version in versionsToDelete)
            {
                aggregateSnapshots.TryRemove(version, out _);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Clears all snapshots (useful for testing).
    /// </summary>
    public void Clear()
    {
        _snapshots.Clear();
    }
}
