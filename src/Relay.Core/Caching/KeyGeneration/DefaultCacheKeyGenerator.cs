using System;
using System.Text.Json;
using Relay.Core.Caching.Attributes;

namespace Relay.Core.Caching;

/// <summary>
/// Default cache key generator implementation.
/// </summary>
public class DefaultCacheKeyGenerator : ICacheKeyGenerator
{
    public string GenerateKey<TRequest>(TRequest request, DistributedCacheAttribute cacheAttribute)
    {
        var requestType = typeof(TRequest).Name;
        var requestHash = GenerateRequestHash(request);
        
        var key = cacheAttribute.KeyPattern
            .Replace("{RequestType}", requestType)
            .Replace("{RequestHash}", requestHash)
            .Replace("{Region}", cacheAttribute.Region);

        return key;
    }

    public string GenerateKey<TRequest>(TRequest request, EnhancedCacheAttribute cacheAttribute)
    {
        var requestType = typeof(TRequest).Name;
        var requestHash = GenerateRequestHash(request);
        
        var key = cacheAttribute.KeyPattern
            .Replace("{RequestType}", requestType)
            .Replace("{RequestHash}", requestHash)
            .Replace("{Region}", cacheAttribute.Region);

        return key;
    }

    private string GenerateRequestHash<TRequest>(TRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes)[..8]; // Use first 8 characters
    }
}
