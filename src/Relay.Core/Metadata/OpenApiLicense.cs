namespace Relay.Core;

/// <summary>
/// Represents license information in an OpenAPI document.
/// </summary>
public class OpenApiLicense
{
    /// <summary>
    /// Gets or sets the name of the license.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL for the license.
    /// </summary>
    public string? Url { get; set; }
}
