namespace Relay.Core.Configuration.Resolved;

/// <summary>
/// Resolved configuration for an endpoint.
/// </summary>
public class ResolvedEndpointConfiguration
{
    /// <summary>
    /// Gets the route template for the endpoint.
    /// </summary>
    public string? Route { get; set; }

    /// <summary>
    /// Gets the HTTP method for the endpoint.
    /// </summary>
    public string HttpMethod { get; set; } = "POST";

    /// <summary>
    /// Gets the version of the endpoint.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets whether OpenAPI documentation generation is enabled.
    /// </summary>
    public bool EnableOpenApiGeneration { get; set; }

    /// <summary>
    /// Gets whether automatic route generation is enabled.
    /// </summary>
    public bool EnableAutoRouteGeneration { get; set; }
}
