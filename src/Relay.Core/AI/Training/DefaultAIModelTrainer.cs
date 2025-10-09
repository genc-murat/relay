using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Models;

namespace Relay.Core.AI
{
    /// <summary>
    /// Default implementation of AI model trainer with ML.NET integration.
    /// </summary>
    internal class DefaultAIModelTrainer : IAIModelTrainer, IDisposable
    {
        private readonly ILogger<DefaultAIModelTrainer> _logger;
        private readonly MLNetModelManager _mlNetManager;
        private long _totalTrainingSessions = 0;
        private DateTime _lastTrainingDate = DateTime.MinValue;
        private bool _disposed = false;

        // Training data quality thresholds
        private const int MinimumExecutionSamples = 10;
        private const int MinimumOptimizationSamples = 5;
        private const int MinimumSystemLoadSamples = 10;
        private const double MinimumDataQuality = 0.7;

        public DefaultAIModelTrainer(ILogger<DefaultAIModelTrainer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var mlLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<MLNetModelManager>.Instance;
            _mlNetManager = new MLNetModelManager(mlLogger, modelStoragePath: null);
        }

        public ValueTask TrainModelAsync(AITrainingData trainingData, CancellationToken cancellationToken = default)
        {
            return TrainModelAsync(trainingData, null, cancellationToken);
        }

        public async ValueTask TrainModelAsync(AITrainingData trainingData, TrainingProgressCallback? progressCallback, CancellationToken cancellationToken = default)
        {
            if (trainingData == null)
                throw new ArgumentNullException(nameof(trainingData));

            _totalTrainingSessions++;
            var sessionId = _totalTrainingSessions;
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("AI model training session #{Session} started with {ExecutionCount} execution samples, {OptimizationCount} optimization samples, and {SystemLoadCount} system load samples",
                sessionId,
                trainingData.ExecutionHistory?.Length ?? 0,
                trainingData.OptimizationHistory?.Length ?? 0,
                trainingData.SystemLoadHistory?.Length ?? 0);

            try
            {
                // 1. Validate training data quality
                ReportProgress(progressCallback, TrainingPhase.Validation, 0, "Validating training data...", trainingData, startTime);

                var validationResult = ValidateTrainingData(trainingData);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Training data validation failed for session #{Session}: {Reason}",
                        sessionId, validationResult.ValidationMessage);
                    ReportProgress(progressCallback, TrainingPhase.Completed, 100, $"Validation failed: {validationResult.ValidationMessage}", trainingData, startTime);
                    return;
                }

                _logger.LogInformation("Training data validation passed with quality score: {QualityScore:F2}",
                    validationResult.QualityScore);

                // 2. Train ML.NET models for performance prediction
                ReportProgress(progressCallback, TrainingPhase.PerformanceModels, 20, "Training performance prediction models...", trainingData, startTime);
                await TrainPerformanceModelsAsync(trainingData, cancellationToken);

                // 3. Train optimization strategy classifiers
                ReportProgress(progressCallback, TrainingPhase.OptimizationClassifiers, 40, "Training optimization classifiers...", trainingData, startTime);
                await TrainOptimizationClassifiersAsync(trainingData, cancellationToken);

                // 4. Train anomaly detection models
                ReportProgress(progressCallback, TrainingPhase.AnomalyDetection, 60, "Training anomaly detection models...", trainingData, startTime);
                await TrainAnomalyDetectionModelsAsync(trainingData, cancellationToken);

                // 5. Train time-series forecasting models
                ReportProgress(progressCallback, TrainingPhase.Forecasting, 80, "Training forecasting models...", trainingData, startTime);
                await TrainForecastingModelsAsync(trainingData, cancellationToken);

                // 6. Update model metrics and statistics
                ReportProgress(progressCallback, TrainingPhase.Statistics, 90, "Calculating model statistics...", trainingData, startTime);
                UpdateModelStatistics();

                _lastTrainingDate = DateTime.UtcNow;

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("AI model training session #{Session} completed successfully. Total training time: {Duration}ms",
                    sessionId, duration.TotalMilliseconds);

                ReportProgress(progressCallback, TrainingPhase.Completed, 100, "Training completed successfully", trainingData, startTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI model training session #{Session}", sessionId);
                ReportProgress(progressCallback, TrainingPhase.Completed, 100, $"Training failed: {ex.Message}", trainingData, startTime);
                throw;
            }
        }

