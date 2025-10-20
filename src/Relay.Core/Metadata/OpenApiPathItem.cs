namespace Relay.Core;

/// <summary>
/// Represents a path item in an OpenAPI document.
/// </summary>
public class OpenApiPathItem
{
    /// <summary>
    /// Gets or sets the GET operation.
    /// </summary>
    public OpenApiOperation? Get { get; set; }

    /// <summary>
    /// Gets or sets the POST operation.
    /// </summary>
    public OpenApiOperation? Post { get; set; }

    /// <summary>
    /// Gets or sets the PUT operation.
    /// </summary>
    public OpenApiOperation? Put { get; set; }

    /// <summary>
    /// Gets or sets the DELETE operation.
    /// </summary>
    public OpenApiOperation? Delete { get; set; }

    /// <summary>
    /// Gets or sets the PATCH operation.
    /// </summary>
    public OpenApiOperation? Patch { get; set; }

    /// <summary>
    /// Gets or sets the HEAD operation.
    /// </summary>
    public OpenApiOperation? Head { get; set; }

    /// <summary>
    /// Gets or sets the OPTIONS operation.
    /// </summary>
    public OpenApiOperation? Options { get; set; }

    /// <summary>
    /// Gets or sets the TRACE operation.
    /// </summary>
    public OpenApiOperation? Trace { get; set; }
}
