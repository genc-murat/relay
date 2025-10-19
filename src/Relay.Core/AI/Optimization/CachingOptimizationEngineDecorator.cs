using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Decorator that adds caching to optimization results.
    /// </summary>
    public class CachingOptimizationEngineDecorator : OptimizationEngineDecorator
    {
        private readonly Dictionary<string, (StrategyExecutionResult Result, DateTime Timestamp)> _cache = new();
        private readonly TimeSpan _cacheDuration;

        public CachingOptimizationEngineDecorator(
            OptimizationEngineTemplate innerEngine,
            ILogger logger,
            OptimizationStrategyFactory strategyFactory,
            IEnumerable<IPerformanceObserver> observers,
            TimeSpan cacheDuration)
            : base(innerEngine, logger, strategyFactory, observers)
        {
            _cacheDuration = cacheDuration;
        }

        public override async ValueTask<StrategyExecutionResult> OptimizeAsync(
            OptimizationContext context,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = GenerateCacheKey(context);

            // Check cache first
            if (_cache.TryGetValue(cacheKey, out var cachedEntry))
            {
                if (IsCacheValid(cachedEntry.Timestamp))
                {
                    _logger.LogDebug("Returning cached optimization result for key: {Key}", cacheKey);
                    return cachedEntry.Result;
                }
                else
                {
                    // Remove expired cache entry
                    _cache.Remove(cacheKey);
                }
            }

            // Execute optimization
            var result = await base.OptimizeAsync(context, cancellationToken);

            // Cache successful results
            if (result.Success)
            {
                _cache[cacheKey] = (result, DateTime.UtcNow);
                _logger.LogDebug("Cached optimization result for key: {Key}", cacheKey);
            }

            return result;
        }

        protected override async ValueTask<StrategyExecutionResult> ExecuteOptimizationAsync(
            OptimizationContext context,
            IEnumerable<IOptimizationStrategy> strategies,
            CancellationToken cancellationToken)
        {
            return await _innerEngine.OptimizeAsync(context, cancellationToken);
        }

        protected override bool ValidateContext(OptimizationContext context)
        {
            return _innerEngine.GetType().GetMethod("ValidateContext")?
                .Invoke(_innerEngine, new object[] { context }) as bool? ?? true;
        }

        private string GenerateCacheKey(OptimizationContext context)
        {
            // Create a cache key based on relevant context properties
            var keyParts = new List<string>
            {
                context.Operation,
                context.RequestType?.FullName ?? "null",
                context.Request?.GetHashCode().ToString() ?? "null"
            };

            return string.Join("|", keyParts);
        }

        private bool IsCacheValid(DateTime cacheTimestamp)
        {
            return (DateTime.UtcNow - cacheTimestamp) < _cacheDuration;
        }
    }
}