namespace Relay.MessageBroker.Saga;

/// <summary>
/// Event arguments for saga completed event.
/// </summary>
public sealed class SagaCompletedEventArgs
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
    /// Gets or sets the number of steps executed.
    /// </summary>
    public int StepsExecuted { get; init; }

    /// <summary>
    /// Gets or sets the total execution duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of completion.
    /// </summary>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}
