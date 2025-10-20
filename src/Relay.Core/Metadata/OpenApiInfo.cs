namespace Relay.Core;

/// <summary>
/// Represents API information in an OpenAPI document.
/// </summary>
public class OpenApiInfo
{
    /// <summary>
    /// Gets or sets the title of the API.
    /// </summary>
    public string Title { get; set; } = "Relay API";

    /// <summary>
    /// Gets or sets the description of the API.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the version of the API.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the contact information for the API.
    /// </summary>
    public OpenApiContact? Contact { get; set; }

    /// <summary>
    /// Gets or sets the license information for the API.
    /// </summary>
    public OpenApiLicense? License { get; set; }
}