        private void ReportProgress(TrainingProgressCallback? callback, TrainingPhase phase, double percentage, string message, AITrainingData trainingData, DateTime startTime)
        {
            if (callback == null) return;

            var totalSamples = (trainingData.ExecutionHistory?.Length ?? 0) +
                             (trainingData.OptimizationHistory?.Length ?? 0) +
                             (trainingData.SystemLoadHistory?.Length ?? 0);

            var progress = new TrainingProgress
            {
                Phase = phase,
                ProgressPercentage = percentage,
                StatusMessage = message,
                SamplesProcessed = (int)(totalSamples * (percentage / 100.0)),
                TotalSamples = totalSamples,
                ElapsedTime = DateTime.UtcNow - startTime
            };

            callback(progress);
        }

        private ValidationResult ValidateTrainingData(AITrainingData trainingData)
        {
            var issues = new List<string>();
            var qualityScore = 1.0;

            // Check minimum sample requirements
            var executionCount = trainingData.ExecutionHistory?.Length ?? 0;
            var optimizationCount = trainingData.OptimizationHistory?.Length ?? 0;
            var systemLoadCount = trainingData.SystemLoadHistory?.Length ?? 0;

            if (executionCount < MinimumExecutionSamples)
            {
                issues.Add($"Insufficient execution samples: {executionCount} (minimum: {MinimumExecutionSamples})");
                qualityScore -= 0.3;
            }

            if (optimizationCount < MinimumOptimizationSamples)
            {
                issues.Add($"Insufficient optimization samples: {optimizationCount} (minimum: {MinimumOptimizationSamples})");
                qualityScore -= 0.2;
            }

            if (systemLoadCount < MinimumSystemLoadSamples)
            {
                issues.Add($"Insufficient system load samples: {systemLoadCount} (minimum: {MinimumSystemLoadSamples})");
                qualityScore -= 0.2;
            }

            // Check data quality
            if (trainingData.ExecutionHistory != null)
            {
                var invalidExecutions = trainingData.ExecutionHistory.Count(e => 
                    e.TotalExecutions <= 0 || e.AverageExecutionTime <= TimeSpan.Zero);
                
                if (invalidExecutions > 0)
                {
                    issues.Add($"Found {invalidExecutions} invalid execution metrics");
                    qualityScore -= 0.1 * (invalidExecutions / (double)executionCount);
                }
            }

            qualityScore = Math.Max(0, qualityScore);

            var isValid = qualityScore >= MinimumDataQuality && issues.Count == 0;
            var message = issues.Count > 0 ? string.Join("; ", issues) : "All validation checks passed";

            return new ValidationResult
            {
                IsValid = isValid,
                QualityScore = qualityScore,
                ValidationMessage = message
            };
        }

