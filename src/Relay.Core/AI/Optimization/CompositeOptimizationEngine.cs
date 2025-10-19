using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Composite pattern implementation for combining multiple optimization engines.
    /// </summary>
    public class CompositeOptimizationEngine : OptimizationEngineTemplate
    {
        protected readonly List<OptimizationEngineTemplate> _engines;

        public CompositeOptimizationEngine(
            ILogger logger,
            OptimizationStrategyFactory strategyFactory,
            IEnumerable<IPerformanceObserver> observers,
            IEnumerable<OptimizationEngineTemplate> engines)
            : base(logger, strategyFactory, observers)
        {
            _engines = engines?.ToList() ?? new List<OptimizationEngineTemplate>();
        }

        public override async ValueTask<StrategyExecutionResult> OptimizeAsync(
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
                        StrategyName = "Composite",
                        ErrorMessage = "Invalid optimization context",
                        ExecutionTime = DateTime.UtcNow - startTime
                    };
                }

                // Execute engines directly (skip strategy selection for composite)
                var result = await ExecuteOptimizationAsync(context, Array.Empty<IOptimizationStrategy>(), cancellationToken);

                // Post-optimization processing
                await PostOptimizationAsync(result, context, cancellationToken);

                // Notify observers
                await NotifyObserversAsync(result, cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in composite optimization workflow");

                var errorResult = new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = "Composite",
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };

                await NotifyObserversAsync(errorResult, cancellationToken);
                return errorResult;
            }
        }

        protected override bool ValidateContext(OptimizationContext context)
        {
            // Validate with all engines - all must agree
            return _engines.All(engine => engine.GetType().GetMethod("ValidateContext")?
                .Invoke(engine, new object[] { context }) as bool? ?? true);
        }

        protected override IEnumerable<IOptimizationStrategy> SelectStrategies(OptimizationContext context)
        {
            // Combine strategies from all engines
            var allStrategies = new List<IOptimizationStrategy>();

            foreach (var engine in _engines)
            {
                var engineStrategies = engine.GetType().GetMethod("SelectStrategies")?
                    .Invoke(engine, new object[] { context }) as IEnumerable<IOptimizationStrategy>;

                if (engineStrategies != null)
                {
                    allStrategies.AddRange(engineStrategies);
                }
            }

            // Remove duplicates by name and priority (keep highest priority)
            return allStrategies
                .GroupBy(s => s.Name)
                .Select(g => g.OrderByDescending(s => s.Priority).First())
                .OrderByDescending(s => s.Priority);
        }

        protected override async ValueTask<StrategyExecutionResult> ExecuteOptimizationAsync(
            OptimizationContext context,
            IEnumerable<IOptimizationStrategy> strategies,
            CancellationToken cancellationToken)
        {
            // For composite engine, strategies parameter is ignored - we use engines instead
            if (_engines.Count == 0)
            {
                return new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = "Composite",
                    ErrorMessage = "No engines available",
                    ExecutionTime = TimeSpan.Zero
                };
            }

            // Execute strategies in parallel across engines
            var tasks = _engines.Select(engine =>
                ExecuteEngineOptimizationAsync(engine, context, cancellationToken).AsTask());

            var results = await Task.WhenAll(tasks);

            // Find the best successful result
            var successfulResults = results.Where(r => r.Success).ToList();

            if (successfulResults.Count == 0)
            {
                // Return the first failure result with combined error messages
                var errorMessages = results.Select(r => r.ErrorMessage).Where(msg => !string.IsNullOrEmpty(msg));
                return new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = "Composite",
                    ErrorMessage = $"All engines failed: {string.Join("; ", errorMessages)}",
                    ExecutionTime = results.Max(r => r.ExecutionTime)
                };
            }

            // Return the result with highest confidence
            var bestResult = successfulResults.OrderByDescending(r => r.Confidence).First();

            _logger.LogDebug("Composite engine selected best result: {Strategy} (Confidence: {Confidence:P2})",
                bestResult.StrategyName, bestResult.Confidence);

            return bestResult;
        }

        protected async ValueTask<StrategyExecutionResult> ExecuteEngineOptimizationAsync(
            OptimizationEngineTemplate engine,
            OptimizationContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                return await engine.OptimizeAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Engine {EngineType} failed during composite execution",
                    engine.GetType().Name);

                return new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = engine.GetType().Name,
                    ErrorMessage = ex.Message,
                    ExecutionTime = TimeSpan.Zero
                };
            }
        }

        /// <summary>
        /// Adds an engine to the composite.
        /// </summary>
        public void AddEngine(OptimizationEngineTemplate engine)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            _engines.Add(engine);
        }

        /// <summary>
        /// Removes an engine from the composite.
        /// </summary>
        public bool RemoveEngine(OptimizationEngineTemplate engine)
        {
            return _engines.Remove(engine);
        }

        /// <summary>
        /// Gets the number of engines in the composite.
        /// </summary>
        public int EngineCount => _engines.Count;
    }

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

    /// <summary>
    /// Specialized composite that uses voting to determine the best result.
    /// </summary>
    public class VotingOptimizationEngine : CompositeOptimizationEngine
    {
        public VotingOptimizationEngine(
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
            var results = new List<StrategyExecutionResult>();

            // Execute all engines
            foreach (var engine in base._engines)
            {
                var result = await ExecuteEngineOptimizationAsync(engine, context, cancellationToken);
                results.Add(result);
            }

            // Use voting to determine consensus
            return DetermineConsensusResult(results);
        }

        private StrategyExecutionResult DetermineConsensusResult(List<StrategyExecutionResult> results)
        {
            var successfulResults = results.Where(r => r.Success).ToList();

            if (successfulResults.Count == 0)
            {
                return new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = "Voting",
                    ErrorMessage = "No successful results from any engine",
                    ExecutionTime = results.Max(r => r.ExecutionTime)
                };
            }

            // Group by strategy name and find the most common recommendation
            var strategyVotes = successfulResults
                .GroupBy(r => r.StrategyName)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Average(r => r.Confidence))
                .ToList();

            var winningStrategy = strategyVotes.First();
            var consensusResult = winningStrategy.First(); // Take first result of winning strategy

            _logger.LogDebug("Voting consensus: {Strategy} (Votes: {Votes}/{Total})",
                consensusResult.StrategyName, winningStrategy.Count(), successfulResults.Count);

            return consensusResult;
        }
    }
}