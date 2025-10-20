namespace Relay.MessageBroker.Saga.Events;

/// <summary>
/// Event arguments for saga failed event.
/// </summary>
public sealed class SagaFailedEventArgs
{
    /// <summary>
    /// Gets or sets the saga ID.
    /// </summary>
    public Guid SagaId { get; init; }

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the step that failed.
    /// </summary>
    public string FailedStep { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception that occurred.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of failure.
    /// </summary>
    public DateTimeOffset FailedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the number of steps executed before failure.
    /// </summary>
    public int StepsExecutedBeforeFailure { get; init; }
}
