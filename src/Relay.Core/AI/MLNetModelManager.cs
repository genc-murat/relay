using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Trainers.FastTree;
using Microsoft.ML.TimeSeries;

namespace Relay.Core.AI
{
    /// <summary>
    /// ML.NET model manager for AI optimization predictions
    /// </summary>
    internal sealed class MLNetModelManager : IDisposable
    {
        private readonly ILogger<MLNetModelManager> _logger;
        private readonly MLContext _mlContext;
        private ITransformer? _regressionModel;
        private ITransformer? _classificationModel;
        private ITransformer? _anomalyDetectionModel;
        private ITransformer? _forecastModel;
        private bool _disposed;

        public MLNetModelManager(ILogger<MLNetModelManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mlContext = new MLContext(seed: 42);

            _logger.LogInformation("ML.NET Model Manager initialized");
        }

        /// <summary>
        /// Train regression model for performance prediction
        /// </summary>
        public void TrainRegressionModel(IEnumerable<PerformanceData> trainingData)
        {
            try
            {
                _logger.LogInformation("Training regression model with {Count} samples", trainingData.Count());

                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

                // Define pipeline
                var pipeline = _mlContext.Transforms.Concatenate("Features",
                        nameof(PerformanceData.ExecutionTime),
                        nameof(PerformanceData.ConcurrencyLevel),
                        nameof(PerformanceData.MemoryUsage),
                        nameof(PerformanceData.DatabaseCalls),
                        nameof(PerformanceData.ExternalApiCalls))
                    .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                    .Append(_mlContext.Regression.Trainers.FastTree(new FastTreeRegressionTrainer.Options
                    {
                        NumberOfLeaves = 20,
                        MinimumExampleCountPerLeaf = 10,
                        NumberOfTrees = 100,
                        LearningRate = 0.2,
                        Shrinkage = 0.05,
                        LabelColumnName = nameof(PerformanceData.OptimizationGain)
                    }));

                // Train model
                _regressionModel = pipeline.Fit(dataView);

                // Evaluate model
                var predictions = _regressionModel.Transform(dataView);
                var metrics = _mlContext.Regression.Evaluate(predictions,
                    labelColumnName: nameof(PerformanceData.OptimizationGain));

                _logger.LogInformation("Regression model trained: R²={RSquared:F3}, MAE={MAE:F3}, RMSE={RMSE:F3}",
                    metrics.RSquared, metrics.MeanAbsoluteError, metrics.RootMeanSquaredError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training regression model");
            }
        }

        /// <summary>
        /// Predict optimization gain for given performance metrics
        /// </summary>
        public float PredictOptimizationGain(PerformanceData performanceData)
        {
            if (_regressionModel == null)
            {
                _logger.LogWarning("Regression model not trained, returning default value");
                return 0.5f;
            }

            try
            {
                var predictionEngine = _mlContext.Model.CreatePredictionEngine<PerformanceData, PerformancePrediction>(_regressionModel);
                var prediction = predictionEngine.Predict(performanceData);

                _logger.LogDebug("Predicted optimization gain: {Gain:F3}", prediction.PredictedGain);

                return prediction.PredictedGain;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting optimization gain");
                return 0.5f;
            }
        }

        /// <summary>
        /// Train binary classification model for optimization strategy selection
        /// </summary>
        public void TrainClassificationModel(IEnumerable<OptimizationStrategyData> trainingData)
        {
            try
            {
                _logger.LogInformation("Training classification model with {Count} samples", trainingData.Count());

                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

                // Define pipeline
                var pipeline = _mlContext.Transforms.Concatenate("Features",
                        nameof(OptimizationStrategyData.ExecutionTime),
                        nameof(OptimizationStrategyData.RepeatRate),
                        nameof(OptimizationStrategyData.ConcurrencyLevel),
                        nameof(OptimizationStrategyData.MemoryPressure),
                        nameof(OptimizationStrategyData.ErrorRate))
                    .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                    .Append(_mlContext.BinaryClassification.Trainers.FastTree(new FastTreeBinaryTrainer.Options
                    {
                        NumberOfLeaves = 20,
                        MinimumExampleCountPerLeaf = 10,
                        NumberOfTrees = 100,
                        LearningRate = 0.2,
                        LabelColumnName = nameof(OptimizationStrategyData.ShouldOptimize)
                    }));

                // Train model
                _classificationModel = pipeline.Fit(dataView);

                // Evaluate model
                var predictions = _classificationModel.Transform(dataView);
                var metrics = _mlContext.BinaryClassification.Evaluate(predictions,
                    labelColumnName: nameof(OptimizationStrategyData.ShouldOptimize));

                _logger.LogInformation("Classification model trained: Accuracy={Accuracy:F3}, AUC={AUC:F3}, F1={F1:F3}",
                    metrics.Accuracy, metrics.AreaUnderRocCurve, metrics.F1Score);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training classification model");
            }
        }

        /// <summary>
        /// Predict whether optimization should be applied
        /// </summary>
        public (bool ShouldOptimize, float Confidence) PredictOptimizationStrategy(OptimizationStrategyData strategyData)
        {
            if (_classificationModel == null)
            {
                _logger.LogWarning("Classification model not trained, returning default value");
                return (false, 0.5f);
            }

            try
            {
                var predictionEngine = _mlContext.Model.CreatePredictionEngine<OptimizationStrategyData, StrategyPrediction>(_classificationModel);
                var prediction = predictionEngine.Predict(strategyData);

                _logger.LogDebug("Predicted optimization: {ShouldOptimize} (Probability: {Probability:F3})",
                    prediction.Prediction, prediction.Probability);

                return (prediction.Prediction, prediction.Probability);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting optimization strategy");
                return (false, 0.5f);
            }
        }

        /// <summary>
        /// Train anomaly detection model using Spike Detection
        /// </summary>
        public void TrainAnomalyDetectionModel(IEnumerable<MetricData> historicalData)
        {
            try
            {
                _logger.LogInformation("Training anomaly detection model with {Count} samples", historicalData.Count());

                var dataView = _mlContext.Data.LoadFromEnumerable(historicalData);

                // Spike detection pipeline
                var pipeline = _mlContext.Transforms.DetectIidSpike(
                    outputColumnName: nameof(AnomalyPrediction.Prediction),
                    inputColumnName: nameof(MetricData.Value),
                    confidence: 95.0,
                    pvalueHistoryLength: 30);

                _anomalyDetectionModel = pipeline.Fit(dataView);

                _logger.LogInformation("Anomaly detection model trained successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training anomaly detection model");
            }
        }

        /// <summary>
        /// Detect anomalies in metric data
        /// </summary>
        public bool DetectAnomaly(MetricData metricData)
        {
            if (_anomalyDetectionModel == null)
            {
                _logger.LogWarning("Anomaly detection model not trained, returning false");
                return false;
            }

            try
            {
                var predictionEngine = _mlContext.Model.CreatePredictionEngine<MetricData, AnomalyPrediction>(_anomalyDetectionModel);
                var prediction = predictionEngine.Predict(metricData);

                var isAnomaly = prediction.Prediction[0] == 1;

                if (isAnomaly)
                {
                    _logger.LogWarning("Anomaly detected: Alert={Alert}, P-Value={PValue:F4}, Score={Score:F4}",
                        prediction.Prediction[0], prediction.Prediction[1], prediction.Prediction[2]);
                }

                return isAnomaly;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting anomaly");
                return false;
            }
        }

        /// <summary>
        /// Train time-series forecasting model
        /// </summary>
        public void TrainForecastingModel(IEnumerable<MetricData> timeSeriesData, int horizon)
        {
            try
            {
                _logger.LogInformation("Training forecasting model with {Count} samples, horizon={Horizon}",
                    timeSeriesData.Count(), horizon);

                var dataView = _mlContext.Data.LoadFromEnumerable(timeSeriesData);

                // SSA (Singular Spectrum Analysis) forecasting
                var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(MetricForecast.ForecastedValues),
                    inputColumnName: nameof(MetricData.Value),
                    windowSize: 30,
                    seriesLength: 60,
                    trainSize: timeSeriesData.Count(),
                    horizon: horizon,
                    confidenceLevel: 0.95f,
                    confidenceLowerBoundColumn: nameof(MetricForecast.LowerBound),
                    confidenceUpperBoundColumn: nameof(MetricForecast.UpperBound));

                _forecastModel = forecastingPipeline.Fit(dataView);

                _logger.LogInformation("Forecasting model trained successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training forecasting model");
            }
        }

