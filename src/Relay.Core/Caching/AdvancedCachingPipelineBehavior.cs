using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Configuration;

namespace Relay.Core.Caching
{
    /// <summary>
    /// A pipeline behavior that implements caching for requests with advanced configuration options.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public class AdvancedCachingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache? _distributedCache;
        private readonly ILogger<AdvancedCachingPipelineBehavior<TRequest, TResponse>> _logger;
        private readonly IOptions<RelayOptions> _options;
        private readonly string _handlerKey;

        public AdvancedCachingPipelineBehavior(
            IMemoryCache memoryCache,
            ILogger<AdvancedCachingPipelineBehavior<TRequest, TResponse>> logger,
            IOptions<RelayOptions> options,
            IDistributedCache? distributedCache = null)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _distributedCache = distributedCache;
            _handlerKey = typeof(TRequest).FullName ?? typeof(TRequest).Name;
        }

        public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Get caching configuration
            var cachingOptions = GetCachingOptions();
            var cacheAttribute = typeof(TRequest).GetCustomAttribute<CacheAttribute>();

            // Check if caching is enabled for this request
            if (!IsCachingEnabled(cachingOptions, cacheAttribute))
            {
                return await next();
            }

            // Generate cache key
            var cacheKey = GenerateCacheKey(request, cachingOptions.CacheKeyPrefix);

            // Try to get from cache
            if (await TryGetFromCacheAsync(cacheKey, cachingOptions, out TResponse? cachedResponse))
            {
                _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                return cachedResponse!;
            }

            _logger.LogDebug("Cache miss for key: {CacheKey}. Executing handler.", cacheKey);
            var response = await next();

            // Cache the response
            await CacheResponseAsync(cacheKey, response, cachingOptions, cacheAttribute, cancellationToken);

            return response;
        }

        private CachingOptions GetCachingOptions()
        {
            // Check for handler-specific overrides
            if (_options.Value.CachingOverrides.TryGetValue(_handlerKey, out var handlerOptions))
            {
                return handlerOptions;
            }

            // Return default options
            return _options.Value.DefaultCachingOptions;
        }

        private static bool IsCachingEnabled(CachingOptions cachingOptions, CacheAttribute? cacheAttribute)
        {
            // If caching is explicitly disabled globally, return false
            if (!cachingOptions.EnableAutomaticCaching && cacheAttribute == null)
            {
                return false;
            }

            // If caching is enabled globally or explicitly enabled with CacheAttribute, return true
            return cachingOptions.EnableAutomaticCaching || cacheAttribute != null;
        }

        private static string GenerateCacheKey(TRequest request, string cacheKeyPrefix)
        {
            // Using JSON serialization for the key.
            // This can be slow for complex objects. For high-performance scenarios,
            // a more efficient key generation strategy might be needed,
            // e.g., implementing a specific interface on the request object.
            var serializedRequest = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = false });
            return $"{cacheKeyPrefix}:{typeof(TRequest).FullName}:{serializedRequest}";
        }

        private async Task<bool> TryGetFromCacheAsync(string cacheKey, CachingOptions cachingOptions, out TResponse? cachedResponse)
        {
            cachedResponse = default;

            // Try memory cache first
            if (_memoryCache.TryGetValue(cacheKey, out cachedResponse))
            {
                return true;
            }

            // Try distributed cache if enabled
            if (cachingOptions.EnableDistributedCaching && _distributedCache != null)
            {
                try
                {
                    var cachedData = await _distributedCache.GetAsync(cacheKey);
                    if (cachedData != null && cachedData.Length > 0)
                    {
                        cachedResponse = JsonSerializer.Deserialize<TResponse>(cachedData);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get data from distributed cache for key: {CacheKey}", cacheKey);
                }
            }

            return false;
        }

        private async Task CacheResponseAsync(
            string cacheKey, 
            TResponse response, 
            CachingOptions cachingOptions, 
            CacheAttribute? cacheAttribute, 
            CancellationToken cancellationToken)
        {
            // Determine cache duration
            var cacheDuration = GetCacheDuration(cachingOptions, cacheAttribute);

            // Cache in memory
            var memoryCacheOptions = new MemoryCacheEntryOptions();
            if (cachingOptions.UseSlidingExpiration)
            {
                memoryCacheOptions.SlidingExpiration = TimeSpan.FromSeconds(cachingOptions.SlidingExpirationSeconds);
            }
            else
            {
                memoryCacheOptions.AbsoluteExpirationRelativeToNow = cacheDuration;
            }

            _memoryCache.Set(cacheKey, response, memoryCacheOptions);
            _logger.LogInformation("Cached response in memory for key: {CacheKey} with duration {Duration}s", cacheKey, cacheDuration.TotalSeconds);

            // Cache in distributed cache if enabled
            if (cachingOptions.EnableDistributedCaching && _distributedCache != null)
            {
                try
                {
                    var serializedResponse = JsonSerializer.SerializeToUtf8Bytes(response);
                    var distributedCacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = cacheDuration
                    };

                    await _distributedCache.SetAsync(cacheKey, serializedResponse, distributedCacheOptions, cancellationToken);
                    _logger.LogInformation("Cached response in distributed cache for key: {CacheKey} with duration {Duration}s", cacheKey, cacheDuration.TotalSeconds);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache data in distributed cache for key: {CacheKey}", cacheKey);
                }
            }
        }

        private static TimeSpan GetCacheDuration(CachingOptions cachingOptions, CacheAttribute? cacheAttribute)
        {
            if (cacheAttribute != null)
            {
                return TimeSpan.FromSeconds(cacheAttribute.AbsoluteExpirationSeconds);
            }

            return TimeSpan.FromSeconds(cachingOptions.DefaultCacheDurationSeconds);
        }
    }
}