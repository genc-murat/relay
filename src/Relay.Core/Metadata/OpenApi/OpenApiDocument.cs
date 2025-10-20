using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Relay.Core.Metadata.OpenApi;

/// <summary>
/// Represents an OpenAPI document generated from endpoint metadata.
/// </summary>
public class OpenApiDocument
{
    /// <summary>
    /// Gets or sets the OpenAPI specification version.
    /// </summary>
    [JsonPropertyName("openapi")]
    public string OpenApi { get; set; } = "3.0.1";

    /// <summary>
    /// Gets or sets the API information.
    /// </summary>
    public OpenApiInfo Info { get; set; } = new();

    /// <summary>
    /// Gets or sets the servers for the API.
    /// </summary>
    public List<OpenApiServer> Servers { get; set; } = new();

    /// <summary>
    /// Gets or sets the paths for the API endpoints.
    /// </summary>
    public Dictionary<string, OpenApiPathItem> Paths { get; set; } = new();

    /// <summary>
    /// Gets or sets the components (schemas, responses, etc.).
    /// </summary>
    public OpenApiComponents Components { get; set; } = new();

    /// <summary>
    /// Gets or sets additional properties for the document.
    /// </summary>
    public Dictionary<string, object> Extensions { get; set; } = new();
}
