using System;
using System.Collections.Generic;

namespace Relay.Core.ContractValidation.SchemaDiscovery;

/// <summary>
/// Provides context information for schema resolution.
/// </summary>
public sealed class SchemaContext
{
    /// <summary>
    /// Gets the type for which a schema is being resolved.
    /// </summary>
    public Type RequestType { get; init; } = null!;

    /// <summary>
    /// Gets the schema version to resolve.
    /// </summary>
    public string? SchemaVersion { get; init; }

    /// <summary>
    /// Gets additional metadata for schema resolution.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a request schema (true) or response schema (false).
    /// </summary>
    public bool IsRequest { get; init; }
}
