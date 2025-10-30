namespace Relay.MessageBroker.Backpressure;

/// <summary>
/// Interface for backpressure management to handle situations when consumers cannot keep up with message production.
/// </summary>
public interface IBackpressureController
{
    /// <summary>
    /// Determines whether the system should throttle message consumption based on current conditions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if throttling should be applied, false otherwise.</returns>
    ValueTask<bool> ShouldThrottleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a message processing operation for backpressure monitoring.
    /// </summary>
    /// <param name="duration">The duration of the processing operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask RecordProcessingAsync(TimeSpan duration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current backpressure metrics.
    /// </summary>
    /// <returns>The current backpressure metrics.</returns>
    BackpressureMetrics GetMetrics();
}
