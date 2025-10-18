using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI
{
    /// <summary>
    /// OpenTelemetry-based metrics export strategy.
    /// </summary>
    internal class OpenTelemetryMetricsExportStrategy : IMetricsExportStrategy, IDisposable
    {
        private readonly Meter _meter;
        private readonly Counter<long> _totalExportsCounter;
        private readonly Counter<long> _totalPredictionsCounter;
        private readonly Counter<long> _modelRetrainingCounter;
        private readonly ObservableGauge<double> _accuracyGauge;
        private readonly ObservableGauge<double> _precisionGauge;
        private readonly ObservableGauge<double> _recallGauge;
        private readonly ObservableGauge<double> _f1ScoreGauge;
        private readonly ObservableGauge<double> _confidenceGauge;
        private readonly Histogram<double> _predictionTimeHistogram;
        private readonly Histogram<long> _trainingDataPointsHistogram;

        private readonly ConcurrentQueue<AIModelStatistics> _recentStatistics = new();
        private readonly int _maxHistorySize = 100;
        private AIModelStatistics? _latestStatistics;
        private readonly object _statisticsLock = new();

        public string Name => "OpenTelemetry";

        public OpenTelemetryMetricsExportStrategy(string meterName = "Relay.AI.Metrics", string version = "1.0.0")
        {
            _meter = new Meter(meterName, version);

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
        }

        public ValueTask ExportAsync(AIModelStatistics statistics, CancellationToken cancellationToken = default)
        {
            // Update latest statistics
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

            // Update counters
            _totalExportsCounter.Add(1,
                new KeyValuePair<string, object?>("model.version", statistics.ModelVersion));

            _totalPredictionsCounter.Add(statistics.TotalPredictions,
                new KeyValuePair<string, object?>("model.version", statistics.ModelVersion));

            // Check if this is a retraining event
            if (_recentStatistics.Count > 1)
            {
                var previous = _recentStatistics.ToArray()[^2]; // Second to last
                if (statistics.LastRetraining > previous.LastRetraining)
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

        public void Dispose()
        {
            _meter?.Dispose();
        }
    }
}