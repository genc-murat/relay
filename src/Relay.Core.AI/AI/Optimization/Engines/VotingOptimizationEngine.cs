using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization
{
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