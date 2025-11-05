using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI;

/// <summary>
/// Updates trend velocity calculations for metrics using:
/// - Time-series rate of change analysis (units per minute)
/// - Multiple velocity calculation methods (simple, weighted, acceleration)
/// - Historical data tracking for accurate velocity computation
/// - Temporal analysis with configurable observation windows
/// </summary>
internal sealed class TrendVelocityUpdater : ITrendVelocityUpdater
{
    private readonly ILogger<TrendVelocityUpdater> _logger;
    private readonly TrendAnalysisConfig _config;

    // Historical velocity data: stores (timestamp, value) tuples for trend analysis
    // Maintains up to 60 observations per metric to calculate accurate velocities
    private readonly Dictionary<string, List<(DateTime Timestamp, double Value)>> _metricHistory = new();

    // Cached previous values for quick velocity calculation
    private readonly Dictionary<string, (DateTime LastTimestamp, double LastValue)> _previousValues = new();

    // Configuration constants for velocity analysis
    private const int MaxHistorySize = 60; // Limit historical data per metric
    private const double MinimumTimeWindowSeconds = 1.0; // Minimum time between observations

    public TrendVelocityUpdater(ILogger<TrendVelocityUpdater> logger, TrendAnalysisConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public Dictionary<string, double> UpdateTrendVelocities(
        Dictionary<string, double> currentMetrics,
        DateTime timestamp)
    {
        if (currentMetrics == null)
        {
            throw new ArgumentNullException(nameof(currentMetrics));
        }

        var result = new Dictionary<string, double>();

        try
        {
            foreach (var metric in currentMetrics)
            {
                try
                {
                    var velocity = CalculateMetricVelocity(metric.Key, metric.Value, timestamp);
                    result[metric.Key] = velocity;

                    // Track the new value in history
                    TrackMetricValue(metric.Key, timestamp, metric.Value);

                    if (Math.Abs(velocity) > _config.HighVelocityThreshold)
                    {
                        _logger.LogDebug("High velocity detected for {Metric}: {Velocity:F4}/min",
                            metric.Key, velocity);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error calculating velocity for metric {Metric}", metric.Key);
                    result[metric.Key] = 0.0;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating trend velocities");
        }

        return result;
    }

    /// <summary>
    /// Calculates metric velocity (rate of change) using multiple strategies:
    /// 1. Simple velocity: immediate rate of change from previous value
    /// 2. Weighted velocity: accounts for time intervals and historical patterns
    /// 3. Trend velocity: based on linear regression over recent observations
    /// Returns units per minute for standardized comparison.
    /// </summary>
    private double CalculateMetricVelocity(string metricName, double currentValue, DateTime timestamp)
    {
        try
        {
            // Strategy 1: Calculate velocity from immediate previous value
            if (_previousValues.TryGetValue(metricName, out var previous))
            {
                var timeElapsed = (timestamp - previous.LastTimestamp).TotalSeconds;

                // Only calculate velocity if sufficient time has passed
                if (timeElapsed < MinimumTimeWindowSeconds)
                {
                    _logger.LogTrace("Insufficient time elapsed for metric {Metric}: {Seconds:F2}s",
                        metricName, timeElapsed);
                    return 0.0;
                }

                // Calculate simple velocity: change per minute
                var simpleVelocity = CalculateSimpleVelocity(currentValue, previous.LastValue, timeElapsed);

                // Strategy 2: If sufficient history exists, calculate weighted velocity
                if (_metricHistory.TryGetValue(metricName, out var history) && history.Count >= 3)
                {
                    var weightedVelocity = CalculateWeightedVelocity(history);

                    // Use weighted velocity but cap outliers with simple velocity
                    return BoundVelocity(weightedVelocity, simpleVelocity);
                }

                return simpleVelocity;
            }

            // First observation - no previous value to compare
            // Update previous value cache for next call
            _previousValues[metricName] = (timestamp, currentValue);
            return 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error calculating metric velocity for {Metric}", metricName);
            return 0.0;
        }
    }

    /// <summary>
    /// Calculates simple velocity as immediate rate of change per minute.
    /// Formula: (currentValue - previousValue) / (timeElapsed / 60)
    /// </summary>
    private double CalculateSimpleVelocity(double currentValue, double previousValue, double timeElapsedSeconds)
    {
        if (timeElapsedSeconds <= 0)
            return 0.0;

        // Convert to per-minute rate
        var timeElapsedMinutes = timeElapsedSeconds / 60.0;
        var velocity = (currentValue - previousValue) / timeElapsedMinutes;

        return velocity;
    }

    /// <summary>
    /// Calculates weighted velocity using recent observations with time-weighted emphasis.
    /// More recent observations have higher weight in the velocity calculation.
    /// Uses least squares method for trend fitting.
    /// </summary>
    private double CalculateWeightedVelocity(List<(DateTime Timestamp, double Value)> history)
    {
        if (history.Count < 2)
            return 0.0;

        try
        {
            // Use recent observations (last 10 or all if less)
            var recentCount = Math.Min(10, history.Count);
            var recentHistory = history.Skip(history.Count - recentCount).ToList();

            // Calculate time indices (in minutes from oldest observation)
            var firstTime = recentHistory.First().Timestamp;
            var timeIndices = recentHistory.Select(h =>
                (h.Timestamp - firstTime).TotalSeconds / 60.0).ToArray();
            var values = recentHistory.Select(h => h.Value).ToArray();

            // Calculate least squares fit
            var n = recentHistory.Count;
            var sumT = timeIndices.Sum();
            var sumV = values.Sum();
            var sumTV = timeIndices.Zip(values, (t, v) => t * v).Sum();
            var sumT2 = timeIndices.Sum(t => t * t);

            var denominator = (n * sumT2) - (sumT * sumT);
            if (Math.Abs(denominator) < double.Epsilon)
                return 0.0;

            // Slope is the velocity (change per minute)
            var velocity = (n * sumTV - sumT * sumV) / denominator;

            return velocity;
        }
        catch
        {
            return 0.0;
        }
    }

    /// <summary>
    /// Bounds velocity to prevent unrealistic values while preserving true trends.
    /// Uses simple velocity as a reference point to validate weighted velocity.
    /// </summary>
    private double BoundVelocity(double weightedVelocity, double simpleVelocity)
    {
        // If velocities have different signs, use simple velocity
        if ((weightedVelocity > 0) != (simpleVelocity > 0))
            return simpleVelocity;

        // Allow weighted velocity up to 2x the simple velocity
        var maxDeviation = Math.Abs(simpleVelocity) * 2.0;
        var minAllowed = simpleVelocity - maxDeviation;
        var maxAllowed = simpleVelocity + maxDeviation;

        return Math.Clamp(weightedVelocity, minAllowed, maxAllowed);
    }

    /// <summary>
    /// Tracks a metric value in historical data for velocity trend analysis.
    /// </summary>
    private void TrackMetricValue(string metricName, DateTime timestamp, double value)
    {
        try
        {
            // Initialize history if not present
            if (!_metricHistory.ContainsKey(metricName))
            {
                _metricHistory[metricName] = new List<(DateTime, double)>();
            }

            // Add current observation
            _metricHistory[metricName].Add((timestamp, value));

            // Maintain maximum history size
            if (_metricHistory[metricName].Count > MaxHistorySize)
            {
                _metricHistory[metricName].RemoveAt(0);
            }

            // Update previous value cache
            _previousValues[metricName] = (timestamp, value);

            _logger.LogTrace("Tracked metric {Metric}: value={Value:F3}, history={Count}",
                metricName, value, _metricHistory[metricName].Count);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error tracking metric value for {Metric}", metricName);
        }
    }

    /// <summary>
    /// Clears all historical data and cached values (useful for memory management).
    /// </summary>
    internal void ClearHistory()
    {
        _metricHistory.Clear();
        _previousValues.Clear();
        _logger.LogDebug("Cleared velocity history and cached values");
    }

    /// <summary>
    /// Gets the current history size for a metric (for testing/monitoring).
    /// </summary>
    internal int GetHistorySize(string metricName)
    {
        return _metricHistory.TryGetValue(metricName, out var history) ? history.Count : 0;
    }

    /// <summary>
    /// Gets previous value information for a metric (for testing/monitoring).
    /// </summary>
    internal (DateTime Timestamp, double Value)? GetPreviousValue(string metricName)
    {
        return _previousValues.TryGetValue(metricName, out var previous) ? previous : null;
    }
}