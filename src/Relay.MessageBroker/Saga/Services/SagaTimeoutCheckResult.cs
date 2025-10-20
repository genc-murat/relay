namespace Relay.MessageBroker.Saga.Services;

/// <summary>
/// Result of saga timeout check operation.
/// </summary>
public readonly record struct SagaTimeoutCheckResult
{
    /// <summary>
    /// Gets the number of sagas checked.
    /// </summary>
    public int CheckedCount { get; init; }

    /// <summary>
    /// Gets the number of timed-out sagas found.
    /// </summary>
    public int TimedOutCount { get; init; }
}
