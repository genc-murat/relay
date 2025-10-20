namespace Relay.Core;

/// <summary>
/// Represents contact information in an OpenAPI document.
/// </summary>
public class OpenApiContact
{
    /// <summary>
    /// Gets or sets the name of the contact.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the URL for the contact.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the email address for the contact.
    /// </summary>
    public string? Email { get; set; }
}
