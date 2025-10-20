using Relay.MessageBroker.Saga;

namespace Relay.MessageBroker.Saga.Persistence;

/// <summary>
/// Base entity for saga persistence in databases.
/// </summary>
public class SagaEntityBase
{
    /// <summary>
    /// Gets or sets the unique identifier for the saga instance.
    /// </summary>
    public Guid SagaId { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current state of the saga.
    /// </summary>
    public SagaState State { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the saga was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the saga was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the current step number.
    /// </summary>
    public int CurrentStep { get; set; }

    /// <summary>
    /// Gets or sets serialized metadata as JSON.
    /// </summary>
    public string MetadataJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets serialized saga data as JSON.
    /// </summary>
    public string DataJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the saga type name (for polymorphic queries).
    /// </summary>
    public string SagaType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last error message if saga failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the last error stack trace if saga failed.
    /// </summary>
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// Gets or sets the version for optimistic concurrency.
    /// </summary>
    public int Version { get; set; }
}
