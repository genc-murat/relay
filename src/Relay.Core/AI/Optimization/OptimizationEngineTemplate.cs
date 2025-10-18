using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Template for optimization engine workflows.
    /// </summary>
    public abstract class OptimizationEngineTemplate
    {
        protected readonly ILogger _logger;
        protected readonly OptimizationStrategyFactory _strategyFactory;
        protected readonly IEnumerable<IPerformanceObserver> _observers;

        protected OptimizationEngineTemplate(
            ILogger logger,
            OptimizationStrategyFactory strategyFactory,
            IEnumerable<IPerformanceObserver> observers)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
            _observers = observers ?? new List<IPerformanceObserver>();
        }

        /// <summary>
        /// Main optimization workflow.
        /// </summary>
        public virtual async ValueTask<StrategyExecutionResult> OptimizeAsync(
            OptimizationContext context,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Pre-optimization setup
                await PreOptimizationAsync(context, cancellationToken);

                // Validate context
                if (!ValidateContext(context))
                {
                    return new StrategyExecutionResult
                    {
                        Success = false,
                        StrategyName = "Template",
                        ErrorMessage = "Invalid optimization context",
                        ExecutionTime = DateTime.UtcNow - startTime
                    };
                }

                // Select appropriate strategies
                var strategies = SelectStrategies(context);

                // Execute optimization
                var result = await ExecuteOptimizationAsync(context, strategies, cancellationToken);

                // Post-optimization processing
                await PostOptimizationAsync(result, context, cancellationToken);

                // Notify observers
                await NotifyObserversAsync(result, cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in optimization workflow");

                var errorResult = new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = "Template",
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };

                // Notify observers of failure
                await NotifyObserversAsync(errorResult, cancellationToken);

                return errorResult;
            }
        }

        /// <summary>
        /// Pre-optimization setup (hook method).
        /// </summary>
        protected virtual ValueTask PreOptimizationAsync(
            OptimizationContext context,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting optimization for operation: {Operation}", context.Operation);
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Validates the optimization context.
        /// </summary>
        protected abstract bool ValidateContext(OptimizationContext context);

        /// <summary>
        /// Selects strategies for the given context.
        /// </summary>
        protected virtual IEnumerable<IOptimizationStrategy> SelectStrategies(OptimizationContext context)
        {
            // Default implementation: select all strategies that can handle the operation
            return _strategyFactory.CreateStrategiesForOperation(context.Operation);
        }

        /// <summary>
        /// Executes the optimization logic.
        /// </summary>
        protected abstract ValueTask<StrategyExecutionResult> ExecuteOptimizationAsync(
            OptimizationContext context,
            IEnumerable<IOptimizationStrategy> strategies,
            CancellationToken cancellationToken);

        /// <summary>
        /// Post-optimization processing (hook method).
        /// </summary>
        protected virtual ValueTask PostOptimizationAsync(
            StrategyExecutionResult result,
            OptimizationContext context,
            CancellationToken cancellationToken)
        {
            if (result.Success)
            {
                _logger.LogDebug("Optimization completed successfully: {Strategy}", result.StrategyName);
            }
            else
            {
                _logger.LogWarning("Optimization failed: {Strategy} - {Error}", result.StrategyName, result.ErrorMessage);
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Notifies performance observers.
        /// </summary>
        protected virtual async ValueTask NotifyObserversAsync(
            StrategyExecutionResult result,
            CancellationToken cancellationToken)
        {
            foreach (var observer in _observers)
            {
                try
                {
                    await observer.OnOptimizationCompletedAsync(result);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error notifying observer");
                }
            }
        }
    }
}