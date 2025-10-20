using System.Collections.Generic;

namespace Relay.Core;

/// <summary>
/// Represents a server variable in an OpenAPI document.
/// </summary>
public class OpenApiServerVariable
{
    /// <summary>
    /// Gets or sets the default value for the variable.
    /// </summary>
    public string Default { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the variable.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the enumerated values for the variable.
    /// </summary>
    public List<string> Enum { get; set; } = new();
}
