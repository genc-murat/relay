using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Default implementation of AI metrics exporter with comprehensive monitoring integration.
    /// Supports OpenTelemetry metrics, structured logging, and performance tracking.
    /// </summary>
    internal class DefaultAIMetricsExporter : IAIMetricsExporter, IDisposable
    {
        private readonly ILogger<DefaultAIMetricsExporter> _logger;
        private readonly Meter _meter;
        private readonly ActivitySource _activitySource;
        
        // Counters
        private readonly Counter<long> _totalExportsCounter;
        private readonly Counter<long> _totalPredictionsCounter;
        private readonly Counter<long> _modelRetrainingCounter;
        
        // Gauges (using ObservableGauge)
        private readonly ObservableGauge<double> _accuracyGauge;
        private readonly ObservableGauge<double> _precisionGauge;
        private readonly ObservableGauge<double> _recallGauge;
        private readonly ObservableGauge<double> _f1ScoreGauge;
        private readonly ObservableGauge<double> _confidenceGauge;
        
        // Histograms
        private readonly Histogram<double> _predictionTimeHistogram;
        private readonly Histogram<long> _trainingDataPointsHistogram;
        
        // State tracking
        private long _totalExports = 0;
        private DateTime _lastExportTime = DateTime.MinValue;
        private readonly ConcurrentQueue<AIModelStatistics> _recentStatistics = new();
        private readonly int _maxHistorySize = 100;
        private AIModelStatistics? _latestStatistics;
        private readonly object _statisticsLock = new();
        
        // Performance tracking
        private readonly ConcurrentDictionary<string, MetricTrend> _metricTrends = new();
        private readonly Timer _alertCheckTimer;
        private bool _disposed = false;
        
        // Alert thresholds
        private const double MinAcceptableAccuracy = 0.70;
        private const double MinAcceptableF1Score = 0.65;
        private const double MinAcceptableConfidence = 0.60;
        private const double MaxAcceptablePredictionTimeMs = 100.0;

        public DefaultAIMetricsExporter(ILogger<DefaultAIMetricsExporter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize OpenTelemetry Meter
            _meter = new Meter("Relay.AI.Metrics", "1.0.0");
            _activitySource = new ActivitySource("Relay.AI.MetricsExporter", "1.0.0");
            
            // Initialize counters
            _totalExportsCounter = _meter.CreateCounter<long>(
                "relay.ai.exports.total",
                unit: "exports",
                description: "Total number of metrics exports");
            
            _totalPredictionsCounter = _meter.CreateCounter<long>(
                "relay.ai.predictions.total",
                unit: "predictions",
                description: "Total number of AI model predictions");
            
            _modelRetrainingCounter = _meter.CreateCounter<long>(
                "relay.ai.retraining.total",
                unit: "retrainings",
                description: "Total number of model retraining sessions");
            
            // Initialize gauges (observable)
            _accuracyGauge = _meter.CreateObservableGauge(
                "relay.ai.model.accuracy",
                () => _latestStatistics?.AccuracyScore ?? 0.0,
                unit: "score",
                description: "Current model accuracy score");
            
            _precisionGauge = _meter.CreateObservableGauge(
                "relay.ai.model.precision",
                () => _latestStatistics?.PrecisionScore ?? 0.0,
                unit: "score",
                description: "Current model precision score");
            
            _recallGauge = _meter.CreateObservableGauge(
                "relay.ai.model.recall",
                () => _latestStatistics?.RecallScore ?? 0.0,
                unit: "score",
                description: "Current model recall score");
            
            _f1ScoreGauge = _meter.CreateObservableGauge(
                "relay.ai.model.f1score",
                () => _latestStatistics?.F1Score ?? 0.0,
                unit: "score",
                description: "Current model F1 score");
            
            _confidenceGauge = _meter.CreateObservableGauge(
                "relay.ai.model.confidence",
                () => _latestStatistics?.ModelConfidence ?? 0.0,
                unit: "score",
                description: "Current model confidence level");
            
            // Initialize histograms
            _predictionTimeHistogram = _meter.CreateHistogram<double>(
                "relay.ai.prediction.duration",
                unit: "ms",
                description: "AI model prediction duration");
            
            _trainingDataPointsHistogram = _meter.CreateHistogram<long>(
                "relay.ai.training.datapoints",
                unit: "datapoints",
                description: "Number of training data points used");
            
            // Initialize alert checking timer (every 5 minutes)
            _alertCheckTimer = new Timer(CheckMetricsThresholds, null, 
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("AI Metrics Exporter initialized with OpenTelemetry support");
        }

        public async ValueTask ExportMetricsAsync(AIModelStatistics statistics, CancellationToken cancellationToken = default)
        {
            if (statistics == null)
                throw new ArgumentNullException(nameof(statistics));

            using var activity = _activitySource.StartActivity("ExportMetrics", ActivityKind.Internal);
            
            var exportStartTime = DateTime.UtcNow;
            _totalExports++;
            _lastExportTime = exportStartTime;

            try
            {
                // Update state
                lock (_statisticsLock)
                {
                    _latestStatistics = statistics;
                }
                
                // Store in recent history
                _recentStatistics.Enqueue(statistics);
                while (_recentStatistics.Count > _maxHistorySize)
                {
                    _recentStatistics.TryDequeue(out _);
                }

                // Export to OpenTelemetry metrics
                await ExportToOpenTelemetryAsync(statistics, cancellationToken);

                // Export as structured logs with categorization
                await ExportStructuredLogsAsync(statistics, cancellationToken);

                // Calculate and track trends
                await CalculateAndTrackTrendsAsync(statistics, cancellationToken);

                // Check for alerts
                await CheckForAlertsAsync(statistics, cancellationToken);

                // Record export activity
                activity?.SetTag("export.number", _totalExports);
                activity?.SetTag("export.duration_ms", (DateTime.UtcNow - exportStartTime).TotalMilliseconds);
                activity?.SetTag("model.version", statistics.ModelVersion);
                activity?.SetTag("model.accuracy", statistics.AccuracyScore);

                var exportDuration = (DateTime.UtcNow - exportStartTime).TotalMilliseconds;
                _logger.LogDebug("Metrics export #{ExportNumber} completed in {Duration}ms", 
                    _totalExports, exportDuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting AI metrics");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        private ValueTask ExportToOpenTelemetryAsync(AIModelStatistics statistics, CancellationToken cancellationToken)
        {
            // Update counters
            _totalExportsCounter.Add(1, 
                new KeyValuePair<string, object?>("model.version", statistics.ModelVersion));
            
            _totalPredictionsCounter.Add(statistics.TotalPredictions,
                new KeyValuePair<string, object?>("model.version", statistics.ModelVersion));

            // Check if this is a retraining event
            if (_recentStatistics.Count > 0)
            {
                var previous = _recentStatistics.LastOrDefault();
                if (previous != null && statistics.LastRetraining > previous.LastRetraining)
                {
                    _modelRetrainingCounter.Add(1,
                        new KeyValuePair<string, object?>("model.version", statistics.ModelVersion));
                }
            }

            // Record histograms
            _predictionTimeHistogram.Record(statistics.AveragePredictionTime.TotalMilliseconds,
                new KeyValuePair<string, object?>("model.version", statistics.ModelVersion));
            
            _trainingDataPointsHistogram.Record(statistics.TrainingDataPoints,
                new KeyValuePair<string, object?>("model.version", statistics.ModelVersion));

            return ValueTask.CompletedTask;
        }

        private ValueTask ExportStructuredLogsAsync(AIModelStatistics statistics, CancellationToken cancellationToken)
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
                "AI Model Statistics Export #{ExportNumber} - Quality: {Quality}\n" +
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
                _totalExports,
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

        private ValueTask CalculateAndTrackTrendsAsync(AIModelStatistics statistics, CancellationToken cancellationToken)
        {
            TrackMetricTrend("accuracy", statistics.AccuracyScore);
            TrackMetricTrend("precision", statistics.PrecisionScore);
            TrackMetricTrend("recall", statistics.RecallScore);
            TrackMetricTrend("f1_score", statistics.F1Score);
            TrackMetricTrend("confidence", statistics.ModelConfidence);
            TrackMetricTrend("prediction_time_ms", statistics.AveragePredictionTime.TotalMilliseconds);

            // Log trends if significant changes detected
            foreach (var trend in _metricTrends)
            {
                if (Math.Abs(trend.Value.PercentageChange) > 10.0) // 10% change threshold
                {
                    var direction = trend.Value.PercentageChange > 0 ? "increased" : "decreased";
                    _logger.LogInformation(
                        "Significant trend detected: {MetricName} has {Direction} by {Change:F1}% over the last {Count} exports",
                        trend.Key, direction, Math.Abs(trend.Value.PercentageChange), trend.Value.DataPoints.Count);
                }
            }

            return ValueTask.CompletedTask;
        }

        private ValueTask CheckForAlertsAsync(AIModelStatistics statistics, CancellationToken cancellationToken)
        {
            var alerts = new List<string>();

            if (statistics.AccuracyScore < MinAcceptableAccuracy)
            {
                alerts.Add($"Accuracy ({statistics.AccuracyScore:P2}) is below threshold ({MinAcceptableAccuracy:P2})");
            }

            if (statistics.F1Score < MinAcceptableF1Score)
            {
                alerts.Add($"F1 Score ({statistics.F1Score:P2}) is below threshold ({MinAcceptableF1Score:P2})");
            }

            if (statistics.ModelConfidence < MinAcceptableConfidence)
            {
                alerts.Add($"Model confidence ({statistics.ModelConfidence:P2}) is below threshold ({MinAcceptableConfidence:P2})");
            }

            if (statistics.AveragePredictionTime.TotalMilliseconds > MaxAcceptablePredictionTimeMs)
            {
                alerts.Add($"Average prediction time ({statistics.AveragePredictionTime.TotalMilliseconds:F2}ms) exceeds threshold ({MaxAcceptablePredictionTimeMs}ms)");
            }

            // Check if model is stale (not retrained in 30 days)
            var daysSinceRetraining = (DateTime.UtcNow - statistics.LastRetraining).TotalDays;
            if (daysSinceRetraining > 30)
            {
                alerts.Add($"Model has not been retrained in {daysSinceRetraining:F0} days");
            }

            if (alerts.Count > 0)
            {
                _logger.LogWarning(
                    "AI Model Performance Alerts Detected ({Count} issues):\n{Alerts}",
                    alerts.Count,
                    string.Join("\n", alerts.Select((a, i) => $"  {i + 1}. {a}")));
            }

            return ValueTask.CompletedTask;
        }

        private void CheckMetricsThresholds(object? state)
        {
            if (_latestStatistics == null)
                return;

            try
            {
                // Perform periodic health check
                var healthStatus = CalculateOverallHealth(_latestStatistics);
                
                _logger.LogInformation(
                    "Periodic AI Model Health Check: {Status} ({Score:P1})",
                    healthStatus.Status,
                    healthStatus.Score);

                if (healthStatus.Score < 0.7)
                {
                    _logger.LogWarning(
                        "AI model health is below acceptable levels. Consider retraining or optimization. Issues: {Issues}",
                        string.Join(", ", healthStatus.Issues));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic metrics threshold check");
            }
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
            if (statistics.AveragePredictionTime.TotalMilliseconds > MaxAcceptablePredictionTimeMs)
            {
                score *= 0.9; // 10% penalty
            }

            return score;
        }

        private HealthStatus CalculateOverallHealth(AIModelStatistics statistics)
        {
            var issues = new List<string>();
            var score = CalculateQualityScore(statistics);

            if (statistics.AccuracyScore < MinAcceptableAccuracy)
                issues.Add("Low accuracy");
            
            if (statistics.F1Score < MinAcceptableF1Score)
                issues.Add("Low F1 score");
            
            if (statistics.ModelConfidence < MinAcceptableConfidence)
                issues.Add("Low confidence");
            
            if (statistics.AveragePredictionTime.TotalMilliseconds > MaxAcceptablePredictionTimeMs)
                issues.Add("High prediction latency");
            
            var daysSinceRetraining = (DateTime.UtcNow - statistics.LastRetraining).TotalDays;
            if (daysSinceRetraining > 30)
                issues.Add("Stale model");

            var status = score switch
            {
                >= 0.90 => "Excellent",
                >= 0.75 => "Good",
                >= 0.60 => "Fair",
                >= 0.40 => "Poor",
                _ => "Critical"
            };

            return new HealthStatus
            {
                Score = score,
                Status = status,
                Issues = issues
            };
        }

        private void TrackMetricTrend(string metricName, double value)
        {
            var trend = _metricTrends.GetOrAdd(metricName, _ => new MetricTrend());
            
            trend.DataPoints.Enqueue(value);
            
            // Keep only last 20 data points
            while (trend.DataPoints.Count > 20)
            {
                trend.DataPoints.TryDequeue(out _);
            }

            // Calculate percentage change
            if (trend.DataPoints.Count >= 2)
            {
                var oldestValue = trend.DataPoints.First();
                var newestValue = trend.DataPoints.Last();
                
                if (oldestValue != 0)
                {
                    trend.PercentageChange = ((newestValue - oldestValue) / oldestValue) * 100.0;
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _alertCheckTimer?.Dispose();
            _meter?.Dispose();
            _activitySource?.Dispose();

            _logger.LogInformation(
                "AI Metrics Exporter disposed. Total exports: {TotalExports}, Last export: {LastExportTime}",
                _totalExports,
                _lastExportTime);
        }

        private enum MetricQuality
        {
            Excellent,
            Good,
            Fair,
            Poor,
            Critical
        }

        private class MetricTrend
        {
            public ConcurrentQueue<double> DataPoints { get; } = new();
            public double PercentageChange { get; set; }
        }

        private class HealthStatus
        {
            public double Score { get; set; }
            public string Status { get; set; } = string.Empty;
            public List<string> Issues { get; set; } = new();
        }
    }
}
