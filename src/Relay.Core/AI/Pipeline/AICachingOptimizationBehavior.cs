using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.AI;

/// <summary>
/// Pipeline behavior for AI-driven caching optimization.
/// </summary>
internal class AICachingOptimizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<AICachingOptimizationBehavior<TRequest, TResponse>> _logger;
    private readonly IAIPredictionCache? _cache;
    private readonly AICachingOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, CacheMetrics> _metrics;

    public AICachingOptimizationBehavior(
        ILogger<AICachingOptimizationBehavior<TRequest, TResponse>> logger,
        IAIPredictionCache? cache = null,
        AICachingOptimizationOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache;
        _options = options ?? new AICachingOptimizationOptions();
        _metrics = new ConcurrentDictionary<Type, CacheMetrics>();
    }

    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_cache == null || !_options.EnableCaching || !IsCacheable(request))
        {
            return await next();
        }

        var requestType = typeof(TRequest);
        var metrics = _metrics.GetOrAdd(requestType, _ => new CacheMetrics());
        var cacheKey = GenerateCacheKey(request);

        // Track cache attempt
        metrics.IncrementAttempts();

        try
        {
            // Check cache for optimization recommendation
            var cachedRecommendation = await _cache.GetCachedPredictionAsync(cacheKey, cancellationToken);

            if (cachedRecommendation != null)
            {
                metrics.IncrementHits();

                _logger.LogDebug(
                    "Cache hit for {RequestType}: Strategy={Strategy}, Confidence={Confidence:F2}, " +
                    "EstimatedImprovement={Improvement}ms, HitRate={HitRate:P2}",
                    requestType.Name,
                    cachedRecommendation.Strategy,
                    cachedRecommendation.ConfidenceScore,
                    cachedRecommendation.EstimatedImprovement.TotalMilliseconds,
                    metrics.GetHitRate());

                // Apply cached optimization strategy if confidence is high enough
                if (cachedRecommendation.ConfidenceScore >= _options.MinConfidenceScore)
                {
                    _logger.LogTrace("Applying cached optimization: {Strategy}", cachedRecommendation.Strategy);
                    // In production, this would apply the cached optimization strategy
                }
            }
            else
            {
                metrics.IncrementMisses();
                _logger.LogTrace("Cache miss for {RequestType}: HitRate={HitRate:P2}",
                    requestType.Name, metrics.GetHitRate());
            }

            // Execute the request
            var startTime = DateTime.UtcNow;
            var response = await next();
            var executionTime = DateTime.UtcNow - startTime;

            // Cache the result for future use
            if (ShouldCacheResult(request, response, executionTime))
            {
                var ttl = CalculateDynamicTtl(request, executionTime);
                var recommendation = CreateRecommendation(request, response, executionTime);

                await _cache.SetCachedPredictionAsync(cacheKey, recommendation, ttl, cancellationToken);

                _logger.LogTrace(
                    "Cached optimization for {RequestType}: TTL={TTL}s, Strategy={Strategy}, Confidence={Confidence:F2}",
                    requestType.Name,
                    ttl.TotalSeconds,
                    recommendation.Strategy,
                    recommendation.ConfidenceScore);
            }

            return response;
        }
        catch (Exception ex)
        {
            metrics.IncrementErrors();
            _logger.LogError(ex, "Error in caching optimization behavior for {RequestType}", requestType.Name);

            // Fall back to direct execution
            return await next();
        }
    }

    private bool IsCacheable(TRequest request)
    {
        // Check for IntelligentCachingAttribute
        var attribute = typeof(TRequest).GetCustomAttribute<IntelligentCachingAttribute>();
        if (attribute != null)
        {
            if (!attribute.EnableAIAnalysis)
                return false;

            var metrics = _metrics.GetOrAdd(typeof(TRequest), _ => new CacheMetrics());

            // Check if access frequency meets minimum threshold
            if (metrics.GetAttemptCount() < attribute.MinAccessFrequency)
                return false;

            // Check if predicted hit rate meets threshold
            if (metrics.GetHitRate() < attribute.MinPredictedHitRate && metrics.GetAttemptCount() > 10)
                return false;
        }

        // Check options-based cacheability
        return _options.EnableCaching;
    }

    private bool ShouldCacheResult(TRequest request, TResponse response, TimeSpan executionTime)
    {
        if (response == null)
            return false;

        // Don't cache very fast operations
        if (executionTime.TotalMilliseconds < _options.MinExecutionTimeForCaching)
            return false;

        // Check if response size is reasonable (if applicable)
        if (response is IEstimateSize sizedResponse)
        {
            var estimatedSize = sizedResponse.EstimateSize();
            if (estimatedSize > _options.MaxCacheSizeBytes)
                return false;
        }

        return true;
    }

    private TimeSpan CalculateDynamicTtl(TRequest request, TimeSpan executionTime)
    {
        var attribute = typeof(TRequest).GetCustomAttribute<IntelligentCachingAttribute>();

        if (attribute?.UseDynamicTtl == true)
        {
            var metrics = _metrics.GetOrAdd(typeof(TRequest), _ => new CacheMetrics());
            var hitRate = metrics.GetHitRate();

            // Higher hit rate = longer TTL
            var baseTtl = _options.DefaultCacheTtl.TotalSeconds;
            var adjustedTtl = baseTtl * (1.0 + hitRate);

            // Clamp to reasonable bounds
            adjustedTtl = Math.Max(_options.MinCacheTtl.TotalSeconds, adjustedTtl);
            adjustedTtl = Math.Min(_options.MaxCacheTtl.TotalSeconds, adjustedTtl);

            return TimeSpan.FromSeconds(adjustedTtl);
        }

        return _options.DefaultCacheTtl;
    }

    private OptimizationRecommendation CreateRecommendation(TRequest request, TResponse response, TimeSpan executionTime)
    {
        var metrics = _metrics.GetOrAdd(typeof(TRequest), _ => new CacheMetrics());
        var hitRate = metrics.GetHitRate();

        // Calculate confidence based on cache hit rate and execution time
        var confidence = Math.Min(0.95, 0.5 + (hitRate * 0.3) + Math.Min(0.15, executionTime.TotalMilliseconds / 1000.0));

        // Estimate improvement based on typical cache retrieval time
        var estimatedImprovement = TimeSpan.FromMilliseconds(
            executionTime.TotalMilliseconds * 0.95); // Assume 95% improvement

        return new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = confidence,
            EstimatedImprovement = estimatedImprovement,
            Reasoning = $"Caching recommended: HitRate={hitRate:P2}, ExecutionTime={executionTime.TotalMilliseconds:F2}ms",
            Priority = hitRate > 0.5 ? OptimizationPriority.High : OptimizationPriority.Medium,
            Risk = RiskLevel.Low,
            EstimatedGainPercentage = Math.Min(0.95, hitRate * 1.5),
            Parameters = new System.Collections.Generic.Dictionary<string, object>
            {
                ["HitRate"] = hitRate,
                ["ExecutionTime"] = executionTime,
                ["CacheAttempts"] = metrics.GetAttemptCount()
            }
        };
    }

    private string GenerateCacheKey(TRequest request)
    {
        try
        {
            var attribute = typeof(TRequest).GetCustomAttribute<IntelligentCachingAttribute>();
            var keyPrefix = attribute?.PreferredScope switch
            {
                CacheScope.Global => "global",
                CacheScope.User => "user",
                CacheScope.Session => "session",
                CacheScope.Request => "request",
                _ => "global"
            };

            // Generate deterministic cache key from request
            var json = JsonSerializer.Serialize(request, _options.SerializerOptions);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            var hash = Convert.ToHexString(hashBytes);

            return $"{keyPrefix}:{typeof(TRequest).Name}:{hash[..16]}"; // Use first 16 chars
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate cache key for {RequestType}", typeof(TRequest).Name);
            return $"fallback:{typeof(TRequest).Name}:{Guid.NewGuid():N}";
        }
    }

    private class CacheMetrics
    {
        private long _attempts = 0;
        private long _hits = 0;
        private long _misses = 0;
        private long _errors = 0;

        public void IncrementAttempts() => Interlocked.Increment(ref _attempts);
        public void IncrementHits() => Interlocked.Increment(ref _hits);
        public void IncrementMisses() => Interlocked.Increment(ref _misses);
        public void IncrementErrors() => Interlocked.Increment(ref _errors);

        public long GetAttemptCount() => Interlocked.Read(ref _attempts);
        public long GetHitCount() => Interlocked.Read(ref _hits);
        public long GetMissCount() => Interlocked.Read(ref _misses);
        public long GetErrorCount() => Interlocked.Read(ref _errors);

        public double GetHitRate()
        {
            var attempts = GetAttemptCount();
            return attempts > 0 ? (double)GetHitCount() / attempts : 0.0;
        }
    }
}
