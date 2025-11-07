using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Analysis.Engines;

/// <summary>
/// Updates correlation patterns between strategies and request types
/// </summary>
internal class CorrelationPatternUpdater : IPatternUpdater
{
    private readonly ILogger<CorrelationPatternUpdater> _logger;
    private readonly PatternRecognitionConfig _config;

    public CorrelationPatternUpdater(ILogger<CorrelationPatternUpdater> logger, PatternRecognitionConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public int UpdatePatterns(PredictionResult[] predictions, PatternAnalysisResult analysis)
    {
        try
        {
            var strategyRequestTypeCorrelations = predictions
                .SelectMany(p => p.PredictedStrategies.Select(s => new
                {
                    Strategy = s,
                    p.RequestType,
                    Success = p.ActualImprovement.TotalMilliseconds > 0
                }))
                .GroupBy(x => new { x.Strategy, x.RequestType })
                .Select(g => new
                {
                    g.Key.Strategy,
                    g.Key.RequestType,
                    SuccessRate = g.Count(x => x.Success) / (double)g.Count(),
                    Count = g.Count()
                })
                .Where(x => x.SuccessRate > _config.MinimumCorrelationSuccessRate && x.Count >= _config.MinimumCorrelationCount)
                .ToArray();

            var updatedCount = 0;

            foreach (var correlation in strategyRequestTypeCorrelations)
            {
                _logger.LogDebug("Strong correlation: {Strategy} + {RequestType} = {SuccessRate:P} ({Count} cases)",
                    correlation.Strategy, correlation.RequestType.Name, correlation.SuccessRate, correlation.Count);

                updatedCount++;
            }

            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating correlation patterns");
            return 0;
        }
    }
}