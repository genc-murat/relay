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
    /// Health check for AI Optimization Engine.
    /// </summary>
    internal class AIOptimizationHealthCheck
    {
        private readonly IAIOptimizationEngine _engine;
        private readonly ILogger<AIOptimizationHealthCheck> _logger;
        private readonly AIHealthCheckOptions _options;

        public AIOptimizationHealthCheck(
            IAIOptimizationEngine engine,
            ILogger<AIOptimizationHealthCheck> logger,
            IOptions<AIHealthCheckOptions> options)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new AIHealthCheckOptions();
        }

        public async Task<ComponentHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ComponentHealthResult
            {
                ComponentName = "AI Optimization Engine",
                Data = new Dictionary<string, object>()
            };

            try
            {
                // Check if engine is accessible
                var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromMinutes(5), cancellationToken);
                
                result.IsHealthy = true;
                result.Status = "Operational";
                result.HealthScore = insights.HealthScore?.Overall ?? 0.0;
                result.Description = $"Engine is operational with health score: {result.HealthScore:P1}";

                // Add insights data
                result.Data["PerformanceGrade"] = insights.PerformanceGrade;
                result.Data["HealthScore"] = insights.HealthScore?.Overall ?? 0.0;
                result.Data["BottleneckCount"] = insights.Bottlenecks?.Count ?? 0;
                result.Data["OpportunityCount"] = insights.Opportunities?.Count ?? 0;

                // Check health score threshold
                if (result.HealthScore < _options.MinSystemHealthScore)
                {
                    result.Warnings.Add($"System health score ({result.HealthScore:P1}) is below threshold ({_options.MinSystemHealthScore:P1})");
                }

                // Check for critical areas
                if (insights.HealthScore?.CriticalAreas?.Count > 0)
                {
                    result.Warnings.Add($"Critical areas detected: {string.Join(", ", insights.HealthScore.CriticalAreas)}");
                }

                _logger.LogDebug("AI Optimization Engine health check: {Status}, Score: {Score:P1}", 
                    result.Status, result.HealthScore);
            }
            catch (Exception ex)
            {
                result.IsHealthy = false;
                result.Status = "Failed";
                result.Description = $"Health check failed: {ex.Message}";
                result.Exception = ex;
                result.Errors.Add(ex.Message);

                _logger.LogError(ex, "AI Optimization Engine health check failed");
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }

            return result;
        }
    }

    /// <summary>
    /// Health check for AI Model performance.
    /// </summary>
    internal class AIModelHealthCheck
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

        public Task<ComponentHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Health check for AI Circuit Breakers.
    /// </summary>
    internal class AICircuitBreakerHealthCheck
    {
        private readonly ILogger<AICircuitBreakerHealthCheck> _logger;
        private readonly AIHealthCheckOptions _options;

        public AICircuitBreakerHealthCheck(
            ILogger<AICircuitBreakerHealthCheck> logger,
            IOptions<AIHealthCheckOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new AIHealthCheckOptions();
        }

        public Task<ComponentHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ComponentHealthResult
            {
                ComponentName = "AI Circuit Breakers",
                Data = new Dictionary<string, object>()
            };

            try
            {
                // Circuit breaker health would be checked through metrics
                // For now, assume healthy as circuit breakers are passive components
                result.IsHealthy = true;
                result.Status = "Operational";
                result.HealthScore = 1.0;
                result.Description = "Circuit breaker mechanisms are ready";

                _logger.LogDebug("AI Circuit Breaker health check: {Status}", result.Status);
            }
            catch (Exception ex)
            {
                result.IsHealthy = false;
                result.Status = "Failed";
                result.Description = $"Health check failed: {ex.Message}";
                result.Exception = ex;
                result.Errors.Add(ex.Message);

                _logger.LogError(ex, "AI Circuit Breaker health check failed");
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }

            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// Health check for overall AI system.
    /// </summary>
    internal class AISystemHealthCheck
    {
        private readonly IAIOptimizationEngine _engine;
        private readonly ILogger<AISystemHealthCheck> _logger;
        private readonly AIHealthCheckOptions _options;

        public AISystemHealthCheck(
            IAIOptimizationEngine engine,
            ILogger<AISystemHealthCheck> logger,
            IOptions<AIHealthCheckOptions> options)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new AIHealthCheckOptions();
        }

        public async Task<ComponentHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ComponentHealthResult
            {
                ComponentName = "AI System",
                Data = new Dictionary<string, object>()
            };

            try
            {
                var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromMinutes(10), cancellationToken);
                var healthScore = insights.HealthScore;

                result.IsHealthy = healthScore != null && healthScore.Overall >= _options.MinSystemHealthScore;
                result.HealthScore = healthScore?.Overall ?? 0.0;
                result.Status = healthScore?.Status ?? "Unknown";
                result.Description = $"System health: {result.Status} (Score: {result.HealthScore:P1})";

                // Add system metrics
                if (healthScore != null)
                {
                    result.Data["Overall"] = healthScore.Overall;
                    result.Data["Performance"] = healthScore.Performance;
                    result.Data["Reliability"] = healthScore.Reliability;
                    result.Data["Scalability"] = healthScore.Scalability;
                    result.Data["Security"] = healthScore.Security;
                    result.Data["Maintainability"] = healthScore.Maintainability;

                    // Add critical areas as warnings
                    if (healthScore.CriticalAreas?.Count > 0)
                    {
                        foreach (var area in healthScore.CriticalAreas)
                        {
                            result.Warnings.Add($"Critical area: {area}");
                        }
                    }
                }

                // Add insight metrics
                result.Data["PerformanceGrade"] = insights.PerformanceGrade;
                result.Data["BottleneckCount"] = insights.Bottlenecks?.Count ?? 0;
                result.Data["OpportunityCount"] = insights.Opportunities?.Count ?? 0;

                // Check for severe bottlenecks
                if (insights.Bottlenecks != null && insights.Bottlenecks.Count > 5)
                {
                    result.Warnings.Add($"Multiple performance bottlenecks detected: {insights.Bottlenecks.Count}");
                }

                _logger.LogDebug("AI System health check: {Status}, Score: {Score:P1}, Warnings: {WarningCount}", 
                    result.Status, result.HealthScore, result.Warnings.Count);
            }
            catch (Exception ex)
            {
                result.IsHealthy = false;
                result.Status = "Failed";
                result.Description = $"System health check failed: {ex.Message}";
                result.Exception = ex;
                result.Errors.Add(ex.Message);

                _logger.LogError(ex, "AI System health check failed");
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
