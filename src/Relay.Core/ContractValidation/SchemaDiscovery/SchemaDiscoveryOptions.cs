using System;
using System.Collections.Generic;

namespace Relay.Core.ContractValidation.SchemaDiscovery;

/// <summary>
/// Configuration options for schema discovery.
/// </summary>
public sealed class SchemaDiscoveryOptions
{
    /// <summary>
    /// Gets or sets the directories to search for schema files.
    /// </summary>
    public List<string> SchemaDirectories { get; set; } = new();

    /// <summary>
    /// Gets or sets the naming convention for schema files.
    /// Supports placeholders: {TypeName}, {TypeNamespace}, {IsRequest}
    /// </summary>
    public string NamingConvention { get; set; } = "{TypeName}.schema.json";

    /// <summary>
    /// Gets or sets a value indicating whether to search for schemas in embedded resources.
    /// </summary>
    public bool EnableEmbeddedResources { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to watch the file system for schema changes.
    /// </summary>
    public bool EnableFileSystemWatcher { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to load schemas from HTTP endpoints.
    /// </summary>
    public bool EnableHttpSchemas { get; set; } = false;

    /// <summary>
    /// Gets or sets the HTTP endpoints to load schemas from.
    /// </summary>
    public List<string> HttpSchemaEndpoints { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout for HTTP schema requests.
    /// </summary>
    public TimeSpan HttpSchemaTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
