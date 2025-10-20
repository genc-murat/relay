namespace Relay.MessageBroker.Saga.Interfaces;

/// <summary>
/// Base interface for saga data.
/// </summary>
public interface ISagaData
{
    /// <summary>
    /// Gets or sets the unique identifier for the saga instance.
    /// </summary>
    Guid SagaId { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier.
    /// </summary>
    string CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the current state of the saga.
    /// </summary>
    SagaState State { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the saga was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the saga was last updated.
    /// </summary>
    DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the current step number.
    /// </summary>
    int CurrentStep { get; set; }

    /// <summary>
    /// Gets or sets custom metadata.
    /// </summary>
    Dictionary<string, object> Metadata { get; set; }
}

/// <summary>
/// Base class for saga data.
/// </summary>
public abstract class SagaDataBase : ISagaData
{
    /// <inheritdoc/>
    public Guid SagaId { get; set; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string CorrelationId { get; set; } = string.Empty;

    /// <inheritdoc/>
    public SagaState State { get; set; } = SagaState.NotStarted;

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    public int CurrentStep { get; set; }

    /// <inheritdoc/>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
