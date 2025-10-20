namespace Relay.Core.Metadata.OpenApi;

/// <summary>
/// Represents a header in an OpenAPI document.
/// </summary>
public class OpenApiHeader
{
    /// <summary>
    /// Gets or sets the description of the header.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the header is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the schema for the header.
    /// </summary>
    public OpenApiSchema? Schema { get; set; }
}
