using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using System;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Caching;

/// <summary>
/// A pipeline behavior that implements caching for requests decorated with the <see cref="CacheAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class CachingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingPipelineBehavior<TRequest, TResponse>> _logger;

    public CachingPipelineBehavior(IMemoryCache cache, ILogger<CachingPipelineBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var cacheAttribute = typeof(TRequest).GetCustomAttribute<CacheAttribute>();

        if (cacheAttribute is null)
        {
            // Not a cacheable request, proceed to the next handler
            return await next();
        }

        var cacheKey = GenerateCacheKey(request);

        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse))
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            return cachedResponse!;
        }

        _logger.LogDebug("Cache miss for key: {CacheKey}. Executing handler.", cacheKey);
        var response = await next();

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheAttribute.AbsoluteExpirationSeconds)
        };

        _cache.Set(cacheKey, response, cacheOptions);
        _logger.LogInformation("Cached response for key: {CacheKey} with duration {Duration}s", cacheKey, cacheAttribute.AbsoluteExpirationSeconds);

        return response;
    }

    private static string GenerateCacheKey(TRequest request)
    {
        // Using JSON serialization for the key.
        // This can be slow for complex objects. For high-performance scenarios,
        // a more efficient key generation strategy might be needed,
        // e.g., implementing a specific interface on the request object.
        var serializedRequest = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = false });
        return $"{typeof(TRequest).FullName}:{serializedRequest}";
    }
}
