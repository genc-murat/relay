namespace Relay.Core.Caching;

/// <summary>
/// Interface for cache serialization.
/// </summary>
public interface ICacheSerializer
{
    byte[] Serialize<T>(T obj);
    T Deserialize<T>(byte[] data);
}
