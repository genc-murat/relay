using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Analysis.Engines
{
    /// <summary>
    /// Updates patterns based on strategy effectiveness
    /// </summary>
    internal class StrategyEffectivenessPatternUpdater : IPatternUpdater
    {
        private readonly ILogger<StrategyEffectivenessPatternUpdater> _logger;

        public StrategyEffectivenessPatternUpdater(ILogger<StrategyEffectivenessPatternUpdater> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int UpdatePatterns(PredictionResult[] predictions, PatternAnalysisResult analysis)
        {
            try
            {
                var strategyGroups = predictions
                    .SelectMany(p => p.PredictedStrategies.Select(s => new { Strategy = s, Prediction = p }))
                    .GroupBy(x => x.Strategy);

                var updatedCount = 0;

                foreach (var group in strategyGroups)
                {
                    var strategy = group.Key;
                    var strategyPredictions = group.ToArray();
                    var successes = strategyPredictions.Count(x => x.Prediction.ActualImprovement.TotalMilliseconds > 0);
                    var successRate = (double)successes / strategyPredictions.Length;

                    var avgImprovement = strategyPredictions
                        .Where(x => x.Prediction.ActualImprovement.TotalMilliseconds > 0)
                        .Select(x => x.Prediction.ActualImprovement.TotalMilliseconds)
                        .DefaultIfEmpty(0)
                        .Average();

                    var effectivenessScore = successRate * (1 + Math.Log10(Math.Max(1, avgImprovement)));

                    _logger.LogDebug("Strategy {Strategy} effectiveness: Score={Score:F2}, " +
                        "Success={Success:P}, AvgImprovement={Improvement:F0}ms",
                        strategy, effectivenessScore, successRate, avgImprovement);

                    updatedCount++;
                }

                return updatedCount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating strategy effectiveness patterns");
                return 0;
            }
        }
    }
}