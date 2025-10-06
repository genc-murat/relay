namespace Relay.Core.Caching;

/// <summary>
/// Interface for generating cache keys.
/// </summary>
public interface ICacheKeyGenerator
{
    string GenerateKey<TRequest>(TRequest request, DistributedCacheAttribute cacheAttribute);
}
