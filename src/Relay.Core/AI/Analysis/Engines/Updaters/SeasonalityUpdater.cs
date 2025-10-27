using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Updates seasonality pattern analysis for metrics using:
    /// - Historical seasonal data tracking (by hour and day of week)
    /// - Statistical analysis of expected value ranges
    /// - Z-score based deviation detection from seasonal norms
    /// - Seasonal boundary validation with configurable tolerance
    /// </summary>
    internal sealed class SeasonalityUpdater : ISeasonalityUpdater
    {
        private readonly ILogger<SeasonalityUpdater> _logger;

        // Historical seasonality data: stores values by (hour, dayOfWeek) for pattern analysis
        // Maintains up to 100 observations per time slot to build statistical norms
        private readonly Dictionary<(int Hour, DayOfWeek DayOfWeek), List<double>> _seasonalHistory = new();

        // Cached seasonal statistics for quick lookups: (hour, dayOfWeek) -> (mean, stdDev, count)
        private readonly Dictionary<(int Hour, DayOfWeek DayOfWeek), (double Mean, double StdDev, int Count)> _seasonalStats = new();

        // Configuration constants for seasonal validation
        private const int MaxHistoryPerSlot = 100; // Limit historical data per time slot
        private const double DefaultStandardDeviations = 2.0; // Z-score threshold (2σ = 95% confidence)
        private const double MinValueBound = 0.5; // Minimum expected multiplier
        private const double MaxValueBound = 2.5; // Maximum expected multiplier

        public SeasonalityUpdater(ILogger<SeasonalityUpdater> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Dictionary<string, SeasonalityPattern> UpdateSeasonalityPatterns(
            Dictionary<string, double> currentMetrics,
            DateTime timestamp)
        {
            var result = new Dictionary<string, SeasonalityPattern>();

            try
            {
                var hour = timestamp.Hour;
                var dayOfWeek = timestamp.DayOfWeek;
                var timeSlot = (hour, dayOfWeek);

                foreach (var metric in currentMetrics)
                {
                    try
                    {
                        var pattern = new SeasonalityPattern();

                        // Determine hourly pattern
                        if (hour >= 9 && hour <= 17)
                        {
                            pattern.HourlyPattern = "BusinessHours";
                            pattern.ExpectedMultiplier = 1.5;
                        }
                        else if (hour >= 0 && hour <= 6)
                        {
                            pattern.HourlyPattern = "OffHours";
                            pattern.ExpectedMultiplier = 0.5;
                        }
                        else
                        {
                            pattern.HourlyPattern = "TransitionHours";
                            pattern.ExpectedMultiplier = 1.0;
                        }

                        // Determine daily pattern and adjust multiplier
                        if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                        {
                            pattern.DailyPattern = "Weekend";
                            pattern.ExpectedMultiplier *= 0.6;
                        }
                        else
                        {
                            pattern.DailyPattern = "Weekday";
                        }

                        // Track the metric value in seasonal history
                        TrackSeasonalValue(timeSlot, metric.Value);

                        // Validate against seasonal expectations
                        pattern.MatchesSeasonality = IsWithinSeasonalExpectation(metric.Value, pattern.ExpectedMultiplier, timeSlot);
                        result[metric.Key] = pattern;

                        if (!pattern.MatchesSeasonality)
                        {
                            _logger.LogDebug("Metric {Metric} deviates from seasonal pattern: {Pattern} (Expected: {Expected:F3}, Actual: {Actual:F3})",
                                metric.Key, pattern.HourlyPattern, pattern.ExpectedMultiplier, metric.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error analyzing seasonal pattern for metric {Metric}", metric.Key);
                        // Return a neutral pattern on error
                        result[metric.Key] = new SeasonalityPattern
                        {
                            HourlyPattern = "Unknown",
                            DailyPattern = "Unknown",
                            ExpectedMultiplier = 1.0,
                            MatchesSeasonality = false
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error identifying seasonality patterns");
            }

            return result;
        }

        /// <summary>
        /// Validates if a metric value is within expected seasonal bounds using multiple strategies:
        /// 1. Direct multiplier comparison with configurable bounds
        /// 2. Z-score based statistical analysis (when sufficient historical data exists)
        /// 3. Historical range validation (comparing against past observations)
        /// </summary>
        private bool IsWithinSeasonalExpectation(double value, double expectedMultiplier, (int Hour, DayOfWeek DayOfWeek) timeSlot)
        {
            try
            {
                // Strategy 1: Simple bounds check on expected multiplier
                // Validate that the value is reasonably close to expected multiplier
                if (value < MinValueBound || value > MaxValueBound)
                {
                    _logger.LogTrace("Value {Value:F3} is outside absolute bounds [{Min:F3}, {Max:F3}]",
                        value, MinValueBound, MaxValueBound);
                    return false;
                }

                // Clamp expected multiplier to valid range
                var clampedExpected = Math.Clamp(expectedMultiplier, MinValueBound, MaxValueBound);

                // Allow ±50% deviation from expected multiplier
                var lowerBound = clampedExpected * 0.5;
                var upperBound = clampedExpected * 1.5;

                if (value < lowerBound || value > upperBound)
                {
                    _logger.LogTrace("Value {Value:F3} deviates from expected multiplier {Expected:F3} (bounds: [{Lower:F3}, {Upper:F3}])",
                        value, clampedExpected, lowerBound, upperBound);
                    return false;
                }

                // Strategy 2: Use historical statistics for validation (if available)
                if (_seasonalStats.TryGetValue(timeSlot, out var stats) && stats.Count >= 3)
                {
                    return IsWithinHistoricalRange(value, stats);
                }

                // Strategy 3: Accept values with limited historical data
                // After initial observations, use historical mean and variance
                if (_seasonalHistory.TryGetValue(timeSlot, out var history) && history.Count >= 2)
                {
                    var mean = history.Average();
                    var variance = history.Count > 1
                        ? history.Sum(v => Math.Pow(v - mean, 2)) / history.Count
                        : 0.0;
                    var stdDev = Math.Sqrt(variance);

                    // Value should be within 2 standard deviations of historical mean
                    var zScore = stdDev > 0 ? Math.Abs(value - mean) / stdDev : 0.0;
                    if (zScore > DefaultStandardDeviations)
                    {
                        _logger.LogTrace("Value {Value:F3} has Z-score {ZScore:F3} exceeding threshold {Threshold}",
                            value, zScore, DefaultStandardDeviations);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error validating seasonal expectation for value {Value}", value);
                // Default to accepting the value if validation fails
                return true;
            }
        }

        /// <summary>
        /// Validates a value against historical statistics using Z-score analysis.
        /// </summary>
        private bool IsWithinHistoricalRange(double value, (double Mean, double StdDev, int Count) stats)
        {
            if (stats.StdDev == 0.0)
            {
                // No variation in historical data
                // Accept value if it's close to the mean (within 10%)
                return Math.Abs(value - stats.Mean) <= (stats.Mean * 0.1);
            }

            // Calculate Z-score: (value - mean) / stdDev
            var zScore = Math.Abs(value - stats.Mean) / stats.StdDev;

            // Accept values within 2 standard deviations (95% confidence level)
            var isWithinRange = zScore <= DefaultStandardDeviations;

            if (!isWithinRange)
            {
                _logger.LogTrace("Value {Value:F3} has Z-score {ZScore:F3} against historical mean {Mean:F3} (stdDev: {StdDev:F3})",
                    value, zScore, stats.Mean, stats.StdDev);
            }

            return isWithinRange;
        }

        /// <summary>
        /// Tracks a metric value in seasonal history for building statistical patterns.
        /// Maintains separate historical data for each (hour, dayOfWeek) combination.
        /// </summary>
        private void TrackSeasonalValue((int Hour, DayOfWeek DayOfWeek) timeSlot, double value)
        {
            try
            {
                // Initialize history list if not present
                if (!_seasonalHistory.ContainsKey(timeSlot))
                {
                    _seasonalHistory[timeSlot] = new List<double>();
                }

                // Add value to history
                _seasonalHistory[timeSlot].Add(value);

                // Maintain maximum history size
                if (_seasonalHistory[timeSlot].Count > MaxHistoryPerSlot)
                {
                    _seasonalHistory[timeSlot].RemoveAt(0);
                }

                // Update cached statistics if we have enough data
                if (_seasonalHistory[timeSlot].Count >= 3)
                {
                    UpdateSeasonalStatistics(timeSlot);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error tracking seasonal value for time slot {Hour}:{DayOfWeek}", timeSlot.Hour, timeSlot.DayOfWeek);
            }
        }

        /// <summary>
        /// Calculates and caches statistical metrics (mean, standard deviation) for a time slot.
        /// </summary>
        private void UpdateSeasonalStatistics((int Hour, DayOfWeek DayOfWeek) timeSlot)
        {
            try
            {
                if (!_seasonalHistory.TryGetValue(timeSlot, out var history) || history.Count < 2)
                    return;

                var mean = history.Average();
                var variance = history.Sum(v => Math.Pow(v - mean, 2)) / history.Count;
                var stdDev = Math.Sqrt(variance);

                _seasonalStats[timeSlot] = (mean, stdDev, history.Count);

                _logger.LogTrace("Updated seasonal statistics for {Hour}:{DayOfWeek}: Mean={Mean:F3}, StdDev={StdDev:F3}, Count={Count}",
                    timeSlot.Hour, timeSlot.DayOfWeek, mean, stdDev, history.Count);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error updating seasonal statistics for time slot {Hour}:{DayOfWeek}", timeSlot.Hour, timeSlot.DayOfWeek);
            }
        }

        /// <summary>
        /// Clears all historical seasonality data (useful for memory management).
        /// </summary>
        internal void ClearHistory()
        {
            _seasonalHistory.Clear();
            _seasonalStats.Clear();
            _logger.LogDebug("Cleared seasonality history and statistics");
        }

        /// <summary>
        /// Gets the number of observations for a specific time slot (for testing/monitoring).
        /// </summary>
        internal int GetHistorySize((int Hour, DayOfWeek DayOfWeek) timeSlot)
        {
            return _seasonalHistory.TryGetValue(timeSlot, out var history) ? history.Count : 0;
        }

        /// <summary>
        /// Gets statistical information for a time slot (for testing/monitoring).
        /// </summary>
        internal (double Mean, double StdDev, int Count)? GetSeasonalStatistics((int Hour, DayOfWeek DayOfWeek) timeSlot)
        {
            return _seasonalStats.TryGetValue(timeSlot, out var stats) ? stats : null;
        }
    }
}