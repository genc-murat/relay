using System.Collections.Generic;

namespace Relay.Core.Metadata.OpenApi;

/// <summary>
/// Represents a media type in an OpenAPI document.
/// </summary>
public class OpenApiMediaType
{
    /// <summary>
    /// Gets or sets the schema for the media type.
    /// </summary>
    public OpenApiSchema? Schema { get; set; }

    /// <summary>
    /// Gets or sets examples for the media type.
    /// </summary>
    public Dictionary<string, OpenApiExample> Examples { get; set; } = new();
}
