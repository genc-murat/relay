namespace Relay.Core.Configuration;

/// <summary>
/// Configuration options for authorization.
/// </summary>
public class AuthorizationOptions
{
    /// <summary>
    /// Gets or sets whether to enable automatic authorization for all requests.
    /// </summary>
    public bool EnableAutomaticAuthorization { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to throw an exception when authorization fails.
    /// </summary>
    public bool ThrowOnAuthorizationFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets the default order for authorization pipeline behaviors.
    /// </summary>
    public int DefaultOrder { get; set; } = -3000; // Run very early in the pipeline
}