using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Updates correlation analysis between metrics
    /// </summary>
    internal sealed class CorrelationUpdater : ICorrelationUpdater
    {
        private readonly ILogger<CorrelationUpdater> _logger;
        private readonly TrendAnalysisConfig _config;
        private readonly Dictionary<string, Queue<double>> _metricHistory;
        private const int MaxHistorySize = 100; // Keep last 100 values for correlation calculation

        public CorrelationUpdater(ILogger<CorrelationUpdater> logger, TrendAnalysisConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _metricHistory = new Dictionary<string, Queue<double>>();
        }

        public Dictionary<string, List<string>> UpdateCorrelations(Dictionary<string, double> currentMetrics)
        {
            var result = new Dictionary<string, List<string>>();

            try
            {
                // Update history for each metric
                foreach (var metric in currentMetrics)
                {
                    if (!_metricHistory.ContainsKey(metric.Key))
                    {
                        _metricHistory[metric.Key] = new Queue<double>();
                    }

                    var queue = _metricHistory[metric.Key];
                    queue.Enqueue(metric.Value);

                    // Keep only the last MaxHistorySize values
                    while (queue.Count > MaxHistorySize)
                    {
                        queue.Dequeue();
                    }
                }

                var metricKeys = currentMetrics.Keys.ToArray();

                foreach (var metric1 in metricKeys)
                {
                    var correlations = new List<string>();

                    foreach (var metric2 in metricKeys)
                    {
                        if (metric1 == metric2) continue;

                        var correlation = CalculateCorrelation(metric1, metric2);

                        if (System.Math.Abs(correlation) > _config.CorrelationThreshold)
                        {
                            correlations.Add($"{metric2} (r={correlation:F2})");
                        }
                    }

                    if (correlations.Count > 0)
                    {
                        result[metric1] = correlations;
                        _logger.LogDebug("Metric {Metric} correlated with: {Correlations}",
                            metric1, string.Join(", ", correlations));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating metric correlations");
            }

            return result;
        }

        /// <summary>
        /// Calculates the Pearson correlation coefficient between two metrics using historical data
        /// </summary>
        /// <param name="metric1">The name of the first metric</param>
        /// <param name="metric2">The name of the second metric</param>
        /// <returns>Correlation coefficient between -1.0 and 1.0, or 0 if insufficient data</returns>
        private double CalculateCorrelation(string metric1, string metric2)
        {
            // Validate that both metrics have history
            if (!_metricHistory.ContainsKey(metric1) || !_metricHistory.ContainsKey(metric2))
            {
                return 0.0;
            }

            var series1 = _metricHistory[metric1].ToList();
            var series2 = _metricHistory[metric2].ToList();

            // Need at least 2 data points to calculate correlation
            if (series1.Count < 2 || series2.Count < 2)
            {
                return 0.0;
            }

            // Both series should have the same length; align if necessary
            var minLength = System.Math.Min(series1.Count, series2.Count);
            series1 = series1.Skip(series1.Count - minLength).ToList();
            series2 = series2.Skip(series2.Count - minLength).ToList();

            // Calculate means
            double mean1 = series1.Average();
            double mean2 = series2.Average();

            // Calculate numerator (covariance)
            double covariance = 0.0;
            for (int i = 0; i < minLength; i++)
            {
                covariance += (series1[i] - mean1) * (series2[i] - mean2);
            }

            // Calculate standard deviations
            double variance1 = 0.0;
            double variance2 = 0.0;
            for (int i = 0; i < minLength; i++)
            {
                variance1 += System.Math.Pow(series1[i] - mean1, 2);
                variance2 += System.Math.Pow(series2[i] - mean2, 2);
            }

            double stdDev1 = System.Math.Sqrt(variance1 / minLength);
            double stdDev2 = System.Math.Sqrt(variance2 / minLength);

            // Handle case where standard deviation is zero
            if (stdDev1 == 0.0 || stdDev2 == 0.0)
            {
                return 0.0;
            }

            // Calculate Pearson correlation coefficient
            double correlation = covariance / (minLength * stdDev1 * stdDev2);

            // Clamp correlation to [-1, 1] range to handle floating point precision issues
            correlation = System.Math.Max(-1.0, System.Math.Min(1.0, correlation));

            _logger.LogTrace("Correlation between {Metric1} and {Metric2}: {Correlation:F4}",
                metric1, metric2, correlation);

            return correlation;
        }
    }
}