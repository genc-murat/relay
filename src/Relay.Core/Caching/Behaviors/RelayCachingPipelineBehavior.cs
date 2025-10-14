using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Relay.Core.Caching.Attributes;
using Relay.Core.Caching.Invalidation;
using Relay.Core.Caching.Metrics;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Caching.Behaviors;

/// <summary>
/// Unified caching pipeline behavior that supports both memory and distributed caching
/// with comprehensive configuration options.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class UnifiedCachingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly ILogger<UnifiedCachingPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ICacheSerializer _serializer;
    private readonly ICacheMetrics? _metrics;
    private readonly ICacheInvalidator? _invalidator;
    private readonly ICacheKeyTracker? _keyTracker;

    public UnifiedCachingPipelineBehavior(
        IMemoryCache memoryCache,
        ILogger<UnifiedCachingPipelineBehavior<TRequest, TResponse>> logger,
        ICacheKeyGenerator keyGenerator,
        ICacheSerializer serializer,
        ICacheMetrics? metrics = null,
        ICacheInvalidator? invalidator = null,
        ICacheKeyTracker? keyTracker = null,
        IDistributedCache? distributedCache = null)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _metrics = metrics;
        _invalidator = invalidator;
        _keyTracker = keyTracker;
        _distributedCache = distributedCache;
    }

    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var cacheAttribute = GetCacheAttribute();
        if (cacheAttribute == null || !cacheAttribute.Enabled)
        {
            return await next();
        }

        var cacheKey = GenerateCacheKey(request, cacheAttribute);
        var requestType = typeof(TRequest).Name;

        // Try to get from cache
        var cacheResult = await TryGetFromCacheAsync(cacheKey, cacheAttribute, cancellationToken);
        if (cacheResult.Success)
        {
            _metrics?.RecordHit(cacheKey, requestType);
            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            return cacheResult.Response!;
        }

        _metrics?.RecordMiss(cacheKey, requestType);
        _logger.LogDebug("Cache miss for key: {CacheKey}. Executing handler.", cacheKey);

        var response = await next();

        // Cache the response
        await CacheResponseAsync(cacheKey, response, cacheAttribute, cancellationToken);

        return response;
    }

    private RelayCacheAttribute? GetCacheAttribute()
    {
        return typeof(TRequest).GetCustomAttribute<RelayCacheAttribute>();
    }

    private string GenerateCacheKey(TRequest request, RelayCacheAttribute cacheAttribute)
    {
        // For backward compatibility, we need to handle different key generation methods
        if (cacheAttribute.KeyPattern.Contains("{RequestHash}"))
        {
            return GenerateKeyWithHash(request, cacheAttribute);
        }
        else
        {
            return _keyGenerator.GenerateKey(request, cacheAttribute);
        }
    }

    private string GenerateKeyWithHash(TRequest request, RelayCacheAttribute cacheAttribute)
    {
        var requestHash = GenerateRequestHash(request);
        var keyPattern = cacheAttribute.KeyPattern
            .Replace("{RequestType}", typeof(TRequest).Name)
            .Replace("{RequestHash}", requestHash)
            .Replace("{Region}", cacheAttribute.Region);
        
        return keyPattern;
    }

    private string GenerateRequestHash(TRequest request)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes)[..8];
    }

    private async Task<(bool Success, TResponse? Response)> TryGetFromCacheAsync(string cacheKey, RelayCacheAttribute cacheAttribute, CancellationToken cancellationToken)
    {
        // Try memory cache first
        if (_memoryCache.TryGetValue(cacheKey, out TResponse? cachedResponse))
        {
            return (true, cachedResponse);
        }

        // Try distributed cache if enabled and available
        if (cacheAttribute.UseDistributedCache && _distributedCache != null)
        {
            try
            {
                var cachedData = await _distributedCache.GetAsync(cacheKey, cancellationToken);
                if (cachedData != null && cachedData.Length > 0)
                {
                    cachedResponse = _serializer.Deserialize<TResponse>(cachedData);
                    
                    // Store in memory cache for faster access
                    var memoryCacheOptions = CreateMemoryCacheOptions(cacheAttribute);
                    _memoryCache.Set(cacheKey, cachedResponse, memoryCacheOptions);
                    
                    return (true, cachedResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get data from distributed cache for key: {CacheKey}", cacheKey);
            }
        }

        return (false, default);
    }

    private async Task CacheResponseAsync(
        string cacheKey,
        TResponse response,
        RelayCacheAttribute cacheAttribute,
        CancellationToken cancellationToken)
    {
        try
        {
            var serializedResponse = _serializer.Serialize(response);
            
            // Record metrics
            if (cacheAttribute.EnableMetrics)
            {
                _metrics?.RecordSet(cacheKey, typeof(TRequest).Name, serializedResponse.Length);
            }

            // Track key for invalidation
            _keyTracker?.AddKey(cacheKey, cacheAttribute.Tags);

            // Cache in memory
            var memoryCacheOptions = CreateMemoryCacheOptions(cacheAttribute);
            _memoryCache.Set(cacheKey, response, memoryCacheOptions);

            // Cache in distributed cache if enabled and available
            if (cacheAttribute.UseDistributedCache && _distributedCache != null)
            {
                var distributedCacheOptions = CreateDistributedCacheOptions(cacheAttribute);
                await _distributedCache.SetAsync(cacheKey, serializedResponse, distributedCacheOptions, cancellationToken);
            }

            _logger.LogInformation("Cached response for key: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache response for key: {CacheKey}", cacheKey);
        }
    }

    private MemoryCacheEntryOptions CreateMemoryCacheOptions(RelayCacheAttribute cacheAttribute)
    {
        var options = new MemoryCacheEntryOptions();

        if (cacheAttribute.SlidingExpirationSeconds > 0)
        {
            options.SlidingExpiration = TimeSpan.FromSeconds(cacheAttribute.SlidingExpirationSeconds);
        }
        else
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheAttribute.AbsoluteExpirationSeconds);
        }

        // Set priority based on attribute
        options.Priority = cacheAttribute.Priority switch
        {
            CachePriority.Low => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Low,
            CachePriority.High => Microsoft.Extensions.Caching.Memory.CacheItemPriority.High,
            CachePriority.Never => Microsoft.Extensions.Caching.Memory.CacheItemPriority.NeverRemove,
            _ => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal
        };

        return options;
    }

    private DistributedCacheEntryOptions CreateDistributedCacheOptions(RelayCacheAttribute cacheAttribute)
    {
        var options = new DistributedCacheEntryOptions();

        if (cacheAttribute.SlidingExpirationSeconds > 0)
        {
            options.SlidingExpiration = TimeSpan.FromSeconds(cacheAttribute.SlidingExpirationSeconds);
        }

        if (cacheAttribute.AbsoluteExpirationSeconds > 0)
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheAttribute.AbsoluteExpirationSeconds);
        }

        return options;
    }
}