using System.Collections.Generic;

namespace Relay.Core;

/// <summary>
/// Represents a response in an OpenAPI document.
/// </summary>
public class OpenApiResponse
{
    /// <summary>
    /// Gets or sets the description of the response.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content types for the response.
    /// </summary>
    public Dictionary<string, OpenApiMediaType> Content { get; set; } = new();

    /// <summary>
    /// Gets or sets the headers for the response.
    /// </summary>
    public Dictionary<string, OpenApiHeader> Headers { get; set; } = new();
}
