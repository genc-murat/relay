using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Analysis.Engines;

/// <summary>
/// Updates temporal patterns (hourly and daily)
/// </summary>
internal class TemporalPatternUpdater : IPatternUpdater
{
    private readonly ILogger<TemporalPatternUpdater> _logger;

    public TemporalPatternUpdater(ILogger<TemporalPatternUpdater> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public int UpdatePatterns(PredictionResult[] predictions, PatternAnalysisResult analysis)
    {
        try
        {
            var hourlyGroups = predictions.GroupBy(p => p.Timestamp.Hour);
            var dailyGroups = predictions.GroupBy(p => p.Timestamp.DayOfWeek);

            foreach (var hourGroup in hourlyGroups)
            {
                var hour = hourGroup.Key;
                var hourPredictions = hourGroup.ToArray();
                var successRate = hourPredictions.Count(p => p.ActualImprovement.TotalMilliseconds > 0) /
                                  (double)hourPredictions.Length;

                _logger.LogTrace("Hour {Hour}: Success rate = {SuccessRate:P} ({Count} predictions)",
                    hour, successRate, hourPredictions.Length);
            }

            foreach (var dayGroup in dailyGroups)
            {
                var day = dayGroup.Key;
                var dayPredictions = dayGroup.ToArray();
                var successRate = dayPredictions.Count(p => p.ActualImprovement.TotalMilliseconds > 0) /
                                  (double)dayPredictions.Length;

                _logger.LogTrace("Day {Day}: Success rate = {SuccessRate:P} ({Count} predictions)",
                    day, successRate, dayPredictions.Length);
            }

            return hourlyGroups.Count() + dailyGroups.Count();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating temporal patterns");
            return 0;
        }
    }
}