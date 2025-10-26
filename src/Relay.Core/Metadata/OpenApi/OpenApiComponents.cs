using System.Collections.Generic;

namespace Relay.Core.Metadata.OpenApi;

/// <summary>
/// Represents the components section of an OpenAPI document.
/// </summary>
public class OpenApiComponents
{
    /// <summary>
    /// Gets or sets the schemas in the components.
    /// </summary>
    public Dictionary<string, OpenApiSchema> Schemas { get; set; } = [];

    /// <summary>
    /// Gets or sets the responses in the components.
    /// </summary>
    public Dictionary<string, OpenApiResponse> Responses { get; set; } = [];

    /// <summary>
    /// Gets or sets the parameters in the components.
    /// </summary>
    public Dictionary<string, OpenApiParameter> Parameters { get; set; } = [];

    /// <summary>
    /// Gets or sets the examples in the components.
    /// </summary>
    public Dictionary<string, OpenApiExample> Examples { get; set; } = [];

    /// <summary>
    /// Gets or sets the request bodies in the components.
    /// </summary>
    public Dictionary<string, OpenApiRequestBody> RequestBodies { get; set; } = [];

    /// <summary>
    /// Gets or sets the headers in the components.
    /// </summary>
    public Dictionary<string, OpenApiHeader> Headers { get; set; } = [];
}