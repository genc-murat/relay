using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Specialized composite for load-balanced optimization.
    /// </summary>
    public class LoadBalancedOptimizationEngine : CompositeOptimizationEngine
    {
        private int _currentEngineIndex = 0;

        public LoadBalancedOptimizationEngine(
            ILogger logger,
            OptimizationStrategyFactory strategyFactory,
            IEnumerable<IPerformanceObserver> observers,
            IEnumerable<OptimizationEngineTemplate> engines)
            : base(logger, strategyFactory, observers, engines)
        {
        }

        protected override async ValueTask<StrategyExecutionResult> ExecuteOptimizationAsync(
            OptimizationContext context,
            IEnumerable<IOptimizationStrategy> strategies,
            CancellationToken cancellationToken)
        {
            // Use round-robin load balancing
            var engineIndex = Interlocked.Increment(ref _currentEngineIndex) % base._engines.Count;
            var selectedEngine = base._engines[engineIndex];

            _logger.LogDebug("Load balancing: selected engine {Index} ({Type})",
                engineIndex, selectedEngine.GetType().Name);

            return await ExecuteEngineOptimizationAsync(selectedEngine, context, cancellationToken);
        }
    }
}