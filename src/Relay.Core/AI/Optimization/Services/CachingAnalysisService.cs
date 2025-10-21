using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Models;
using System;
using System.Linq;

namespace Relay.Core.AI.Optimization.Services
{
    /// <summary>
    /// Service for analyzing caching patterns and generating caching recommendations
    /// </summary>
    internal class CachingAnalysisService
    {
        private readonly ILogger _logger;

        public CachingAnalysisService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public CachingRecommendation AnalyzeCachingPatterns(
            Type requestType,
            CachingAnalysisData analysisData,
            AccessPattern[] accessPatterns)
        {
            if (requestType == null) throw new ArgumentNullException(nameof(requestType));
            if (analysisData == null) throw new ArgumentNullException(nameof(analysisData));
            if (accessPatterns == null) throw new ArgumentNullException(nameof(accessPatterns));

            var shouldCache = ShouldEnableCaching(analysisData, accessPatterns);
            var recommendedTtl = CalculateRecommendedTtl(accessPatterns);
            var strategy = DetermineCacheStrategy(accessPatterns);
            var expectedHitRate = PredictHitRate(analysisData, accessPatterns);
            var cacheKey = GenerateCacheKey(requestType);
            var scope = DetermineCacheScope(accessPatterns);
            var confidence = CalculateConfidence(analysisData, accessPatterns);
            var memorySavings = EstimateMemorySavings(accessPatterns);
            var performanceGain = EstimatePerformanceGain(accessPatterns);
            var keyStrategy = DetermineKeyStrategy(accessPatterns);
            var keyProperties = GetKeyProperties(accessPatterns);
            var priority = DetermineCachePriority(accessPatterns);
            var useDistributed = ShouldUseDistributedCache(accessPatterns);

            var recommendation = new CachingRecommendation
            {
                ShouldCache = shouldCache,
                RecommendedTtl = recommendedTtl,
                Strategy = strategy,
                ExpectedHitRate = expectedHitRate,
                CacheKey = cacheKey,
                Scope = scope,
                ConfidenceScore = confidence,
                EstimatedMemorySavings = memorySavings,
                EstimatedPerformanceGain = performanceGain,
                PredictedHitRate = expectedHitRate,
                KeyStrategy = keyStrategy,
                KeyProperties = keyProperties,
                Priority = priority,
                UseDistributedCache = useDistributed
            };

            _logger.LogDebug("Generated caching recommendation for {RequestType}: ShouldCache={ShouldCache}, HitRate={HitRate:P}, TTL={Ttl}",
                requestType.Name, shouldCache, expectedHitRate, recommendedTtl);

            return recommendation;
        }

        private bool ShouldEnableCaching(CachingAnalysisData analysisData, AccessPattern[] accessPatterns)
        {
            // Enable caching if hit rate is above threshold or access frequency is high
            if (analysisData.CacheHitRate > 0.3)
                return true;

            var avgFrequency = accessPatterns.Average(p => p.AccessFrequency);
            if (avgFrequency > 1.0) // More than 1 request per second
                return true;

            // Enable if execution time is high and access count is significant
            var avgExecutionTime = accessPatterns.Average(p => p.ExecutionTime.TotalMilliseconds);
            var totalAccesses = accessPatterns.Sum(p => p.AccessCount);
            if (avgExecutionTime > 500 && totalAccesses > 10)
                return true;

            return false;
        }

        private TimeSpan CalculateRecommendedTtl(AccessPattern[] accessPatterns)
        {
            if (accessPatterns.Length == 0)
                return TimeSpan.FromMinutes(5);

            // Calculate based on data volatility and access patterns
            var avgVolatility = accessPatterns.Average(p => p.DataVolatility);
            var avgTimeBetweenAccesses = accessPatterns.Average(p => p.TimeSinceLastAccess.TotalMinutes);

            // Higher volatility = shorter TTL
            // Longer time between accesses = longer TTL
            var baseTtlMinutes = 5.0;
            baseTtlMinutes *= (1.0 - avgVolatility); // Reduce for volatile data
            baseTtlMinutes *= (1.0 + avgTimeBetweenAccesses / 60.0); // Increase for infrequent access

            return TimeSpan.FromMinutes(Math.Max(1, Math.Min(60, baseTtlMinutes)));
        }

        private CacheStrategy DetermineCacheStrategy(AccessPattern[] accessPatterns)
        {
            var avgFrequency = accessPatterns.Average(p => p.AccessFrequency);
            var hasTimePatterns = accessPatterns.Any(p => p.TimeOfDayPattern != default);

            if (hasTimePatterns)
                return CacheStrategy.Adaptive;

            if (avgFrequency > 10)
                return CacheStrategy.LRU; // High frequency, use LRU

            return CacheStrategy.TimeBasedExpiration;
        }

