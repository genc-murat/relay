using System;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Relay.Core.Observability;

namespace Relay.Core.Caching
{
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

    /// <summary>
    /// Attribute for configuring distributed caching.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DistributedCacheAttribute : Attribute
    {
        /// <summary>
        /// Absolute expiration in seconds.
        /// </summary>
        public int AbsoluteExpirationSeconds { get; set; } = 300; // 5 minutes default

        /// <summary>
        /// Sliding expiration in seconds.
        /// </summary>
        public int SlidingExpirationSeconds { get; set; } = 0; // No sliding expiration by default

        /// <summary>
        /// Cache key pattern (supports placeholders).
        /// </summary>
        public string KeyPattern { get; set; } = "{RequestType}:{RequestHash}";

        /// <summary>
        /// Cache regions for logical grouping.
        /// </summary>
        public string Region { get; set; } = "default";

        /// <summary>
        /// Whether to use cache for this request type.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Interface for generating cache keys.
    /// </summary>
    public interface ICacheKeyGenerator
    {
        string GenerateKey<TRequest>(TRequest request, DistributedCacheAttribute cacheAttribute);
    }

    /// <summary>
    /// Interface for cache serialization.
    /// </summary>
    public interface ICacheSerializer
    {
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] data);
    }

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

        private string GenerateRequestHash<TRequest>(TRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(hashBytes)[..8]; // Use first 8 characters
        }
    }

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
}