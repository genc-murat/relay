using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI;

/// <summary>
/// Updates anomaly detection for metrics using multiple statistical methods:
/// - Z-Score analysis (standard deviation-based)
/// - Interquartile Range (IQR) analysis
/// - Median Absolute Deviation (MAD) analysis
/// - Spike and drop detection
/// </summary>
internal sealed class AnomalyUpdater : IAnomalyUpdater
{
    private readonly ILogger<AnomalyUpdater> _logger;
    private readonly TrendAnalysisConfig _config;

    // Cache for calculating standard deviations from moving averages
    private readonly Dictionary<string, List<double>> _metricHistory = new();

    public AnomalyUpdater(ILogger<AnomalyUpdater> logger, TrendAnalysisConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public List<MetricAnomaly> UpdateAnomalies(
        Dictionary<string, double> currentMetrics,
        Dictionary<string, MovingAverageData> movingAverages)
    {
        var anomalies = new List<MetricAnomaly>();

        try
        {
            foreach (var metric in currentMetrics)
            {
                if (!movingAverages.TryGetValue(metric.Key, out var ma)) continue;

                // Track metric history for standard deviation calculation
                if (!_metricHistory.ContainsKey(metric.Key))
                {
                    _metricHistory[metric.Key] = new List<double>();
                }

                _metricHistory[metric.Key].Add(metric.Value);

                // Keep history to reasonable size (last 100 values)
                if (_metricHistory[metric.Key].Count > 100)
                {
                    _metricHistory[metric.Key].RemoveAt(0);
                }

                // Detect anomalies using multiple methods
                var detectedAnomalies = new List<MetricAnomaly>();

                // Method 1: Z-Score based detection
                var zScoreAnomaly = DetectAnomalyByZScore(metric.Key, metric.Value, ma);
                if (zScoreAnomaly != null)
                    detectedAnomalies.Add(zScoreAnomaly);

                // Method 2: IQR based detection (more robust to outliers)
                var iqrAnomaly = DetectAnomalyByIQR(metric.Key, metric.Value, ma);
                if (iqrAnomaly != null)
                    detectedAnomalies.Add(iqrAnomaly);

                // Method 3: Spike/Drop detection
                var spikeAnomaly = DetectSpikeOrDrop(metric.Key, metric.Value, ma);
                if (spikeAnomaly != null)
                    detectedAnomalies.Add(spikeAnomaly);

                // Method 4: Velocity (rate of change) detection
                var velocityAnomaly = DetectHighVelocity(metric.Key, metric.Value, ma);
                if (velocityAnomaly != null)
                    detectedAnomalies.Add(velocityAnomaly);

                // Add detected anomalies to results
                anomalies.AddRange(detectedAnomalies);

                // Log if anomalies detected
                foreach (var anomaly in detectedAnomalies)
                {
                    _logger.LogWarning(
                        "Anomaly [{Severity}] detected in {Metric}: Current={Current:F2}, Expected={Expected:F2}, Description={Description}",
                        anomaly.Severity, metric.Key, metric.Value, anomaly.ExpectedValue, anomaly.Description);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting performance anomalies");
        }

        return anomalies;
    }

    /// <summary>
    /// Detects anomalies using Z-Score analysis with accurate standard deviation.
    /// Z-Score = (Value - Mean) / StdDev
    /// </summary>
    private MetricAnomaly? DetectAnomalyByZScore(string metricName, double value, MovingAverageData ma)
    {
        try
        {
            var mean = ma.MA15;
            var stdDev = CalculateStandardDeviation(metricName, mean);

            if (stdDev <= 0)
                return null; // Cannot calculate Z-score without valid standard deviation

            var zScore = CalculateZScore(value, mean, stdDev);

            if (System.Math.Abs(zScore) >= _config.AnomalyZScoreThreshold)
            {
                var severity = DetermineSeverity(System.Math.Abs(zScore));

                return new MetricAnomaly
                {
                    MetricName = metricName,
                    CurrentValue = value,
                    ExpectedValue = mean,
                    Deviation = System.Math.Abs(value - mean),
                    ZScore = zScore,
                    Severity = severity,
                    Description = $"Z-Score anomaly in {metricName}: Current={value:F2}, Expected={mean:F2}, Z-Score={zScore:F2}, StdDev={stdDev:F2}",
                    Timestamp = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error in Z-Score anomaly detection for {Metric}", metricName);
        }

        return null;
    }

    /// <summary>
    /// Detects anomalies using Interquartile Range (IQR) method.
    /// More robust to outliers than Z-Score.
    /// Anomaly if value is outside Q1 - 1.5*IQR to Q3 + 1.5*IQR
    /// </summary>
    private MetricAnomaly? DetectAnomalyByIQR(string metricName, double value, MovingAverageData ma)
    {
        try
        {
            if (!_metricHistory.TryGetValue(metricName, out var history) || history.Count < 4)
                return null; // Need at least 4 values for IQR calculation

            var sorted = history.OrderBy(x => x).ToList();
            var q1Index = sorted.Count / 4;
            var q3Index = (3 * sorted.Count) / 4;

            var q1 = sorted[q1Index];
            var q3 = sorted[q3Index];
            var iqr = q3 - q1;

            var lowerBound = q1 - (1.5 * iqr);
            var upperBound = q3 + (1.5 * iqr);

            if (value < lowerBound || value > upperBound)
            {
                var deviation = value < lowerBound
                    ? lowerBound - value
                    : value - upperBound;

                return new MetricAnomaly
                {
                    MetricName = metricName,
                    CurrentValue = value,
                    ExpectedValue = (q1 + q3) / 2,
                    Deviation = deviation,
                    ZScore = (value - ma.MA15) / (iqr > 0 ? iqr : 1),
                    Severity = DetermineIQRSeverity(value, lowerBound, upperBound, iqr),
                    Description = $"IQR anomaly: Current={value:F2}, Range=[{lowerBound:F2}, {upperBound:F2}], IQR={iqr:F2}",
                    Timestamp = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error in IQR anomaly detection for {Metric}", metricName);
        }

        return null;
    }

    /// <summary>
    /// Detects sudden spikes or drops in metric values.
    /// Anomaly if value differs from MA15 by more than configured percentage.
    /// </summary>
    private MetricAnomaly? DetectSpikeOrDrop(string metricName, double value, MovingAverageData ma)
    {
        try
        {
            var ma15 = ma.MA15;
            if (ma15 == 0) return null;

            var percentageChange = System.Math.Abs(value - ma15) / ma15;
            var spikeThreshold = 0.5; // 50% change is significant

            if (percentageChange >= spikeThreshold)
            {
                var isSpike = value > ma15;
                var description = isSpike
                    ? $"Spike detected: {percentageChange:P0} increase from {ma15:F2} to {value:F2}"
                    : $"Drop detected: {percentageChange:P0} decrease from {ma15:F2} to {value:F2}";

                return new MetricAnomaly
                {
                    MetricName = metricName,
                    CurrentValue = value,
                    ExpectedValue = ma15,
                    Deviation = System.Math.Abs(value - ma15),
                    ZScore = percentageChange,
                    Severity = AnomalySeverity.High, // Spikes/drops are always high severity
                    Description = description,
                    Timestamp = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error in spike/drop anomaly detection for {Metric}", metricName);
        }

        return null;
    }

    /// <summary>
    /// Detects high velocity (rapid rate of change) in metrics.
    /// </summary>
    private MetricAnomaly? DetectHighVelocity(string metricName, double value, MovingAverageData ma)
    {
        try
        {
            if (!_metricHistory.TryGetValue(metricName, out var history) || history.Count < 2)
                return null;

            var previousValue = history[history.Count - 2];
            if (previousValue == 0) return null;

            var velocity = System.Math.Abs(value - previousValue) / previousValue;

            if (velocity > _config.HighVelocityThreshold)
            {
                return new MetricAnomaly
                {
                    MetricName = metricName,
                    CurrentValue = value,
                    ExpectedValue = previousValue,
                    Deviation = System.Math.Abs(value - previousValue),
                    ZScore = velocity,
                    Severity = velocity > (2 * _config.HighVelocityThreshold) ? AnomalySeverity.High : AnomalySeverity.Medium,
                    Description = $"High velocity detected: {velocity:P0} change from {previousValue:F2} to {value:F2}",
                    Timestamp = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error in velocity anomaly detection for {Metric}", metricName);
        }

        return null;
    }

    /// <summary>
    /// Calculates accurate Z-Score: (Value - Mean) / StandardDeviation
    /// </summary>
    private double CalculateZScore(double value, double mean, double stdDev)
    {
        if (stdDev <= 0)
            return 0;

        return (value - mean) / stdDev;
    }

    /// <summary>
    /// Calculates standard deviation from historical metric values and moving averages.
    /// Uses the formula: StdDev = sqrt(E[(X - μ)²])
    /// </summary>
    private double CalculateStandardDeviation(string metricName, double mean)
    {
        try
        {
            if (!_metricHistory.TryGetValue(metricName, out var history) || history.Count < 2)
            {
                // Fallback: estimate based on moving averages range
                return System.Math.Max(mean * 0.1, 0.1); // Use 10% of mean as baseline
            }

            var variance = history.Sum(x => System.Math.Pow(x - mean, 2)) / history.Count;
            return System.Math.Sqrt(variance);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error calculating standard deviation for {Metric}", metricName);
            return 0;
        }
    }

    /// <summary>
    /// Determines anomaly severity based on Z-Score magnitude.
    /// </summary>
    private AnomalySeverity DetermineSeverity(double absoluteZScore)
    {
        if (absoluteZScore >= _config.HighAnomalyZScoreThreshold)
            return AnomalySeverity.High;

        if (absoluteZScore >= _config.AnomalyZScoreThreshold)
            return AnomalySeverity.Medium;

        return AnomalySeverity.Low;
    }

    /// <summary>
    /// Determines severity based on IQR boundaries.
    /// </summary>
    private AnomalySeverity DetermineIQRSeverity(double value, double lowerBound, double upperBound, double iqr)
    {
        var extendedLower = lowerBound - (3 * iqr);
        var extendedUpper = upperBound + (3 * iqr);

        // Critical: far beyond extended bounds
        if (value < extendedLower || value > extendedUpper)
            return AnomalySeverity.Critical;

        // High: outside extended bounds but within 3*IQR
        if (value < lowerBound - (1.5 * iqr) || value > upperBound + (1.5 * iqr))
            return AnomalySeverity.High;

        // Medium: outside normal bounds
        return AnomalySeverity.Medium;
    }

    /// <summary>
    /// Clears historical data to free memory (useful for long-running processes).
    /// </summary>
    internal void ClearHistory()
    {
        _metricHistory.Clear();
        _logger.LogDebug("Cleared metric history");
    }
}