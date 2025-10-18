using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI
{
    /// <summary>
    /// Command for executing metrics export operations.
    /// </summary>
    internal class MetricsExportCommand
    {
        private readonly AIModelStatistics _statistics;
        private readonly IMetricsValidator _validator;
        private readonly IMetricsTrendAnalyzer _trendAnalyzer;
        private readonly IMetricsExportStrategy _exportStrategy;
        private readonly IMetricsAlertObserver? _alertObserver;

        public MetricsExportCommand(
            AIModelStatistics statistics,
            IMetricsValidator validator,
            IMetricsTrendAnalyzer trendAnalyzer,
            IMetricsExportStrategy exportStrategy,
            IMetricsAlertObserver? alertObserver = null)
        {
            _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _trendAnalyzer = trendAnalyzer ?? throw new ArgumentNullException(nameof(trendAnalyzer));
            _exportStrategy = exportStrategy ?? throw new ArgumentNullException(nameof(exportStrategy));
            _alertObserver = alertObserver;
        }

        public async ValueTask ExecuteAsync(Activity? activity = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate statistics
                _validator.Validate(_statistics);

                // Analyze trends
                await _trendAnalyzer.AnalyzeTrendsAsync(_statistics, cancellationToken);

                // Check for alerts
                var alerts = CheckForAlerts();
                if (alerts.Count > 0 && _alertObserver != null)
                {
                    _alertObserver.OnAlertsDetected(alerts, _statistics);
                }

                // Execute export
                await _exportStrategy.ExportAsync(_statistics, cancellationToken);

                // Set activity tags
                activity?.SetTag("export.duration_ms", stopwatch.Elapsed.TotalMilliseconds);
                activity?.SetTag("model.version", _statistics.ModelVersion);
                activity?.SetTag("model.accuracy", _statistics.AccuracyScore);
                activity?.SetTag("alerts.count", alerts.Count);
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        private IReadOnlyList<string> CheckForAlerts()
        {
            var alerts = new List<string>();

            const double MinAcceptableAccuracy = 0.70;
            const double MinAcceptableF1Score = 0.65;
            const double MinAcceptableConfidence = 0.60;
            const double MaxAcceptablePredictionTimeMs = 100.0;

            if (_statistics.AccuracyScore < MinAcceptableAccuracy)
            {
                alerts.Add($"Accuracy ({_statistics.AccuracyScore:P2}) is below threshold ({MinAcceptableAccuracy:P2})");
            }

            if (_statistics.F1Score < MinAcceptableF1Score)
            {
                alerts.Add($"F1 Score ({_statistics.F1Score:P2}) is below threshold ({MinAcceptableF1Score:P2})");
            }

            if (_statistics.ModelConfidence < MinAcceptableConfidence)
            {
                alerts.Add($"Model confidence ({_statistics.ModelConfidence:P2}) is below threshold ({MinAcceptableConfidence:P2})");
            }

            if (_statistics.AveragePredictionTime.TotalMilliseconds > MaxAcceptablePredictionTimeMs)
            {
                alerts.Add($"Average prediction time ({_statistics.AveragePredictionTime.TotalMilliseconds:F2}ms) exceeds threshold ({MaxAcceptablePredictionTimeMs}ms)");
            }

            // Check if model is stale (not retrained in 30 days)
            var daysSinceRetraining = (DateTime.UtcNow - _statistics.LastRetraining).TotalDays;
            if (daysSinceRetraining > 30)
            {
                alerts.Add($"Model has not been retrained in {daysSinceRetraining:F0} days");
            }

            return alerts;
        }
    }
}