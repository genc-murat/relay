using System.Collections.Generic;

namespace Relay.Core;

/// <summary>
/// Represents a schema in an OpenAPI document.
/// </summary>
public class OpenApiSchema
{
    /// <summary>
    /// Gets or sets the type of the schema.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the format of the schema.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets the title of the schema.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the description of the schema.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the properties of the schema (for object types).
    /// </summary>
    public Dictionary<string, OpenApiSchema> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets the required properties of the schema.
    /// </summary>
    public List<string> Required { get; set; } = new();

    /// <summary>
    /// Gets or sets the items schema (for array types).
    /// </summary>
    public OpenApiSchema? Items { get; set; }

    /// <summary>
    /// Gets or sets the enumerated values for the schema.
    /// </summary>
    public List<object> Enum { get; set; } = new();

    /// <summary>
    /// Gets or sets the reference to another schema.
    /// </summary>
    public string? Ref { get; set; }
}