        private Task TrainPerformanceModelsAsync(AITrainingData trainingData, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                if (trainingData.ExecutionHistory == null || trainingData.ExecutionHistory.Length == 0)
                    return;

                _logger.LogInformation("Training performance prediction models with {Count} samples", 
                    trainingData.ExecutionHistory.Length);

                // Convert execution metrics to performance data for ML.NET
                var performanceData = trainingData.ExecutionHistory
                    .Where(e => e.TotalExecutions > 0)
                    .Select(e => new PerformanceData
                    {
                        ExecutionTime = (float)e.AverageExecutionTime.TotalMilliseconds,
                        ConcurrencyLevel = e.ConcurrentExecutions,
                        MemoryUsage = e.MemoryUsage,
                        DatabaseCalls = e.DatabaseCalls,
                        ExternalApiCalls = e.ExternalApiCalls,
                        OptimizationGain = CalculateOptimizationGain(e)
                    })
                    .ToList();

                if (performanceData.Count >= MinimumExecutionSamples)
                {
                    _mlNetManager.TrainRegressionModel(performanceData);
                    _logger.LogInformation("Performance regression model trained successfully");
                }
            }, cancellationToken);
        }

        private Task TrainOptimizationClassifiersAsync(AITrainingData trainingData, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                if (trainingData.OptimizationHistory == null || trainingData.OptimizationHistory.Length == 0)
                    return;

                _logger.LogInformation("Training optimization strategy classifiers with {Count} samples",
                    trainingData.OptimizationHistory.Length);

                // Convert optimization results to strategy data for ML.NET
                var strategyData = trainingData.OptimizationHistory
                    .Select(o => new OptimizationStrategyData
                    {
                        ExecutionTime = (float)o.ExecutionTime.TotalMilliseconds,
                        RepeatRate = o.Success ? 0.8f : 0.2f,
                        ConcurrencyLevel = 1.0f,
                        MemoryPressure = 0.5f,
                        ErrorRate = o.Success ? 0.0f : 1.0f,
                        ShouldOptimize = o.Success && o.PerformanceImprovement > 0.1
                    })
                    .ToList();

                if (strategyData.Count >= MinimumOptimizationSamples)
                {
                    _mlNetManager.TrainClassificationModel(strategyData);
                    _logger.LogInformation("Optimization classification model trained successfully");
                }
            }, cancellationToken);
        }

        private Task TrainAnomalyDetectionModelsAsync(AITrainingData trainingData, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                if (trainingData.SystemLoadHistory == null || trainingData.SystemLoadHistory.Length == 0)
                    return;

                _logger.LogInformation("Training anomaly detection models with {Count} samples",
                    trainingData.SystemLoadHistory.Length);

                // Convert system load metrics to metric data for anomaly detection
                var metricData = trainingData.SystemLoadHistory
                    .OrderBy(s => s.Timestamp)
                    .Select(s => new MetricData
                    {
                        Timestamp = s.Timestamp,
                        Value = (float)s.CpuUtilization
                    })
                    .ToList();

                if (metricData.Count >= MinimumSystemLoadSamples)
                {
                    _mlNetManager.TrainAnomalyDetectionModel(metricData);
                    _logger.LogInformation("Anomaly detection model trained successfully");
                }
            }, cancellationToken);
        }

        private Task TrainForecastingModelsAsync(AITrainingData trainingData, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                if (trainingData.SystemLoadHistory == null || trainingData.SystemLoadHistory.Length < 30)
                    return;

                _logger.LogInformation("Training time-series forecasting models with {Count} samples",
                    trainingData.SystemLoadHistory.Length);

                // Convert system load to time series for forecasting
                var timeSeriesData = trainingData.SystemLoadHistory
                    .OrderBy(s => s.Timestamp)
                    .Select(s => new MetricData
                    {
                        Timestamp = s.Timestamp,
                        Value = (float)s.ThroughputPerSecond
                    })
                    .ToList();

                var horizon = Math.Min(12, timeSeriesData.Count / 5); // 20% of data as forecast horizon
                if (horizon >= 3)
                {
                    _mlNetManager.TrainForecastingModel(timeSeriesData, horizon);
                    _logger.LogInformation("Time-series forecasting model trained successfully with horizon={Horizon}", horizon);
                }
            }, cancellationToken);
        }

        private void UpdateModelStatistics()
        {
            var featureImportance = _mlNetManager.GetFeatureImportance();
            
            if (featureImportance != null)
            {
                _logger.LogInformation("Model feature importance calculated:");
                foreach (var feature in featureImportance.OrderByDescending(f => f.Value))
                {
                    _logger.LogInformation("  {Feature}: {Importance:P1}", feature.Key, feature.Value);
                }
            }
        }

        private float CalculateOptimizationGain(RequestExecutionMetrics metrics)
        {
            // Calculate optimization gain based on multiple factors
            var successRate = metrics.SuccessRate;
            var avgTime = (float)metrics.AverageExecutionTime.TotalMilliseconds;
            var p95Time = (float)metrics.P95ExecutionTime.TotalMilliseconds;
            
            // Higher success rate and lower execution time = higher optimization potential
            var timeBasedGain = p95Time > 0 ? Math.Min(1.0f, 1000.0f / p95Time) : 0.5f;
            var reliabilityGain = (float)successRate;
            
            return (timeBasedGain * 0.6f) + (reliabilityGain * 0.4f);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _mlNetManager?.Dispose();

            _logger.LogInformation("DefaultAIModelTrainer disposed. Total training sessions: {Sessions}", 
                _totalTrainingSessions);
        }

        private class ValidationResult
        {
            public bool IsValid { get; set; }
            public double QualityScore { get; set; }
            public string ValidationMessage { get; set; } = string.Empty;
        }
    }
}
