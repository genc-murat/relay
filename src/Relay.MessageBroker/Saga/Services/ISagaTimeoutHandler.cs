namespace Relay.MessageBroker.Saga.Services;

/// <summary>
/// Interface for saga timeout handlers.
/// Implement this for each saga type that supports timeout handling.
/// </summary>
public interface ISagaTimeoutHandler
{
    /// <summary>
    /// Checks for and handles timed-out sagas.
    /// </summary>
    /// <param name="defaultTimeout">Default timeout duration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing count of checked and timed-out sagas.</returns>
    ValueTask<SagaTimeoutCheckResult> CheckAndHandleTimeoutsAsync(
        TimeSpan defaultTimeout,
        CancellationToken cancellationToken = default);
}
