namespace Relay.Core.Metadata.OpenApi;

/// <summary>
/// Represents a parameter in an OpenAPI document.
/// </summary>
public class OpenApiParameter
{
    /// <summary>
    /// Gets or sets the name of the parameter.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the location of the parameter (query, header, path, cookie).
    /// </summary>
    public string In { get; set; } = "query";

    /// <summary>
    /// Gets or sets whether the parameter is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the description of the parameter.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the schema for the parameter.
    /// </summary>
    public OpenApiSchema? Schema { get; set; }
}
