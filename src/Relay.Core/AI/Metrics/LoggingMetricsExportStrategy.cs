using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Structured logging-based metrics export strategy.
    /// </summary>
    internal class LoggingMetricsExportStrategy : IMetricsExportStrategy
    {
        private readonly ILogger _logger;

        public string Name => "Logging";

        public LoggingMetricsExportStrategy(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ValueTask ExportAsync(AIModelStatistics statistics, CancellationToken cancellationToken = default)
        {
            // Categorize metrics quality
            var quality = CategorizeMetricsQuality(statistics);
            var logLevel = quality switch
            {
                MetricQuality.Excellent => LogLevel.Information,
                MetricQuality.Good => LogLevel.Information,
                MetricQuality.Fair => LogLevel.Warning,
                MetricQuality.Poor => LogLevel.Warning,
                _ => LogLevel.Error
            };

            _logger.Log(logLevel,
                "AI Model Statistics Export - Quality: {Quality}\n" +
                "  Model Version: {ModelVersion}\n" +
                "  Training Date: {ModelTrainingDate:yyyy-MM-dd HH:mm:ss}\n" +
                "  Last Retraining: {LastRetraining:yyyy-MM-dd HH:mm:ss}\n" +
                "  Performance Metrics:\n" +
                "    - Total Predictions: {TotalPredictions:N0}\n" +
                "    - Accuracy: {AccuracyScore:P2}\n" +
                "    - Precision: {PrecisionScore:P2}\n" +
                "    - Recall: {RecallScore:P2}\n" +
                "    - F1 Score: {F1Score:P2}\n" +
                "    - Confidence: {ModelConfidence:P2}\n" +
                "  Efficiency Metrics:\n" +
                "    - Avg Prediction Time: {AveragePredictionTime:F2}ms\n" +
                "    - Training Data Points: {TrainingDataPoints:N0}",
                quality,
                statistics.ModelVersion,
                statistics.ModelTrainingDate,
                statistics.LastRetraining,
                statistics.TotalPredictions,
                statistics.AccuracyScore,
                statistics.PrecisionScore,
                statistics.RecallScore,
                statistics.F1Score,
                statistics.ModelConfidence,
                statistics.AveragePredictionTime.TotalMilliseconds,
                statistics.TrainingDataPoints);

            return ValueTask.CompletedTask;
        }

        private MetricQuality CategorizeMetricsQuality(AIModelStatistics statistics)
        {
            var score = CalculateQualityScore(statistics);

            return score switch
            {
                >= 0.90 => MetricQuality.Excellent,
                >= 0.75 => MetricQuality.Good,
                >= 0.60 => MetricQuality.Fair,
                >= 0.40 => MetricQuality.Poor,
                _ => MetricQuality.Critical
            };
        }

        private double CalculateQualityScore(AIModelStatistics statistics)
        {
            // Weighted quality score
            var accuracyWeight = 0.25;
            var precisionWeight = 0.20;
            var recallWeight = 0.20;
            var f1Weight = 0.25;
            var confidenceWeight = 0.10;

            var score = (statistics.AccuracyScore * accuracyWeight) +
                       (statistics.PrecisionScore * precisionWeight) +
                       (statistics.RecallScore * recallWeight) +
                       (statistics.F1Score * f1Weight) +
                       (statistics.ModelConfidence * confidenceWeight);

            // Penalty for slow predictions
            if (statistics.AveragePredictionTime.TotalMilliseconds > 100.0)
            {
                score *= 0.9; // 10% penalty
            }

            return score;
        }

        private enum MetricQuality
        {
            Excellent,
            Good,
            Fair,
            Poor,
            Critical
        }
    }
}