        private double PredictHitRate(CachingAnalysisData analysisData, AccessPattern[] accessPatterns)
        {
            if (analysisData.TotalAccesses == 0)
                return 0.0;

            // Use historical hit rate as base
            var baseHitRate = analysisData.CacheHitRate;

            // Adjust based on recent patterns
            var recentPatterns = accessPatterns.Where(p => p.Timestamp > DateTime.UtcNow.AddHours(-1)).ToArray();
            if (recentPatterns.Length > 0)
            {
                var recentHitRate = recentPatterns.Count(p => p.WasCacheHit) / (double)recentPatterns.Length;
                baseHitRate = (baseHitRate + recentHitRate) / 2.0;
            }

            return Math.Min(baseHitRate, 0.95);
        }

        private string GenerateCacheKey(Type requestType)
        {
            return $"{requestType.FullName}:{{request}}";
        }

        private CacheScope DetermineCacheScope(AccessPattern[] accessPatterns)
        {
            var hasUserContext = accessPatterns.Any(p => !string.IsNullOrEmpty(p.UserContext));
            var hasRegionalData = accessPatterns.Any(p => !string.IsNullOrEmpty(p.Region));

            if (hasUserContext)
                return CacheScope.User;

            if (hasRegionalData)
                return CacheScope.Regional;

            return CacheScope.Global;
        }

        private double CalculateConfidence(CachingAnalysisData analysisData, AccessPattern[] accessPatterns)
        {
            var baseConfidence = 0.5;

            // More data = higher confidence
            if (analysisData.TotalAccesses > 100)
                baseConfidence += 0.3;
            else if (analysisData.TotalAccesses > 10)
                baseConfidence += 0.1;

            // Consistent patterns = higher confidence
            var avgFrequency = accessPatterns.Average(p => p.AccessFrequency);
            var frequencyVariance = accessPatterns.Length > 1 ?
                accessPatterns.Select(p => Math.Pow(p.AccessFrequency - avgFrequency, 2)).Average() : 0;

            if (frequencyVariance < 1.0)
                baseConfidence += 0.1;

            return Math.Min(baseConfidence, 0.9);
        }

        private long EstimateMemorySavings(AccessPattern[] accessPatterns)
        {
            if (accessPatterns.Length == 0)
                return 0;

            var avgExecutionTime = accessPatterns.Average(p => p.ExecutionTime.TotalMilliseconds);
            var totalAccesses = accessPatterns.Sum(p => p.AccessCount);

            // Rough estimate: assume 50% of execution time is CPU-bound work that can be saved
            var savedTimePerRequest = avgExecutionTime * 0.5;
            var totalSavedTime = savedTimePerRequest * totalAccesses;

            // Convert to "memory equivalent" - arbitrary but reasonable estimate
            return (long)(totalSavedTime * 1024); // ~1KB per ms saved
        }

        private TimeSpan EstimatePerformanceGain(AccessPattern[] accessPatterns)
        {
            if (accessPatterns.Length == 0)
                return TimeSpan.Zero;

            var avgExecutionTime = accessPatterns.Average(p => p.ExecutionTime.TotalMilliseconds);
            var predictedHitRate = PredictHitRate(new CachingAnalysisData(), accessPatterns);

            var avgGain = avgExecutionTime * predictedHitRate * 0.8; // 80% of execution time saved on cache hits
            return TimeSpan.FromMilliseconds(avgGain);
        }

        private CacheKeyStrategy DetermineKeyStrategy(AccessPattern[] accessPatterns)
        {
            var hasComplexRequests = accessPatterns.Any(p => p.RequestType != null);
            var hasUserContext = accessPatterns.Any(p => !string.IsNullOrEmpty(p.UserContext));

            if (hasUserContext)
                return CacheKeyStrategy.SelectedProperties;

            if (hasComplexRequests)
                return CacheKeyStrategy.FullRequest;

            return CacheKeyStrategy.RequestTypeOnly;
        }

        private string[] GetKeyProperties(AccessPattern[] accessPatterns)
        {
            var properties = new System.Collections.Generic.HashSet<string>();

            foreach (var pattern in accessPatterns)
            {
                if (!string.IsNullOrEmpty(pattern.UserContext))
                    properties.Add("UserId");

                if (!string.IsNullOrEmpty(pattern.Region))
                    properties.Add("Region");
            }

            return properties.ToArray();
        }

        private CachePriority DetermineCachePriority(AccessPattern[] accessPatterns)
        {
            var avgFrequency = accessPatterns.Average(p => p.AccessFrequency);
            var avgExecutionTime = accessPatterns.Average(p => p.ExecutionTime.TotalMilliseconds);

            if (avgFrequency > 10 || avgExecutionTime > 1000)
                return CachePriority.High;

            if (avgFrequency > 1 || avgExecutionTime > 100)
                return CachePriority.Normal;

            return CachePriority.Low;
        }

        private bool ShouldUseDistributedCache(AccessPattern[] accessPatterns)
        {
            var hasRegionalData = accessPatterns.Any(p => !string.IsNullOrEmpty(p.Region));
            var totalAccesses = accessPatterns.Sum(p => p.AccessCount);

            // Use distributed cache for high volume or regional data
            return hasRegionalData || totalAccesses > 1000;
        }
    }
}