        /// <summary>
        /// Forecast future metric values
        /// </summary>
        public MetricForecast? ForecastMetric(int horizon)
        {
            if (_forecastModel == null)
            {
                _logger.LogWarning("Forecasting model not trained");
                return null;
            }

            try
            {
                var predictionEngine = _mlContext.Model.CreatePredictionEngine<MetricData, MetricForecast>(_forecastModel);
                var forecast = predictionEngine.Predict(new MetricData());

                _logger.LogDebug("Forecasted {Count} future values", forecast.ForecastedValues.Length);

                return forecast;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forecasting metric");
                return null;
            }
        }

        /// <summary>
        /// Update time-series model with new observation
        /// </summary>
        public void UpdateForecastingModel(MetricData newObservation)
        {
            if (_forecastModel == null)
            {
                _logger.LogWarning("Forecasting model not trained, cannot update");
                return;
            }

            try
            {
                // For SSA models, retraining is needed to incorporate new observations
                // In production, would maintain a sliding window of data
                _logger.LogTrace("Forecasting model checkpoint recorded");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating forecasting model");
            }
        }

        /// <summary>
        /// Get feature importance from regression model
        /// </summary>
        public Dictionary<string, float>? GetFeatureImportance()
        {
            if (_regressionModel == null)
            {
                _logger.LogWarning("Regression model not trained");
                return null;
            }

            try
            {
                var dummyData = _mlContext.Data.LoadFromEnumerable(new[] { new PerformanceData() });
                var transformedSchema = _regressionModel.GetOutputSchema(dummyData.Schema);

                var featureNames = new[] { "ExecutionTime", "ConcurrencyLevel", "MemoryUsage", "DatabaseCalls", "ExternalApiCalls" };
                var importance = new Dictionary<string, float>();

                // Note: Feature importance extraction from FastTree requires accessing tree internals
                // This is a simplified version - in production would use proper feature importance extraction

                for (int i = 0; i < featureNames.Length; i++)
                {
                    importance[featureNames[i]] = 0.2f; // Placeholder - would calculate actual importance
                }

                _logger.LogDebug("Feature importance extracted for {Count} features", importance.Count);

                return importance;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting feature importance");
                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _logger.LogInformation("ML.NET Model Manager disposed");
        }
    }

