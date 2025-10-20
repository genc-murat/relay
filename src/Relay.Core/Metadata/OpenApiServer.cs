using System.Collections.Generic;

namespace Relay.Core;

/// <summary>
/// Represents a server in an OpenAPI document.
/// </summary>
public class OpenApiServer
{
    /// <summary>
    /// Gets or sets the URL of the server.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the server.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the variables for the server URL.
    /// </summary>
    public Dictionary<string, OpenApiServerVariable> Variables { get; set; } = new();
}
