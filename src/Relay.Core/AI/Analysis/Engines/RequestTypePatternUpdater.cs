using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Analysis.Engines
{
    /// <summary>
    /// Updates patterns based on request types
    /// </summary>
    internal class RequestTypePatternUpdater : IPatternUpdater
    {
        private readonly ILogger<RequestTypePatternUpdater> _logger;
        private readonly PatternRecognitionConfig _config;

        public RequestTypePatternUpdater(ILogger<RequestTypePatternUpdater> logger, PatternRecognitionConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public int UpdatePatterns(PredictionResult[] predictions, PatternAnalysisResult analysis)
        {
            try
            {
                var requestTypes = predictions.Select(p => p.RequestType).Distinct();
                var updatedCount = 0;

                foreach (var requestType in requestTypes)
                {
                    var typePredictions = predictions.Where(p => p.RequestType == requestType).ToArray();
                    var typeSuccesses = typePredictions.Count(p => p.ActualImprovement.TotalMilliseconds > 0);
                    var successRate = (double)typeSuccesses / typePredictions.Length;

                    var currentWeight = 1.0;
                    var newWeight = CalculateNewPatternWeight(currentWeight, successRate);

                    var avgImprovement = typePredictions
                        .Where(p => p.ActualImprovement.TotalMilliseconds > 0)
                        .Select(p => p.ActualImprovement.TotalMilliseconds)
                        .DefaultIfEmpty(0)
                        .Average();

                    _logger.LogDebug("Updated pattern for {RequestType}: Weight={Weight:F2}, " +
                        "Success={Success:P}, AvgImprovement={Improvement:F0}ms",
                        requestType.Name, newWeight, successRate, avgImprovement);

                    updatedCount++;
                }

                _logger.LogInformation("Updated patterns for {Count} request types", requestTypes.Count());
                return updatedCount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating request type patterns");
                return 0;
            }
        }

        private double CalculateNewPatternWeight(double currentWeight, double successRate)
        {
            return currentWeight * (1 - _config.WeightUpdateAlpha) + successRate * _config.WeightUpdateAlpha;
        }
    }
}