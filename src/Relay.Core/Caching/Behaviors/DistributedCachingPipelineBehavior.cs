using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Observability;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Caching;

/// <summary>
/// Advanced distributed caching pipeline behavior with smart cache strategies.
/// </summary>
public class DistributedCachingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<DistributedCachingPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ICacheSerializer _serializer;

    public DistributedCachingPipelineBehavior(
        IDistributedCache distributedCache,
        ILogger<DistributedCachingPipelineBehavior<TRequest, TResponse>> logger,
        ICacheKeyGenerator keyGenerator,
        ICacheSerializer serializer)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var cacheAttribute = GetCacheAttribute();
        if (cacheAttribute == null)
        {
            return await next();
        }

        var cacheKey = _keyGenerator.GenerateKey(request, cacheAttribute);
        var requestType = typeof(TRequest).Name;

        // Try to get from cache
        var cachedBytes = await _distributedCache.GetAsync(cacheKey, cancellationToken);
        if (cachedBytes != null)
        {
            try
            {
                var cachedResponse = _serializer.Deserialize<TResponse>(cachedBytes);
                _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                RelayMetrics.RecordCacheHit(cacheKey, requestType);
                return cachedResponse;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached response for key: {CacheKey}", cacheKey);
                // Continue to execute handler if deserialization fails
            }
        }

        _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
        RelayMetrics.RecordCacheMiss(cacheKey, requestType);

        var response = await next();

        // Cache the response
        try
        {
            var serializedResponse = _serializer.Serialize(response);
            var cacheOptions = new DistributedCacheEntryOptions();

            if (cacheAttribute.AbsoluteExpirationSeconds > 0)
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheAttribute.AbsoluteExpirationSeconds);
            }

            if (cacheAttribute.SlidingExpirationSeconds > 0)
            {
                cacheOptions.SlidingExpiration = TimeSpan.FromSeconds(cacheAttribute.SlidingExpirationSeconds);
            }

            await _distributedCache.SetAsync(cacheKey, serializedResponse, cacheOptions, cancellationToken);
            _logger.LogDebug("Cached response for key: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache response for key: {CacheKey}", cacheKey);
            // Don't throw - return the response even if caching fails
        }

        return response;
    }

    private DistributedCacheAttribute? GetCacheAttribute()
    {
        return typeof(TRequest).GetCustomAttribute<DistributedCacheAttribute>();
    }
}
