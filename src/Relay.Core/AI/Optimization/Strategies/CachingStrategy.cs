using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization.Strategies
{
    /// <summary>
    /// Strategy for determining optimal caching configurations.
    /// </summary>
    internal class CachingStrategy : IOptimizationStrategy
    {
        private readonly ILogger _logger;

        public string Name => "Caching";
        public int Priority => 80;

        public CachingStrategy(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool CanHandle(string operation) => operation == "OptimizeCaching";

        public async ValueTask<StrategyExecutionResult> ExecuteAsync(OptimizationContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (context.AccessPatterns == null || context.AccessPatterns.Length == 0)
                {
                    return new StrategyExecutionResult
                    {
                        Success = false,
                        StrategyName = Name,
                        ErrorMessage = "Access patterns are required for caching optimization",
                        ExecutionTime = DateTime.UtcNow - startTime
                    };
                }

                var cachingRecommendation = AnalyzeCachingOpportunity(context);

                _logger.LogDebug("Caching optimization completed for {RequestType}: {Strategy} (Confidence: {Confidence:P2})",
                    context.RequestType?.Name ?? "Unknown", cachingRecommendation.Strategy, cachingRecommendation.ConfidenceScore);

                return new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = Name,
                    Confidence = cachingRecommendation.ConfidenceScore,
                    Data = cachingRecommendation,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    Metadata = new()
                    {
                        ["request_type"] = context.RequestType?.Name ?? "Unknown",
                        ["access_pattern_count"] = context.AccessPatterns.Length,
                        ["analysis_time"] = DateTime.UtcNow - startTime
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in caching strategy");

                return new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = Name,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        private OptimizationRecommendation AnalyzeCachingOpportunity(OptimizationContext context)
        {
            var accessPatterns = context.AccessPatterns!;
            var totalRequests = accessPatterns.Length;
            var cacheableRequests = accessPatterns.Where(p => p.AccessCount > 1).Count(); // Consider cacheable if accessed more than once

            // Calculate cache hit ratio potential
            var cacheHitRatio = CalculatePotentialCacheHitRatio(accessPatterns, totalRequests);

            // Determine if caching is beneficial
            var shouldCache = cacheHitRatio > 0.3 || // 30% hit ratio threshold
                             (cacheableRequests > totalRequests * 0.5); // 50% of requests are cacheable

            if (!shouldCache)
            {
                return new OptimizationRecommendation
                {
                    Strategy = OptimizationStrategy.None,
                    ConfidenceScore = 0.8,
                    EstimatedImprovement = TimeSpan.Zero,
                    Reasoning = $"Low cache potential ({cacheHitRatio:P2} hit ratio, {cacheableRequests}/{totalRequests} cacheable)",
                    Priority = OptimizationPriority.Low,
                    EstimatedGainPercentage = 0.0,
                    Risk = RiskLevel.VeryLow
                };
            }

            // Calculate optimal cache TTL
            var optimalTtl = CalculateOptimalCacheTtl(accessPatterns);

            // Estimate performance improvement
            var estimatedImprovement = TimeSpan.FromMilliseconds(
                context.ExecutionMetrics?.AverageExecutionTime.TotalMilliseconds * cacheHitRatio * 0.9 ?? 50);

            return new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.Caching,
                ConfidenceScore = Math.Min(cacheHitRatio + 0.2, 0.95), // Add base confidence
                EstimatedImprovement = estimatedImprovement,
                Reasoning = $"High cache potential ({cacheHitRatio:P2} hit ratio) with {optimalTtl.TotalMinutes:F0}min TTL",
                Parameters = new()
                {
                    ["cache_ttl_seconds"] = (int)optimalTtl.TotalSeconds,
                    ["estimated_hit_ratio"] = cacheHitRatio,
                    ["cacheable_request_ratio"] = (double)cacheableRequests / totalRequests
                },
                Priority = cacheHitRatio > 0.7 ? OptimizationPriority.High : OptimizationPriority.Medium,
                EstimatedGainPercentage = cacheHitRatio * 0.8, // Conservative estimate
                Risk = RiskLevel.Low
            };
        }

        private double CalculatePotentialCacheHitRatio(AccessPattern[] patterns, int totalRequests)
        {
            if (patterns.Length == 0) return 0.0;

            // Simple heuristic: requests with high frequency and low variability are good cache candidates
            var weightedHitRatio = 0.0;
            var totalWeight = 0.0;

            foreach (var pattern in patterns)
            {
                if (pattern.AccessCount <= 1) continue; // Skip single-access patterns

                // Weight by access count
                var weight = pattern.AccessCount;
                totalWeight += weight;

                // Hit ratio based on access frequency and data stability
                var frequencyFactor = Math.Min(pattern.AccessFrequency / 10.0, 1.0); // Normalize to 10 req/sec
                var stabilityFactor = 1.0 - pattern.DataVolatility; // Lower volatility = higher hit ratio
                var hitRatio = Math.Min((frequencyFactor * 0.6 + stabilityFactor * 0.4) * 0.8 + 0.2, 0.95);

                weightedHitRatio += hitRatio * weight;
            }

            return totalWeight > 0 ? weightedHitRatio / totalWeight : 0.0;
        }

        private TimeSpan CalculateOptimalCacheTtl(AccessPattern[] patterns)
        {
            if (patterns.Length == 0) return TimeSpan.FromMinutes(5);

            // Analyze request frequency patterns to determine optimal TTL
            var avgFrequency = patterns
                .Where(p => p.AccessCount > 1)
                .Select(p => p.AccessFrequency)
                .DefaultIfEmpty(0.1) // Default 1 request per 10 seconds
                .Average();

            // TTL based on frequency: higher frequency = longer TTL
            var baseTtlSeconds = 300; // 5 minutes base
            if (avgFrequency > 1.0) // More than 1 req/sec
            {
                baseTtlSeconds = 1800; // 30 minutes
            }
            else if (avgFrequency > 0.1) // More than 1 req/10sec
            {
                baseTtlSeconds = 900; // 15 minutes
            }

            // Adjust for data volatility
            var avgVolatility = patterns.Average(p => p.DataVolatility);
            var volatilityFactor = 1.0 - avgVolatility; // Lower volatility = longer TTL
            var optimalSeconds = Math.Clamp(baseTtlSeconds * volatilityFactor, 60, 3600);

            return TimeSpan.FromSeconds(optimalSeconds);
        }
    }
}