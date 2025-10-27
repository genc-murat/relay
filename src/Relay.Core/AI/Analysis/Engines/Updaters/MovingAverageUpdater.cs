using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Updates moving average calculations for metrics using:
    /// - Simple Moving Average (SMA) for 5, 15, and 60 period windows
    /// - Exponential Moving Average (EMA) with configurable alpha smoothing
    /// - Historical metric tracking for accurate calculations
    /// </summary>
    internal sealed class MovingAverageUpdater : IMovingAverageUpdater
    {
        private readonly ILogger<MovingAverageUpdater> _logger;
        private readonly TrendAnalysisConfig _config;

        // Cache for storing historical metric values
        // Stores up to 60 values per metric (to support MA60 calculation)
        private readonly Dictionary<string, List<double>> _metricHistory = new();

        // Cache for storing exponential moving averages (previous EMA values)
        private readonly Dictionary<string, double> _emaCache = new();

        public MovingAverageUpdater(ILogger<MovingAverageUpdater> logger, TrendAnalysisConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public Dictionary<string, MovingAverageData> UpdateMovingAverages(
            Dictionary<string, double> currentMetrics,
            DateTime timestamp)
        {
            var result = new Dictionary<string, MovingAverageData>();

            try
            {
                foreach (var metric in currentMetrics)
                {
                    try
                    {
                        // Initialize metric history if not present
                        if (!_metricHistory.ContainsKey(metric.Key))
                        {
                            _metricHistory[metric.Key] = new List<double>();
                            _emaCache[metric.Key] = metric.Value;
                        }

                        // Add current value to history
                        _metricHistory[metric.Key].Add(metric.Value);

                        // Keep history to reasonable size (max 60 for MA60 calculation)
                        const int maxHistorySize = 60;
                        if (_metricHistory[metric.Key].Count > maxHistorySize)
                        {
                            _metricHistory[metric.Key].RemoveAt(0);
                        }

                        // Calculate moving averages
                        var ma5 = CalculateMovingAverage(metric.Key, metric.Value, _config.MovingAveragePeriods[0]);
                        var ma15 = CalculateMovingAverage(metric.Key, metric.Value, _config.MovingAveragePeriods[1]);
                        var ma60 = CalculateMovingAverage(metric.Key, metric.Value, _config.MovingAveragePeriods[2]);
                        var ema = CalculateExponentialMovingAverage(metric.Key, metric.Value, _config.ExponentialMovingAverageAlpha);

                        result[metric.Key] = new MovingAverageData
                        {
                            MA5 = ma5,
                            MA15 = ma15,
                            MA60 = ma60,
                            EMA = ema,
                            CurrentValue = metric.Value,
                            Timestamp = timestamp
                        };

                        _logger.LogTrace("Moving averages for {Metric}: MA5={MA5:F3}, MA15={MA15:F3}, MA60={MA60:F3}, EMA={EMA:F3}",
                            metric.Key, ma5, ma15, ma60, ema);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error calculating moving averages for metric {Metric}", metric.Key);
                        // Still add to result with current value as fallback
                        result[metric.Key] = new MovingAverageData
                        {
                            MA5 = metric.Value,
                            MA15 = metric.Value,
                            MA60 = metric.Value,
                            EMA = metric.Value,
                            CurrentValue = metric.Value,
                            Timestamp = timestamp
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating moving averages");
            }

            return result;
        }

        /// <summary>
        /// Calculates Simple Moving Average (SMA) for a given period.
        /// SMA = (Sum of last N values) / N
        /// </summary>
        private double CalculateMovingAverage(string metricName, double currentValue, int period)
        {
            try
            {
                if (!_metricHistory.TryGetValue(metricName, out var history) || history.Count == 0)
                    return currentValue;

                // Use the available history up to the requested period
                var valuesToAverage = history.Skip(Math.Max(0, history.Count - period)).ToList();

                if (valuesToAverage.Count == 0)
                    return currentValue;

                // Calculate simple average
                return valuesToAverage.Average();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error calculating moving average for metric {Metric} with period {Period}",
                    metricName, period);
                return currentValue;
            }
        }

        /// <summary>
        /// Calculates Exponential Moving Average (EMA) using smoothing.
        /// EMA = (CurrentValue * Alpha) + (PreviousEMA * (1 - Alpha))
        ///
        /// Where Alpha is the smoothing factor:
        /// - Higher Alpha (closer to 1.0): More weight on recent values
        /// - Lower Alpha (closer to 0.0): More weight on historical average
        /// - Typical value: 0.3 for recent-biased smoothing
        /// </summary>
        private double CalculateExponentialMovingAverage(string metricName, double currentValue, double alpha)
        {
            try
            {
                // Validate alpha is in valid range [0, 1]
                var validAlpha = Math.Clamp(alpha, 0.0, 1.0);

                if (!_emaCache.TryGetValue(metricName, out var previousEma))
                {
                    previousEma = currentValue;
                }

                // Calculate new EMA using the standard formula
                var newEma = (currentValue * validAlpha) + (previousEma * (1.0 - validAlpha));

                // Store the new EMA for next calculation
                _emaCache[metricName] = newEma;

                // Handle edge cases
                if (double.IsNaN(newEma) || double.IsInfinity(newEma))
                {
                    _logger.LogDebug("Invalid EMA result for {Metric}: NaN or Infinity. Using current value.", metricName);
                    return currentValue;
                }

                return newEma;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error calculating exponential moving average for metric {Metric}", metricName);
                return currentValue;
            }
        }

        /// <summary>
        /// Clears all historical data to free memory (useful for long-running processes).
        /// </summary>
        internal void ClearHistory()
        {
            _metricHistory.Clear();
            _emaCache.Clear();
            _logger.LogDebug("Cleared metric history and EMA cache");
        }

        /// <summary>
        /// Gets the current history size for a metric (for testing/monitoring).
        /// </summary>
        internal int GetHistorySize(string metricName)
        {
            return _metricHistory.TryGetValue(metricName, out var history) ? history.Count : 0;
        }
    }
}