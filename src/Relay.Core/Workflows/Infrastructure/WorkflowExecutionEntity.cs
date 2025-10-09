using System;

namespace Relay.Core.Workflows.Infrastructure;

/// <summary>
/// Database entity for storing workflow execution state.
/// </summary>
public class WorkflowExecutionEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the execution.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the workflow definition identifier.
    /// </summary>
    public string WorkflowDefinitionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the workflow status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the workflow started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the workflow completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the serialized input data.
    /// </summary>
    public string? InputData { get; set; }

    /// <summary>
    /// Gets or sets the serialized output data.
    /// </summary>
    public string? OutputData { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the current step index.
    /// </summary>
    public int CurrentStepIndex { get; set; }

    /// <summary>
    /// Gets or sets the serialized workflow context.
    /// </summary>
    public string? ContextData { get; set; }

    /// <summary>
    /// Gets or sets the serialized step executions.
    /// </summary>
    public string? StepExecutionsData { get; set; }

    /// <summary>
    /// Gets or sets the version number (for optimistic concurrency).
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the timestamp for the last update.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
