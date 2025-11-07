using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI;

/// <summary>
/// Updates regression analysis for metrics using least squares linear regression:
/// - Calculates slope (trend direction and magnitude)
/// - Calculates intercept (baseline value)
/// - Calculates R² (coefficient of determination - goodness of fit)
/// - Tracks metric history for accurate regression analysis
/// </summary>
internal sealed class RegressionUpdater : IRegressionUpdater
{
    private readonly ILogger<RegressionUpdater> _logger;

    // Cache for storing historical metric values with timestamps
    // Stores up to 60 data points per metric for regression analysis
    private readonly Dictionary<string, List<(int Index, double Value)>> _metricHistory = new();

    // Counter for sequence indexing (time-series x-axis)
    private int _sequenceIndex = 0;

    public RegressionUpdater(ILogger<RegressionUpdater> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Dictionary<string, RegressionResult> UpdateRegressionResults(
        Dictionary<string, double> currentMetrics,
        DateTime timestamp)
    {
        var result = new Dictionary<string, RegressionResult>();

        try
        {
            // Increment sequence for this batch
            _sequenceIndex++;

            foreach (var metric in currentMetrics)
            {
                try
                {
                    // Initialize metric history if not present
                    if (!_metricHistory.ContainsKey(metric.Key))
                    {
                        _metricHistory[metric.Key] = new List<(int, double)>();
                    }

                    // Add current value with its sequence index
                    _metricHistory[metric.Key].Add((_sequenceIndex, metric.Value));

                    // Keep history to reasonable size (max 60 data points)
                    const int maxHistorySize = 60;
                    if (_metricHistory[metric.Key].Count > maxHistorySize)
                    {
                        _metricHistory[metric.Key].RemoveAt(0);
                    }

                    // Perform linear regression analysis
                    var regression = CalculateLinearRegression(metric.Key);
                    result[metric.Key] = regression;

                    _logger.LogTrace("Regression for {Metric}: Slope={Slope:F4}, Intercept={Intercept:F4}, R²={RSquared:F3}",
                        metric.Key, regression.Slope, regression.Intercept, regression.RSquared);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error calculating regression for metric {Metric}", metric.Key);
                    // Return zero regression on error
                    result[metric.Key] = new RegressionResult
                    {
                        Slope = 0,
                        Intercept = 0,
                        RSquared = 0
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error performing regression analysis");
        }

        return result;
    }

    /// <summary>
    /// Calculates linear regression using least squares method.
    ///
    /// Linear regression formula: y = mx + b
    /// Where:
    /// - m (slope) = (n*Σ(xy) - Σx*Σy) / (n*Σ(x²) - (Σx)²)
    /// - b (intercept) = (Σy - m*Σx) / n
    /// - R² = 1 - (SS_res / SS_tot)
    ///
    /// Where:
    /// - SS_res = Σ(y - ŷ)² (sum of squares of residuals)
    /// - SS_tot = Σ(y - ȳ)² (total sum of squares)
    /// </summary>
    private RegressionResult CalculateLinearRegression(string metricName)
    {
        try
        {
            if (!_metricHistory.TryGetValue(metricName, out var history) || history.Count < 2)
            {
                // Need at least 2 points for regression
                return new RegressionResult
                {
                    Slope = 0,
                    Intercept = 0,
                    RSquared = 0
                };
            }

            var n = history.Count;
            var xValues = history.Select(h => (double)h.Index).ToArray();
            var yValues = history.Select(h => h.Value).ToArray();

            // Calculate sums needed for least squares formulas
            var sumX = xValues.Sum();
            var sumY = yValues.Sum();
            var sumXY = xValues.Zip(yValues, (x, y) => x * y).Sum();
            var sumX2 = xValues.Select(x => x * x).Sum();

            // Calculate slope (m)
            var denominator = (n * sumX2) - (sumX * sumX);
            if (Math.Abs(denominator) < double.Epsilon)
            {
                // No variation in x, can't calculate slope
                return new RegressionResult
                {
                    Slope = 0,
                    Intercept = yValues.Average(),
                    RSquared = 0
                };
            }

            var slope = (n * sumXY - sumX * sumY) / denominator;

            // Calculate intercept (b)
            var intercept = (sumY - slope * sumX) / n;

            // Calculate R² (coefficient of determination)
            var meanY = yValues.Average();
            var rSquared = CalculateRSquared(yValues, xValues, slope, intercept, meanY);

            return new RegressionResult
            {
                Slope = slope,
                Intercept = intercept,
                RSquared = rSquared
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error calculating linear regression for metric {Metric}", metricName);
            return new RegressionResult
            {
                Slope = 0,
                Intercept = 0,
                RSquared = 0
            };
        }
    }

    /// <summary>
    /// Calculates R² (coefficient of determination).
    /// R² indicates how well the regression line fits the data.
    /// - R² = 1: Perfect fit
    /// - R² = 0: No linear relationship
    /// - R² < 0: Model fits worse than a horizontal line
    /// </summary>
    private double CalculateRSquared(double[] yValues, double[] xValues, double slope, double intercept, double meanY)
    {
        try
        {
            if (yValues.Length < 2)
                return 0;

            // Calculate predicted values (ŷ = mx + b)
            var predictedValues = xValues.Select(x => slope * x + intercept).ToArray();

            // Calculate sum of squares of residuals (SS_res)
            var residuals = yValues.Zip(predictedValues, (actual, predicted) => actual - predicted).ToArray();
            var ssRes = residuals.Select(r => r * r).Sum();

            // Calculate total sum of squares (SS_tot)
            var deviations = yValues.Select(y => y - meanY).ToArray();
            var ssTot = deviations.Select(d => d * d).Sum();

            // Avoid division by zero
            if (Math.Abs(ssTot) < double.Epsilon)
                return 0;

            // R² = 1 - (SS_res / SS_tot)
            var rSquared = 1.0 - (ssRes / ssTot);

            // Clamp to valid range [-∞, 1]
            return Math.Min(rSquared, 1.0);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error calculating R²");
            return 0;
        }
    }

    /// <summary>
    /// Determines the trend direction based on slope.
    /// </summary>
    internal TrendDirection GetTrendDirection(double slope)
    {
        const double threshold = 0.001; // Small threshold to avoid floating-point noise

        if (slope > threshold)
            return TrendDirection.Increasing;
        else if (slope < -threshold)
            return TrendDirection.Decreasing;
        else
            return TrendDirection.Stable;
    }

    /// <summary>
    /// Calculates the confidence level of the regression fit based on R².
    /// </summary>
    internal ConfidenceLevel GetConfidenceLevel(double rSquared)
    {
        if (rSquared >= 0.9)
            return ConfidenceLevel.VeryHigh;
        else if (rSquared >= 0.7)
            return ConfidenceLevel.High;
        else if (rSquared >= 0.5)
            return ConfidenceLevel.Medium;
        else if (rSquared >= 0.3)
            return ConfidenceLevel.Low;
        else
            return ConfidenceLevel.VeryLow;
    }

    /// <summary>
    /// Clears all historical data to free memory.
    /// </summary>
    internal void ClearHistory()
    {
        _metricHistory.Clear();
        _sequenceIndex = 0;
        _logger.LogDebug("Cleared regression history");
    }

    /// <summary>
    /// Gets the current history size for a metric (for testing/monitoring).
    /// </summary>
    internal int GetHistorySize(string metricName)
    {
        return _metricHistory.TryGetValue(metricName, out var history) ? history.Count : 0;
    }

    /// <summary>
    /// Trend direction enumeration.
    /// </summary>
    internal enum TrendDirection
    {
        Decreasing = -1,
        Stable = 0,
        Increasing = 1
    }

    /// <summary>
    /// Confidence level enumeration based on R².
    /// </summary>
    internal enum ConfidenceLevel
    {
        VeryLow = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        VeryHigh = 4
    }
}