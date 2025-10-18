using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Alerting-based metrics export strategy.
    /// </summary>
    internal class AlertingMetricsExportStrategy : IMetricsExportStrategy
    {
        private readonly ILogger _logger;

        // Alert thresholds
        private const double MinAcceptableAccuracy = 0.70;
        private const double MinAcceptableF1Score = 0.65;
        private const double MinAcceptableConfidence = 0.60;
        private const double MaxAcceptablePredictionTimeMs = 100.0;

        public string Name => "Alerting";

        public AlertingMetricsExportStrategy(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ValueTask ExportAsync(AIModelStatistics statistics, CancellationToken cancellationToken = default)
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
    }
}