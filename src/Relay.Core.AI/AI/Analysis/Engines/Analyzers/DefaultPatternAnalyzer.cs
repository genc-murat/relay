using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Analysis.Engines;

/// <summary>
/// Default implementation of pattern analyzer
/// </summary>
internal class DefaultPatternAnalyzer : IPatternAnalyzer
{
    private readonly ILogger<DefaultPatternAnalyzer> _logger;
    private readonly PatternRecognitionConfig _config;

    public DefaultPatternAnalyzer(ILogger<DefaultPatternAnalyzer> logger, PatternRecognitionConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public PatternAnalysisResult AnalyzePatterns(PredictionResult[] predictions)
    {
        var result = new PatternAnalysisResult
        {
            TotalPredictions = predictions.Length,
            AnalysisTimestamp = DateTime.UtcNow
        };

        try
        {
            result.SuccessfulPredictions = predictions
                .Where(p => p.ActualImprovement.TotalMilliseconds > 0)
                .ToArray();

            result.FailedPredictions = predictions
                .Where(p => p.ActualImprovement.TotalMilliseconds <= 0)
                .ToArray();

            result.OverallAccuracy = (double)result.SuccessfulPredictions.Length / predictions.Length;
            result.SuccessRate = result.OverallAccuracy;
            result.FailureRate = 1.0 - result.OverallAccuracy;

            result.HighImpactSuccesses = result.SuccessfulPredictions
                .Count(p => p.ActualImprovement.TotalMilliseconds > _config.ImprovementThresholds.HighImpact);

            result.MediumImpactSuccesses = result.SuccessfulPredictions
                .Count(p => p.ActualImprovement.TotalMilliseconds > _config.ImprovementThresholds.LowImpact &&
                           p.ActualImprovement.TotalMilliseconds <= _config.ImprovementThresholds.HighImpact);

            result.LowImpactSuccesses = result.SuccessfulPredictions
                .Count(p => p.ActualImprovement.TotalMilliseconds <= _config.ImprovementThresholds.LowImpact);

            if (result.SuccessfulPredictions.Length > 0)
            {
                result.AverageImprovement = result.SuccessfulPredictions
                    .Average(p => p.ActualImprovement.TotalMilliseconds);
            }

            result.BestRequestTypes = predictions
                .GroupBy(p => p.RequestType)
                .Select(g => new
                {
                    Type = g.Key,
                    SuccessRate = g.Count(p => p.ActualImprovement.TotalMilliseconds > 0) / (double)g.Count()
                })
                .OrderByDescending(x => x.SuccessRate)
                .Take(_config.TopRequestTypesCount)
                .Select(x => x.Type)
                .ToArray();

            result.WorstRequestTypes = predictions
                .GroupBy(p => p.RequestType)
                .Select(g => new
                {
                    Type = g.Key,
                    SuccessRate = g.Count(p => p.ActualImprovement.TotalMilliseconds > 0) / (double)g.Count()
                })
                .OrderBy(x => x.SuccessRate)
                .Take(_config.TopRequestTypesCount)
                .Select(x => x.Type)
                .ToArray();

            _logger.LogDebug("Pattern analysis: Success={Success:P}, High impact={High}, Medium={Medium}, Low={Low}",
                result.SuccessRate, result.HighImpactSuccesses, result.MediumImpactSuccesses, result.LowImpactSuccesses);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error analyzing prediction patterns");
            return result;
        }
    }
}