using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Updates anomaly detection for metrics
    /// </summary>
    internal sealed class AnomalyUpdater : IAnomalyUpdater
    {
        private readonly ILogger<AnomalyUpdater> _logger;
        private readonly TrendAnalysisConfig _config;

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

                    var zScore = CalculateZScore(metric.Value, ma.MA15);

                    if (System.Math.Abs(zScore) >= _config.AnomalyZScoreThreshold)
                    {
                        var anomaly = new MetricAnomaly
                        {
                            MetricName = metric.Key,
                            CurrentValue = metric.Value,
                            ExpectedValue = ma.MA15,
                            Deviation = System.Math.Abs(metric.Value - ma.MA15),
                            ZScore = zScore,
                            Severity = System.Math.Abs(zScore) > _config.HighAnomalyZScoreThreshold ? AnomalySeverity.High : AnomalySeverity.Medium,
                            Description = $"Anomaly detected in {metric.Key}: Current={metric.Value:F2}, Expected={ma.MA15:F2}, Z-Score={zScore:F2}",
                            Timestamp = DateTime.UtcNow
                        };

                        anomalies.Add(anomaly);

                        _logger.LogWarning("Anomaly detected in {Metric}: Current={Current:F2}, Expected={Expected:F2}, Z-Score={ZScore:F2}",
                            metric.Key, metric.Value, ma.MA15, zScore);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error detecting performance anomalies");
            }

            return anomalies;
        }

        private double CalculateZScore(double value, double mean)
        {
            var stdDev = mean * 0.1; // Placeholder - in real scenario would use actual standard deviation
            return stdDev > 0 ? (value - mean) / stdDev : 0;
        }
    }
}