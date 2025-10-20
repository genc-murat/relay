namespace Relay.MessageBroker.Saga.Events;

/// <summary>
/// Event arguments for saga compensated event.
/// </summary>
public sealed class SagaCompensatedEventArgs
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
    /// Gets or sets whether compensation was successful.
    /// </summary>
    public bool CompensationSucceeded { get; init; }

    /// <summary>
    /// Gets or sets the number of steps compensated.
    /// </summary>
    public int StepsCompensated { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of compensation.
    /// </summary>
    public DateTimeOffset CompensatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the original failure exception.
    /// </summary>
    public Exception? OriginalException { get; init; }
}
