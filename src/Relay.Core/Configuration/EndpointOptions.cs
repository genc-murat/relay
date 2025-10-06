namespace Relay.Core.Configuration;

/// <summary>
/// Configuration options for endpoint generation.
/// </summary>
public class EndpointOptions
{
    /// <summary>
    /// Gets or sets the default HTTP method for endpoints.
    /// </summary>
    public string DefaultHttpMethod { get; set; } = "POST";

    /// <summary>
    /// Gets or sets the default route prefix for endpoints.
    /// </summary>
    public string? DefaultRoutePrefix { get; set; }

    /// <summary>
    /// Gets or sets the default API version for endpoints.
    /// </summary>
    public string? DefaultVersion { get; set; }

    /// <summary>
    /// Gets or sets whether to enable OpenAPI documentation generation.
    /// </summary>
    public bool EnableOpenApiGeneration { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable automatic route generation.
    /// </summary>
    public bool EnableAutoRouteGeneration { get; set; } = true;
}