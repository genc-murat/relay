using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Pipeline.Behaviors.Strategies;

/// <summary>
/// Strategy for applying AI-powered caching optimizations.
/// </summary>
public class CachingOptimizationStrategy<TRequest, TResponse> : BaseOptimizationStrategy<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IMemoryCache? _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly IAIOptimizationEngine _aiEngine;
    private readonly AIOptimizationOptions _options;

    public CachingOptimizationStrategy(
        ILogger logger,
        IMemoryCache? memoryCache,
        IDistributedCache? distributedCache,
        IAIOptimizationEngine aiEngine,
        AIOptimizationOptions options,
        IMetricsProvider? metricsProvider = null)
        : base(logger, metricsProvider)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _aiEngine = aiEngine ?? throw new ArgumentNullException(nameof(aiEngine));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override OptimizationStrategy StrategyType => OptimizationStrategy.EnableCaching;

    public override async ValueTask<bool> CanApplyAsync(
        TRequest request,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken)
    {
        // Check if caching infrastructure is available
        if (_memoryCache == null && _distributedCache == null)
        {
            Logger.LogWarning("Caching optimization recommended but no cache provider available for {RequestType}", typeof(TRequest).Name);
            return false;
        }

        // Check confidence threshold
        if (!MeetsConfidenceThreshold(recommendation, _options.MinConfidenceScore))
            return false;

        // Get AI caching recommendation
        var accessPatterns = await GetAccessPatternsAsync(typeof(TRequest), cancellationToken);
        var cachingRecommendation = await _aiEngine.ShouldCacheAsync(typeof(TRequest), accessPatterns, cancellationToken);

        return cachingRecommendation.ShouldCache &&
               cachingRecommendation.PredictedHitRate >= _options.MinCacheHitRate;
    }

    public override async ValueTask<RequestHandlerDelegate<TResponse>> ApplyAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken)
    {
        // Get AI caching recommendation with access patterns
        var accessPatterns = await GetAccessPatternsAsync(typeof(TRequest), cancellationToken);
        var cachingRecommendation = await _aiEngine.ShouldCacheAsync(typeof(TRequest), accessPatterns, cancellationToken);

        Logger.LogDebug("Applying AI-powered caching for {RequestType} (Predicted HitRate: {HitRate:P}, TTL: {TTL}s)",
            typeof(TRequest).Name, cachingRecommendation.PredictedHitRate, cachingRecommendation.RecommendedTtl.TotalSeconds);

        // Generate cache key using AI-recommended strategy
        var cacheKey = GenerateSmartCacheKey(request, cachingRecommendation);

        // Wrap the handler with caching logic
        return async () =>
        {
            // Try memory cache first (L1)
            if (_memoryCache != null && _memoryCache.TryGetValue<TResponse>(cacheKey, out var memCachedResponse) && memCachedResponse != null)
            {
                Logger.LogDebug("AI cache hit (Memory L1) for {RequestType}: {CacheKey}", typeof(TRequest).Name, cacheKey);
                RecordCacheMetrics(typeof(TRequest), "Memory", hit: true);
                return memCachedResponse;
            }

            // Try distributed cache (L2)
            if (_distributedCache != null)
            {
                try
                {
                    var cachedBytes = await _distributedCache.GetAsync(cacheKey, cancellationToken);
                    if (cachedBytes != null && cachedBytes.Length > 0)
                    {
                        var distCachedResponse = DeserializeResponse(cachedBytes);
                        Logger.LogDebug("AI cache hit (Distributed L2) for {RequestType}: {CacheKey}", typeof(TRequest).Name, cacheKey);

                        // Promote to memory cache (cache warming)
                        if (_memoryCache != null)
                        {
                            var memOptions = new MemoryCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = cachingRecommendation.RecommendedTtl,
                                Size = EstimateResponseSize(distCachedResponse)
                            };
                            _memoryCache.Set(cacheKey, distCachedResponse, memOptions);
                        }

                        RecordCacheMetrics(typeof(TRequest), "Distributed", hit: true);
                        return distCachedResponse;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to retrieve from distributed cache for {RequestType}", typeof(TRequest).Name);
                }
            }

            Logger.LogDebug("AI cache miss for {RequestType}: {CacheKey}", typeof(TRequest).Name, cacheKey);
            RecordCacheMetrics(typeof(TRequest), "All", hit: false);

            // Execute handler
            var response = await next();

            // Store in cache with AI-recommended TTL and eviction policy
            await StoreToCacheAsync(cacheKey, response, cachingRecommendation, cancellationToken);

            return response;
        };
    }

    private async ValueTask<AccessPattern[]> GetAccessPatternsAsync(Type requestType, CancellationToken cancellationToken)
    {
        // Try to get access patterns from metrics provider
        if (MetricsProvider != null)
        {
            try
            {
                var stats = MetricsProvider.GetHandlerExecutionStats(requestType);
                if (stats != null && stats.TotalExecutions > 0)
                {
                    return new[]
                    {
                        new AccessPattern
                        {
                            RequestType = requestType,
                            AccessFrequency = CalculateExecutionFrequency(stats),
                            AverageExecutionTime = stats.AverageExecutionTime,
                            DataVolatility = CalculateDataVolatility(stats),
                            TimeOfDayPattern = TimeOfDayPattern.Uniform,
                            SampleSize = stats.TotalExecutions
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to retrieve access patterns for {RequestType}", requestType.Name);
            }
        }

        // Return default pattern
        await Task.CompletedTask;
        return new[]
        {
            new AccessPattern
            {
                RequestType = requestType,
                AccessFrequency = 1.0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                DataVolatility = 0.5,
                TimeOfDayPattern = TimeOfDayPattern.Uniform,
                SampleSize = 0
            }
        };
    }

    private double CalculateDataVolatility(Telemetry.HandlerExecutionStats stats)
    {
        // High failure rate or high execution time variance indicates volatile data
        var failureRate = stats.TotalExecutions > 0
            ? (double)stats.FailedExecutions / stats.TotalExecutions
            : 0.0;

        var executionTimeVariance = CalculateExecutionTimeVariance(stats);

        // Combine factors (0 = stable, 1 = highly volatile)
        return Math.Clamp(failureRate * 0.7 + executionTimeVariance * 0.3, 0.0, 1.0);
    }

    private double CalculateExecutionTimeVariance(Telemetry.HandlerExecutionStats stats)
    {
        // Simplified variance calculation - in practice this would use more sophisticated metrics
        return stats.AverageExecutionTime.TotalMilliseconds > 1000 ? 0.8 : 0.2;
    }

    private double CalculateExecutionFrequency(Telemetry.HandlerExecutionStats stats)
    {
        // Calculate frequency based on execution count and time period
        if (stats.TotalExecutions == 0)
            return 0.0;

        var timeSpan = DateTime.UtcNow - stats.LastExecution;
        return stats.TotalExecutions / Math.Max(timeSpan.TotalHours, 1.0);
    }

    private string GenerateSmartCacheKey(TRequest request, CachingRecommendation recommendation)
    {
        var requestType = typeof(TRequest).Name;

        // Use AI-recommended key strategy
        switch (recommendation.KeyStrategy)
        {
            case CacheKeyStrategy.FullRequest:
                return $"ai:cache:{requestType}:{GetRequestHash(request)}";

            case CacheKeyStrategy.RequestTypeOnly:
                return $"ai:cache:{requestType}";

            case CacheKeyStrategy.SelectedProperties:
                return $"ai:cache:{requestType}:{GetSelectedPropertiesHash(request, recommendation.KeyProperties)}";

            case CacheKeyStrategy.Custom:
            default:
                // For custom strategy, use the provided cache key if available, otherwise generate one
                if (!string.IsNullOrEmpty(recommendation.CacheKey))
                    return recommendation.CacheKey;
                return $"ai:cache:{requestType}:{GetRequestHash(request)}";
        }
    }

    private string GetRequestHash(TRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = false });
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(hashBytes)[..16]; // Use first 16 characters
        }
        catch
        {
            return request.GetHashCode().ToString();
        }
    }

    private string GetSelectedPropertiesHash(TRequest request, string[] properties)
    {
        if (properties == null || properties.Length == 0)
            return GetRequestHash(request);

        try
        {
            var values = new List<string>();
            var requestType = typeof(TRequest);

            foreach (var propName in properties)
            {
                var prop = requestType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    var value = prop.GetValue(request);
                    values.Add(value?.ToString() ?? "null");
                }
            }

            var combined = string.Join(":", values);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
            return Convert.ToBase64String(hashBytes)[..16];
        }
        catch
        {
            return GetRequestHash(request);
        }
    }

    private TResponse DeserializeResponse(byte[] cachedBytes)
    {
        try
        {
            return JsonSerializer.Deserialize<TResponse>(cachedBytes)!;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to deserialize cached response for {RequestType}", typeof(TRequest).Name);
            throw;
        }
    }

    private long EstimateResponseSize(TResponse response)
    {
        try
        {
            var json = JsonSerializer.Serialize(response);
            return json.Length;
        }
        catch
        {
            return 1024; // Default 1KB
        }
    }

    private async Task StoreToCacheAsync(string cacheKey, TResponse response, CachingRecommendation recommendation, CancellationToken cancellationToken)
    {
        try
        {
            // Store in memory cache (L1)
            if (_memoryCache != null)
            {
                var memOptions = new MemoryCacheEntryOptions();

                using (var entry = _memoryCache.CreateEntry(cacheKey))
                {
                    entry.Value = response;
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                }
                Logger.LogDebug("Stored in memory cache (L1): {CacheKey}, TTL: {TTL}s", cacheKey, recommendation.RecommendedTtl.TotalSeconds);
            }

            // Store in distributed cache (L2)
            if (_distributedCache != null && recommendation.UseDistributedCache)
            {
                var serialized = JsonSerializer.SerializeToUtf8Bytes(response);
                var distOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = recommendation.RecommendedTtl
                };

                await _distributedCache.SetAsync(cacheKey, serialized, distOptions, cancellationToken);
                Logger.LogDebug("Stored in distributed cache (L2): {CacheKey}, TTL: {TTL}s", cacheKey, recommendation.RecommendedTtl.TotalSeconds);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to store response in cache for {RequestType}", typeof(TRequest).Name);
        }
    }

    private void RecordCacheMetrics(Type requestType, string cacheType, bool hit)
    {
        // Record metrics for AI learning
        var properties = new Dictionary<string, object>
        {
            ["CacheType"] = cacheType,
            ["CacheHit"] = hit
        };

        RecordMetrics(requestType, TimeSpan.Zero, true, properties);
    }
}