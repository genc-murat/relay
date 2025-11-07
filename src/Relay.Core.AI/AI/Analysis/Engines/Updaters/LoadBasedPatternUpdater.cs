using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Analysis.Engines;

/// <summary>
/// Updates load-based patterns
/// </summary>
internal class LoadBasedPatternUpdater : IPatternUpdater
{
    private readonly ILogger<LoadBasedPatternUpdater> _logger;
    private readonly PatternRecognitionConfig _config;

    public LoadBasedPatternUpdater(ILogger<LoadBasedPatternUpdater> logger, PatternRecognitionConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public int UpdatePatterns(PredictionResult[] predictions, PatternAnalysisResult analysis)
    {
        try
        {
            var loadGroups = predictions.GroupBy(p => ClassifyLoad(p.Metrics));
            var updatedCount = 0;

            foreach (var group in loadGroups)
            {
                var loadLevel = group.Key;
                var loadPredictions = group.ToArray();
                var successRate = loadPredictions.Count(p => p.ActualImprovement.TotalMilliseconds > 0) /
                                  (double)loadPredictions.Length;

                _logger.LogDebug("Load level {LoadLevel}: Success rate = {SuccessRate:P} ({Count} predictions)",
                    loadLevel, successRate, loadPredictions.Length);

                updatedCount++;
            }

            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating load-based patterns");
            return 0;
        }
    }

    private string ClassifyLoad(RequestExecutionMetrics metrics)
    {
        if (metrics.ConcurrentExecutions > _config.LoadThresholds.HighLoad)
            return "High";
        else if (metrics.ConcurrentExecutions > _config.LoadThresholds.MediumLoad)
            return "Medium";
        else
            return "Low";
    }
}