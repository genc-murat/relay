namespace Relay.Core.EventSourcing.Core;

/// <summary>
/// Interface for aggregates that support snapshotting.
/// </summary>
/// <typeparam name="TSnapshot">The type of the snapshot.</typeparam>
public interface ISnapshotable<TSnapshot> where TSnapshot : class
{
    /// <summary>
    /// Creates a snapshot of the current aggregate state.
    /// </summary>
    /// <returns>The snapshot of the aggregate.</returns>
    TSnapshot CreateSnapshot();

    /// <summary>
    /// Restores the aggregate state from a snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to restore from.</param>
    void RestoreFromSnapshot(TSnapshot snapshot);

    /// <summary>
    /// Gets the snapshot frequency (how many events before taking a snapshot).
    /// </summary>
    int SnapshotFrequency { get; }
}
