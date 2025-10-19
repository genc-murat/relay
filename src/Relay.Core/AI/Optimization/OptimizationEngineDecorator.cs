using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Base decorator for optimization engines.
    /// </summary>
    public abstract class OptimizationEngineDecorator : OptimizationEngineTemplate
    {
        protected readonly OptimizationEngineTemplate _innerEngine;

        protected OptimizationEngineDecorator(
            OptimizationEngineTemplate innerEngine,
            ILogger logger,
            OptimizationStrategyFactory strategyFactory,
            IEnumerable<IPerformanceObserver> observers)
            : base(logger, strategyFactory, observers)
        {
            _innerEngine = innerEngine ?? throw new ArgumentNullException(nameof(innerEngine));
        }
    }

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

    /// <summary>
    /// Decorator that adds performance monitoring.
    /// </summary>
    public class MonitoringOptimizationEngineDecorator : OptimizationEngineDecorator
    {
        private readonly Dictionary<string, List<TimeSpan>> _performanceHistory = new();

        public MonitoringOptimizationEngineDecorator(
            OptimizationEngineTemplate innerEngine,
            ILogger logger,
            OptimizationStrategyFactory strategyFactory,
            IEnumerable<IPerformanceObserver> observers)
            : base(innerEngine, logger, strategyFactory, observers)
        {
        }

        public override async ValueTask<StrategyExecutionResult> OptimizeAsync(
            OptimizationContext context,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("Starting monitored optimization for operation: {Operation}", context.Operation);

            var result = await base.OptimizeAsync(context, cancellationToken);

            var totalTime = DateTime.UtcNow - startTime;

            // Record performance metrics
            RecordPerformanceMetrics(context.Operation, totalTime);

            _logger.LogInformation("Completed monitored optimization in {Time}ms (Success: {Success})",
                totalTime.TotalMilliseconds, result.Success);

            // Check for performance degradation
            if (IsPerformanceDegraded(context.Operation, totalTime))
            {
                _logger.LogWarning("Performance degradation detected for operation: {Operation}", context.Operation);

                // Notify observers
                var alert = new PerformanceAlert
                {
                    AlertType = "PerformanceDegradation",
                    Message = $"Performance degraded for operation {context.Operation}",
                    Severity = AlertSeverity.Medium,
                    Data = new { Operation = context.Operation, ExecutionTime = totalTime }
                };

                await NotifyPerformanceAlertAsync(alert, cancellationToken);
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

        private void RecordPerformanceMetrics(string operation, TimeSpan executionTime)
        {
            if (!_performanceHistory.ContainsKey(operation))
            {
                _performanceHistory[operation] = new List<TimeSpan>();
            }

            _performanceHistory[operation].Add(executionTime);

            // Keep only last 100 measurements
            if (_performanceHistory[operation].Count > 100)
            {
                _performanceHistory[operation].RemoveAt(0);
            }
        }

        private bool IsPerformanceDegraded(string operation, TimeSpan currentTime)
        {
            if (!_performanceHistory.ContainsKey(operation) || _performanceHistory[operation].Count < 5)
            {
                return false; // Not enough data
            }

            var recentTimes = _performanceHistory[operation].Skip(Math.Max(0, _performanceHistory[operation].Count - 10)).ToArray();
            var averageTime = recentTimes.Average(t => t.TotalMilliseconds);
            var currentMs = currentTime.TotalMilliseconds;

            // Consider degraded if current time is 50% worse than average
            return currentMs > averageTime * 1.5;
        }

        private async ValueTask NotifyPerformanceAlertAsync(PerformanceAlert alert, CancellationToken cancellationToken)
        {
            foreach (var observer in base._observers)
            {
                try
                {
                    await observer.OnPerformanceThresholdExceededAsync(alert);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error notifying observer of performance alert");
                }
            }
        }
    }

    /// <summary>
    /// Decorator that adds retry logic for failed optimizations.
    /// </summary>
    public class RetryOptimizationEngineDecorator : OptimizationEngineDecorator
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _retryDelay;

        public RetryOptimizationEngineDecorator(
            OptimizationEngineTemplate innerEngine,
            ILogger logger,
            OptimizationStrategyFactory strategyFactory,
            IEnumerable<IPerformanceObserver> observers,
            int maxRetries = 3,
            TimeSpan? retryDelay = null)
            : base(innerEngine, logger, strategyFactory, observers)
        {
            _maxRetries = maxRetries;
            _retryDelay = retryDelay ?? TimeSpan.FromSeconds(1);
        }

        public override async ValueTask<StrategyExecutionResult> OptimizeAsync(
            OptimizationContext context,
            CancellationToken cancellationToken = default)
        {
            var lastException = (Exception?)null;

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger.LogDebug("Retrying optimization (attempt {Attempt}/{MaxRetries})", attempt, _maxRetries);
                        await Task.Delay(_retryDelay, cancellationToken);
                    }

                    var result = await base.OptimizeAsync(context, cancellationToken);

                    if (result.Success)
                    {
                        return result;
                    }

                    // If we got here, the result was not successful
                    lastException = new InvalidOperationException(result.ErrorMessage);

                    if (attempt == _maxRetries)
                    {
                        // Exhausted retries, return custom error
                        break;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (attempt == _maxRetries)
                    {
                        _logger.LogError(ex, "Optimization failed after {Attempts} attempts", _maxRetries + 1);
                        break;
                    }

                    _logger.LogWarning(ex, "Optimization attempt {Attempt} failed, retrying", attempt + 1);
                }
            }

            return new StrategyExecutionResult
            {
                Success = false,
                StrategyName = "RetryDecorator",
                ErrorMessage = $"Failed after {_maxRetries + 1} attempts. Last error: {lastException?.Message}",
                ExecutionTime = TimeSpan.Zero
            };
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
    }
}