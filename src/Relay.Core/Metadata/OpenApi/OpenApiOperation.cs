using System.Collections.Generic;

namespace Relay.Core.Metadata.OpenApi;

/// <summary>
/// Represents an operation in an OpenAPI document.
/// </summary>
public class OpenApiOperation
{
    /// <summary>
    /// Gets or sets the operation ID.
    /// </summary>
    public string? OperationId { get; set; }

    /// <summary>
    /// Gets or sets the summary of the operation.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the description of the operation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the tags for the operation.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the parameters for the operation.
    /// </summary>
    public List<OpenApiParameter> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the request body for the operation.
    /// </summary>
    public OpenApiRequestBody? RequestBody { get; set; }

    /// <summary>
    /// Gets or sets the responses for the operation.
    /// </summary>
    public Dictionary<string, OpenApiResponse> Responses { get; set; } = new();
}
