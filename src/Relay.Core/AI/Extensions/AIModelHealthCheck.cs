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
    /// Health check for AI Model performance.
    /// </summary>
    public class AIModelHealthCheck
    {
        private readonly IAIOptimizationEngine _engine;
        private readonly ILogger<AIModelHealthCheck> _logger;
        private readonly AIHealthCheckOptions _options;

        public AIModelHealthCheck(
            IAIOptimizationEngine engine,
            ILogger<AIModelHealthCheck> logger,
            IOptions<AIHealthCheckOptions> options)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new AIHealthCheckOptions();
        }

        public virtual Task<ComponentHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ComponentHealthResult
            {
                ComponentName = "AI Model",
                Data = new Dictionary<string, object>()
            };

            try
            {
                var stats = _engine.GetModelStatistics();

                result.IsHealthy = true;
                result.Status = "Operational";
                
                // Calculate weighted health score
                var healthScore = CalculateModelHealthScore(stats);
                result.HealthScore = healthScore;
                result.Description = $"Model performance: Accuracy={stats.AccuracyScore:P1}, F1={stats.F1Score:P1}";

                // Add model statistics to data
                result.Data["Accuracy"] = stats.AccuracyScore;
                result.Data["Precision"] = stats.PrecisionScore;
                result.Data["Recall"] = stats.RecallScore;
                result.Data["F1Score"] = stats.F1Score;
                result.Data["Confidence"] = stats.ModelConfidence;
                result.Data["TotalPredictions"] = stats.TotalPredictions;
                result.Data["AvgPredictionTime"] = stats.AveragePredictionTime.TotalMilliseconds;
                result.Data["ModelVersion"] = stats.ModelVersion;
                result.Data["LastRetraining"] = stats.LastRetraining;

                // Check accuracy threshold
                if (stats.AccuracyScore < _options.MinAccuracyScore)
                {
                    result.Warnings.Add($"Accuracy ({stats.AccuracyScore:P1}) is below threshold ({_options.MinAccuracyScore:P1})");
                    result.IsHealthy = false;
                }

                // Check F1 score threshold
                if (stats.F1Score < _options.MinF1Score)
                {
                    result.Warnings.Add($"F1 Score ({stats.F1Score:P1}) is below threshold ({_options.MinF1Score:P1})");
                    result.IsHealthy = false;
                }

                // Check confidence threshold
                if (stats.ModelConfidence < _options.MinConfidence)
                {
                    result.Warnings.Add($"Model confidence ({stats.ModelConfidence:P1}) is below threshold ({_options.MinConfidence:P1})");
                }

                // Check prediction time
                if (stats.AveragePredictionTime.TotalMilliseconds > _options.MaxPredictionTimeMs)
                {
                    result.Warnings.Add($"Average prediction time ({stats.AveragePredictionTime.TotalMilliseconds:F1}ms) exceeds threshold ({_options.MaxPredictionTimeMs}ms)");
                }

                // Check model staleness
                var daysSinceRetraining = (DateTime.UtcNow - stats.LastRetraining).TotalDays;
                if (daysSinceRetraining > _options.MaxDaysSinceRetraining)
                {
                    result.Warnings.Add($"Model has not been retrained in {daysSinceRetraining:F0} days (threshold: {_options.MaxDaysSinceRetraining} days)");
                }

                result.Status = result.IsHealthy ? "Healthy" : "Degraded";

                _logger.LogDebug("AI Model health check: {Status}, Score: {Score:P1}, Warnings: {WarningCount}", 
                    result.Status, result.HealthScore, result.Warnings.Count);
            }
            catch (Exception ex)
            {
                result.IsHealthy = false;
                result.Status = "Failed";
                result.Description = $"Health check failed: {ex.Message}";
                result.Exception = ex;
                result.Errors.Add(ex.Message);

                _logger.LogError(ex, "AI Model health check failed");
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }

            return Task.FromResult(result);
        }

        private double CalculateModelHealthScore(AIModelStatistics stats)
        {
            // Weighted scoring
            var accuracyWeight = 0.30;
            var precisionWeight = 0.20;
            var recallWeight = 0.20;
            var f1Weight = 0.20;
            var confidenceWeight = 0.10;

            var score = (stats.AccuracyScore * accuracyWeight) +
                       (stats.PrecisionScore * precisionWeight) +
                       (stats.RecallScore * recallWeight) +
                       (stats.F1Score * f1Weight) +
                       (stats.ModelConfidence * confidenceWeight);

            // Apply penalties
            if (stats.AveragePredictionTime.TotalMilliseconds > _options.MaxPredictionTimeMs)
            {
                score *= 0.9; // 10% penalty for slow predictions
            }

            var daysSinceRetraining = (DateTime.UtcNow - stats.LastRetraining).TotalDays;
            if (daysSinceRetraining > _options.MaxDaysSinceRetraining)
            {
                score *= 0.95; // 5% penalty for stale model
            }

            return Math.Max(0.0, Math.Min(1.0, score));
        }
    }
}
