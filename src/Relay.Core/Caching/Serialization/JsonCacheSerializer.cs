using System.Text.Json;

namespace Relay.Core.Caching;

/// <summary>
/// JSON-based cache serializer.
/// </summary>
public class JsonCacheSerializer : ICacheSerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonCacheSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public byte[] Serialize<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj, _options);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    public T Deserialize<T>(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<T>(json, _options)!;
    }
}