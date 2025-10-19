using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// The built optimization engine.
    /// </summary>
    public class OptimizationEngine : OptimizationEngineTemplate
    {
        private readonly IEnumerable<IOptimizationStrategy> _strategies;
        private readonly TimeSpan _defaultTimeout;

        public OptimizationEngine(
            ILogger logger,
            OptimizationStrategyFactory strategyFactory,
            IEnumerable<IOptimizationStrategy> strategies,
            IEnumerable<IPerformanceObserver> observers,
            TimeSpan defaultTimeout)
            : base(logger, strategyFactory, observers)
        {
            _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
            _defaultTimeout = defaultTimeout;
        }

        protected override bool ValidateContext(OptimizationContext context)
        {
            return !string.IsNullOrEmpty(context.Operation);
        }

        protected override IEnumerable<IOptimizationStrategy> SelectStrategies(OptimizationContext context)
        {
            // Use strategies that can handle the operation
            return _strategies.Where(s => s.CanHandle(context.Operation));
        }

        protected override async ValueTask<StrategyExecutionResult> ExecuteOptimizationAsync(
            OptimizationContext context,
            IEnumerable<IOptimizationStrategy> strategies,
            CancellationToken cancellationToken)
        {
            var strategyList = strategies.OrderByDescending(s => s.Priority).ToList();

            if (strategyList.Count == 0)
            {
                return new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = "Engine",
                    ErrorMessage = $"No strategies available for operation: {context.Operation}",
                    ExecutionTime = TimeSpan.Zero
                };
            }

            // Execute the highest priority strategy
            var primaryStrategy = strategyList[0];
            _logger.LogDebug("Executing primary strategy: {Strategy} for operation: {Operation}",
                primaryStrategy.Name, context.Operation);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_defaultTimeout);

            var result = await primaryStrategy.ExecuteAsync(context, cts.Token);

            // If primary strategy fails, try alternatives
            if (!result.Success && strategyList.Count > 1)
            {
                _logger.LogDebug("Primary strategy failed, trying alternatives");

                foreach (var alternative in strategyList.Skip(1))
                {
                    _logger.LogDebug("Trying alternative strategy: {Strategy}", alternative.Name);

                    var altResult = await alternative.ExecuteAsync(context, cts.Token);
                    if (altResult.Success)
                    {
                        return altResult;
                    }
                }
            }

            return result;
        }
    }
}