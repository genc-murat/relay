using System.Text.Json;

namespace Relay.Core.Testing;

/// <summary>
/// Defines the contract for snapshot serialization.
/// </summary>
public interface ISnapshotSerializer
{
    /// <summary>
    /// Serializes an object to a string representation.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <returns>The serialized string representation.</returns>
    string Serialize<T>(T value);

    /// <summary>
    /// Deserializes a string representation back to an object.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize.</typeparam>
    /// <param name="snapshot">The serialized string representation.</param>
    /// <returns>The deserialized object.</returns>
    T Deserialize<T>(string snapshot);
}

/// <summary>
/// JSON-based implementation of snapshot serialization.
/// </summary>
public class JsonSnapshotSerializer : ISnapshotSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSnapshotSerializer"/> class.
    /// </summary>
    /// <param name="options">The JSON serializer options.</param>
    public JsonSnapshotSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc/>
    public string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, _options);
    }

    /// <inheritdoc/>
    public T Deserialize<T>(string snapshot)
    {
        return JsonSerializer.Deserialize<T>(snapshot, _options);
    }
}