using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.Core.AI
{
    /// <summary>
    /// Health check for AI Metrics export system.
    /// </summary>
    internal class AIMetricsHealthCheck
    {
        private readonly IAIMetricsExporter _exporter;
        private readonly ILogger<AIMetricsHealthCheck> _logger;
        private readonly AIHealthCheckOptions _options;

        public AIMetricsHealthCheck(
            IAIMetricsExporter exporter,
            ILogger<AIMetricsHealthCheck> logger,
            IOptions<AIHealthCheckOptions> options)
        {
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new AIHealthCheckOptions();
        }

        public async Task<ComponentHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ComponentHealthResult
            {
                ComponentName = "AI Metrics Exporter",
                Data = new Dictionary<string, object>()
            };

            try
            {
                // Test metrics export with dummy data
                var testStatistics = new AIModelStatistics
                {
                    AccuracyScore = 0.85,
                    PrecisionScore = 0.83,
                    RecallScore = 0.87,
                    F1Score = 0.85,
                    ModelConfidence = 0.80,
                    TotalPredictions = 0,
                    AveragePredictionTime = TimeSpan.FromMilliseconds(50),
                    ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                    LastRetraining = DateTime.UtcNow.AddDays(-7),
                    TrainingDataPoints = 1000,
                    ModelVersion = "health-check-test"
                };

                await _exporter.ExportMetricsAsync(testStatistics, cancellationToken);

                result.IsHealthy = true;
                result.Status = "Operational";
                result.HealthScore = 1.0;
                result.Description = "Metrics exporter is operational";
                result.Data["TestExportSuccessful"] = true;

                _logger.LogDebug("AI Metrics Exporter health check: {Status}", result.Status);
            }
            catch (Exception ex)
            {
                result.IsHealthy = false;
                result.Status = "Failed";
                result.Description = $"Metrics export failed: {ex.Message}";
                result.Exception = ex;
                result.Errors.Add(ex.Message);
                result.HealthScore = 0.0;

                _logger.LogError(ex, "AI Metrics Exporter health check failed");
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }

            return result;
        }
    }
}
