using System.Collections.Generic;

namespace Relay.Core.Metadata.OpenApi;

/// <summary>
/// Represents a request body in an OpenAPI document.
/// </summary>
public class OpenApiRequestBody
{
    /// <summary>
    /// Gets or sets the description of the request body.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the request body is required.
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Gets or sets the content types for the request body.
    /// </summary>
    public Dictionary<string, OpenApiMediaType> Content { get; set; } = new();
}
