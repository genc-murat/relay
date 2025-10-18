using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Updates trend direction analysis for metrics
    /// </summary>
    internal sealed class TrendDirectionUpdater : ITrendDirectionUpdater
    {
        private readonly ILogger<TrendDirectionUpdater> _logger;

        public TrendDirectionUpdater(ILogger<TrendDirectionUpdater> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Dictionary<string, TrendDirection> UpdateTrendDirections(
            Dictionary<string, double> currentMetrics,
            Dictionary<string, MovingAverageData> movingAverages)
        {
            var result = new Dictionary<string, TrendDirection>();

            try
            {
                foreach (var metric in currentMetrics)
                {
                    if (!movingAverages.TryGetValue(metric.Key, out var ma)) continue;

                    var direction = TrendDirection.Stable;
                    var strength = 0.0;

                    var shortTermAboveLongTerm = ma.MA5 > ma.MA15;
                    var currentAboveShortTerm = metric.Value > ma.MA5;

                    if (currentAboveShortTerm && shortTermAboveLongTerm && ma.MA5 > ma.MA60)
                    {
                        direction = TrendDirection.StronglyIncreasing;
                        strength = CalculateTrendStrength(metric.Value, ma.MA5, ma.MA15);
                    }
                    else if (currentAboveShortTerm && shortTermAboveLongTerm)
                    {
                        direction = TrendDirection.Increasing;
                        strength = CalculateTrendStrength(metric.Value, ma.MA5, ma.MA15) * 0.7;
                    }
                    else if (!currentAboveShortTerm && !shortTermAboveLongTerm && ma.MA5 < ma.MA60)
                    {
                        direction = TrendDirection.StronglyDecreasing;
                        strength = CalculateTrendStrength(metric.Value, ma.MA5, ma.MA15);
                    }
                    else if (!currentAboveShortTerm && !shortTermAboveLongTerm)
                    {
                        direction = TrendDirection.Decreasing;
                        strength = CalculateTrendStrength(metric.Value, ma.MA5, ma.MA15) * 0.7;
                    }
                    else
                    {
                        direction = TrendDirection.Stable;
                        strength = 0.1;
                    }

                    result[metric.Key] = direction;

                    _logger.LogDebug("Trend for {Metric}: {Direction} (strength: {Strength:F2})",
                        metric.Key, direction, strength);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error detecting trend directions");
            }

            return result;
        }

        private double CalculateTrendStrength(double current, double ma5, double ma15)
        {
            if (ma15 == 0) return 0;
            return System.Math.Abs((ma5 - ma15) / ma15);
        }
    }
}