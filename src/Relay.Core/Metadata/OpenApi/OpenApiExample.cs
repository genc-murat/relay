namespace Relay.Core.Metadata.OpenApi;

/// <summary>
/// Represents an example in an OpenAPI document.
/// </summary>
public class OpenApiExample
{
    /// <summary>
    /// Gets or sets the summary of the example.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the description of the example.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the value of the example.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the external value reference for the example.
    /// </summary>
    public string? ExternalValue { get; set; }
}
