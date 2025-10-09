using System;

namespace Relay.Core.Workflows.Infrastructure;

/// <summary>
/// Database entity for storing workflow definitions.
/// </summary>
public class WorkflowDefinitionEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the workflow name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the workflow description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the workflow version.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether this is the active version.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the serialized workflow steps.
    /// </summary>
    public string StepsData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the definition was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the definition was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }
}
