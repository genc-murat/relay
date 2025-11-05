using System.Text.Json;

namespace Relay.Core.Testing;

/// <summary>
/// Configuration options for snapshot testing.
/// </summary>
public class SnapshotOptions
{
    /// <summary>
    /// Gets or sets the directory where snapshots are stored.
    /// Defaults to "__snapshots__".
    /// </summary>
    public string SnapshotDirectory { get; set; } = "__snapshots__";

    /// <summary>
    /// Gets or sets a value indicating whether to update snapshots when they don't match.
    /// Defaults to false.
    /// </summary>
    public bool UpdateSnapshots { get; set; } = false;

    /// <summary>
    /// Gets or sets the serialization options.
    /// </summary>
    public SerializationOptions Serialization { get; set; } = new();
}

/// <summary>
/// Options for snapshot serialization.
/// </summary>
public class SerializationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to write indented JSON.
    /// Defaults to true.
    /// </summary>
    public bool WriteIndented { get; set; } = true;

    /// <summary>
    /// Gets or sets the property naming policy.
    /// Defaults to camelCase.
    /// </summary>
    public JsonNamingPolicy PropertyNamingPolicy { get; set; } = JsonNamingPolicy.CamelCase;

    /// <summary>
    /// Gets or sets a value indicating whether to ignore null values when writing.
    /// Defaults to true.
    /// </summary>
    public bool IgnoreNullValues { get; set; } = true;
}