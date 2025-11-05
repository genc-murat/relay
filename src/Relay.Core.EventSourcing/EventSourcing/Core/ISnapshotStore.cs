using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.EventSourcing.Core;

/// <summary>
/// Interface for snapshot stores that provide snapshot persistence for aggregates.
/// </summary>
public interface ISnapshotStore
{
    /// <summary>
    /// Saves a snapshot of an aggregate.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <param name="aggregateId">The identifier of the aggregate.</param>
    /// <param name="snapshot">The aggregate snapshot.</param>
    /// <param name="version">The version of the aggregate at the time of the snapshot.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ValueTask representing the completion of the operation.</returns>
    ValueTask SaveSnapshotAsync<TAggregate>(Guid aggregateId, TAggregate snapshot, int version, CancellationToken cancellationToken = default)
        where TAggregate : class;

    /// <summary>
    /// Gets the latest snapshot for an aggregate.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <param name="aggregateId">The identifier of the aggregate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A tuple containing the snapshot and its version, or null if no snapshot exists.</returns>
    ValueTask<(TAggregate? Snapshot, int Version)?> GetSnapshotAsync<TAggregate>(Guid aggregateId, CancellationToken cancellationToken = default)
        where TAggregate : class;

    /// <summary>
    /// Deletes snapshots for an aggregate older than the specified version.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate.</param>
    /// <param name="olderThanVersion">Delete snapshots older than this version.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ValueTask representing the completion of the operation.</returns>
    ValueTask DeleteOldSnapshotsAsync(Guid aggregateId, int olderThanVersion, CancellationToken cancellationToken = default);
}