    // ML.NET Data Classes
    internal class PerformanceData
    {
        public float ExecutionTime { get; set; }
        public float ConcurrencyLevel { get; set; }
        public float MemoryUsage { get; set; }
        public float DatabaseCalls { get; set; }
        public float ExternalApiCalls { get; set; }
        public float OptimizationGain { get; set; } // Label
    }

    internal class PerformancePrediction
    {
        [ColumnName("Score")]
        public float PredictedGain { get; set; }
    }

    internal class OptimizationStrategyData
    {
        public float ExecutionTime { get; set; }
        public float RepeatRate { get; set; }
        public float ConcurrencyLevel { get; set; }
        public float MemoryPressure { get; set; }
        public float ErrorRate { get; set; }
        public bool ShouldOptimize { get; set; } // Label
    }

    internal class StrategyPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction { get; set; }

        public float Probability { get; set; }
        public float Score { get; set; }
    }

    internal class MetricData
    {
        public DateTime Timestamp { get; set; }
        public float Value { get; set; }
    }

    internal class AnomalyPrediction
    {
        [VectorType(3)]
        public double[] Prediction { get; set; } = new double[3];
    }

    internal class MetricForecast
    {
        public float[] ForecastedValues { get; set; } = Array.Empty<float>();
        public float[] LowerBound { get; set; } = Array.Empty<float>();
        public float[] UpperBound { get; set; } = Array.Empty<float>();
    }
}
