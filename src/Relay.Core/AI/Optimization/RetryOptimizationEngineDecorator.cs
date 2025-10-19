using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization
{
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