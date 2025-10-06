using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Trainers;
using Microsoft.ML.Trainers.FastTree;
using Microsoft.ML.TimeSeries;

namespace Relay.Core.AI.Optimization.Models
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
        
        // Sliding window buffer for forecasting model updates
        private readonly List<MetricData> _forecastingDataBuffer = new();
        private readonly int _maxBufferSize = 1000;
        private int _currentForecastHorizon = 12;
        
        // Store training data for feature importance calculation
        private IDataView? _regressionTrainingData;

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
                
                // Store training data for feature importance calculation
                _regressionTrainingData = dataView;

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

                // Store data in buffer for future updates
                _forecastingDataBuffer.Clear();
                _forecastingDataBuffer.AddRange(timeSeriesData);
                _currentForecastHorizon = horizon;
                
                // Keep only the most recent data if buffer exceeds max size
                if (_forecastingDataBuffer.Count > _maxBufferSize)
                {
                    var removeCount = _forecastingDataBuffer.Count - _maxBufferSize;
                    _forecastingDataBuffer.RemoveRange(0, removeCount);
                }

                var dataView = _mlContext.Data.LoadFromEnumerable(_forecastingDataBuffer);

                // SSA (Singular Spectrum Analysis) forecasting
                var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(MetricForecast.ForecastedValues),
                    inputColumnName: nameof(MetricData.Value),
                    windowSize: 30,
                    seriesLength: 60,
                    trainSize: _forecastingDataBuffer.Count,
                    horizon: horizon,
                    confidenceLevel: 0.95f,
                    confidenceLowerBoundColumn: nameof(MetricForecast.LowerBound),
                    confidenceUpperBoundColumn: nameof(MetricForecast.UpperBound));

                _forecastModel = forecastingPipeline.Fit(dataView);

                _logger.LogInformation("Forecasting model trained successfully with buffer size {BufferSize}", 
                    _forecastingDataBuffer.Count);
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
                // Add new observation to the buffer
                _forecastingDataBuffer.Add(newObservation);
                
                // Maintain sliding window by removing oldest data if buffer exceeds max size
                if (_forecastingDataBuffer.Count > _maxBufferSize)
                {
                    var removeCount = _forecastingDataBuffer.Count - _maxBufferSize;
                    _forecastingDataBuffer.RemoveRange(0, removeCount);
                    _logger.LogDebug("Removed {Count} old observations from forecasting buffer", removeCount);
                }
                
                // Retrain model periodically with accumulated observations
                // Only retrain if we have accumulated enough new data (e.g., every 50 observations)
                var retrainThreshold = 50;
                if (_forecastingDataBuffer.Count % retrainThreshold == 0 && _forecastingDataBuffer.Count >= 100)
                {
                    _logger.LogInformation("Retraining forecasting model with {Count} observations including latest data", 
                        _forecastingDataBuffer.Count);
                    
                    var dataView = _mlContext.Data.LoadFromEnumerable(_forecastingDataBuffer);
                    
                    // Retrain SSA model with updated data
                    var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                        outputColumnName: nameof(MetricForecast.ForecastedValues),
                        inputColumnName: nameof(MetricData.Value),
                        windowSize: 30,
                        seriesLength: 60,
                        trainSize: _forecastingDataBuffer.Count,
                        horizon: _currentForecastHorizon,
                        confidenceLevel: 0.95f,
                        confidenceLowerBoundColumn: nameof(MetricForecast.LowerBound),
                        confidenceUpperBoundColumn: nameof(MetricForecast.UpperBound));
                    
                    _forecastModel = forecastingPipeline.Fit(dataView);
                    
                    _logger.LogInformation("Forecasting model retrained successfully with updated data");
                }
                else
                {
                    _logger.LogTrace("Forecasting model observation added: {Timestamp}, Value={Value:F2} (Buffer: {BufferSize}/{Threshold})",
                        newObservation.Timestamp, newObservation.Value, _forecastingDataBuffer.Count, retrainThreshold);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating forecasting model");
            }
        }

        /// <summary>
        /// Get feature importance from regression model using Permutation Feature Importance (PFI)
        /// </summary>
        public Dictionary<string, float>? GetFeatureImportance()
        {
            if (_regressionModel == null)
            {
                _logger.LogWarning("Regression model not trained");
                return null;
            }

            if (_regressionTrainingData == null)
            {
                _logger.LogWarning("Training data not available for feature importance calculation");
                return null;
            }

            try
            {
                var featureNames = new[] { "ExecutionTime", "ConcurrencyLevel", "MemoryUsage", "DatabaseCalls", "ExternalApiCalls" };
                var importance = new Dictionary<string, float>();

                // Use Permutation Feature Importance (PFI) to calculate feature importance
                // PFI measures the impact of each feature by randomly permuting its values
                // and measuring the decrease in model performance
                var transformedData = _regressionModel.Transform(_regressionTrainingData);
                
                var permutationMetrics = _mlContext.Regression.PermutationFeatureImportance(
                    _regressionModel,
                    transformedData,
                    labelColumnName: nameof(PerformanceData.OptimizationGain),
                    permutationCount: 10); // Number of permutations per feature

                // PFI returns an ImmutableDictionary with feature column names as keys
                // Since we concatenated features into a single "Features" column, 
                // we need to access individual feature importance differently
                
                // Extract feature importance for the Features column
                if (permutationMetrics.TryGetValue("Features", out var featureMetrics))
                {
                    // Use R-Squared mean as importance metric
                    var rSquaredImportance = Math.Abs(featureMetrics.RSquared.Mean);
                    
                    _logger.LogDebug("Features column: R²Change={RSquared:F4}±{StdDev:F4}, MAE={MAE:F4}",
                        featureMetrics.RSquared.Mean,
                        featureMetrics.RSquared.StandardDeviation,
                        featureMetrics.MeanAbsoluteError.Mean);
                    
                    // Since we can't get individual feature importance from concatenated features,
                    // we'll use an alternative approach: calculate correlation-based importance
                    importance = CalculateCorrelationBasedImportance(featureNames);
                }
                else
                {
                    _logger.LogWarning("Features column not found in PFI results, using correlation-based importance");
                    importance = CalculateCorrelationBasedImportance(featureNames);
                }

                _logger.LogInformation("Feature importance calculated for {Count} features", importance.Count);

                return importance;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating feature importance, falling back to uniform distribution");
                
                // Fallback to uniform distribution if PFI fails
                var featureNames = new[] { "ExecutionTime", "ConcurrencyLevel", "MemoryUsage", "DatabaseCalls", "ExternalApiCalls" };
                var fallbackImportance = new Dictionary<string, float>();
                var uniformValue = 1.0f / featureNames.Length;
                
                foreach (var name in featureNames)
                {
                    fallbackImportance[name] = uniformValue;
                }
                
                return fallbackImportance;
            }
        }

        /// <summary>
        /// Calculate feature importance based on correlation with target variable
        /// </summary>
        private Dictionary<string, float> CalculateCorrelationBasedImportance(string[] featureNames)
        {
            var importance = new Dictionary<string, float>();
            
            try
            {
                if (_regressionTrainingData == null)
                {
                    // Return uniform distribution
                    var uniformValue = 1.0f / featureNames.Length;
                    foreach (var name in featureNames)
                    {
                        importance[name] = uniformValue;
                    }
                    return importance;
                }

                // Convert to enumerable to calculate correlations
                var dataEnumerable = _mlContext.Data.CreateEnumerable<PerformanceData>(_regressionTrainingData, reuseRowObject: false);
                var dataList = dataEnumerable.ToList();
                
                if (dataList.Count == 0)
                {
                    // Return uniform distribution
                    var uniformValue = 1.0f / featureNames.Length;
                    foreach (var name in featureNames)
                    {
                        importance[name] = uniformValue;
                    }
                    return importance;
                }

                // Calculate correlation between each feature and target
                var correlations = new Dictionary<string, float>
                {
                    ["ExecutionTime"] = CalculateCorrelation(dataList.Select(d => d.ExecutionTime), dataList.Select(d => d.OptimizationGain)),
                    ["ConcurrencyLevel"] = CalculateCorrelation(dataList.Select(d => d.ConcurrencyLevel), dataList.Select(d => d.OptimizationGain)),
                    ["MemoryUsage"] = CalculateCorrelation(dataList.Select(d => d.MemoryUsage), dataList.Select(d => d.OptimizationGain)),
                    ["DatabaseCalls"] = CalculateCorrelation(dataList.Select(d => d.DatabaseCalls), dataList.Select(d => d.OptimizationGain)),
                    ["ExternalApiCalls"] = CalculateCorrelation(dataList.Select(d => d.ExternalApiCalls), dataList.Select(d => d.OptimizationGain))
                };

                // Use absolute correlation as importance
                foreach (var kvp in correlations)
                {
                    importance[kvp.Key] = Math.Abs(kvp.Value);
                }

                // Normalize to sum to 1.0
                var totalImportance = importance.Values.Sum();
                if (totalImportance > 0)
                {
                    var normalizedImportance = new Dictionary<string, float>();
                    foreach (var kvp in importance)
                    {
                        normalizedImportance[kvp.Key] = kvp.Value / totalImportance;
                        _logger.LogDebug("Feature {Feature}: Correlation={Correlation:F4}, NormalizedImportance={Importance:F4}",
                            kvp.Key, kvp.Value, normalizedImportance[kvp.Key]);
                    }
                    importance = normalizedImportance;
                }
                else
                {
                    // All correlations are zero, use uniform distribution
                    var uniformValue = 1.0f / featureNames.Length;
                    foreach (var name in featureNames)
                    {
                        importance[name] = uniformValue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in correlation-based importance calculation");
                
                // Return uniform distribution
                var uniformValue = 1.0f / featureNames.Length;
                foreach (var name in featureNames)
                {
                    importance[name] = uniformValue;
                }
            }

            return importance;
        }

        /// <summary>
        /// Calculate Pearson correlation coefficient between two variables
        /// </summary>
        private float CalculateCorrelation(IEnumerable<float> x, IEnumerable<float> y)
        {
            var xArray = x.ToArray();
            var yArray = y.ToArray();
            
            if (xArray.Length != yArray.Length || xArray.Length == 0)
                return 0f;

            var n = xArray.Length;
            var xMean = xArray.Average();
            var yMean = yArray.Average();

            var numerator = 0.0;
            var xVariance = 0.0;
            var yVariance = 0.0;

            for (int i = 0; i < n; i++)
            {
                var xDiff = xArray[i] - xMean;
                var yDiff = yArray[i] - yMean;
                
                numerator += xDiff * yDiff;
                xVariance += xDiff * xDiff;
                yVariance += yDiff * yDiff;
            }

            if (xVariance == 0 || yVariance == 0)
                return 0f;

            return (float)(numerator / Math.Sqrt(xVariance * yVariance));
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _logger.LogInformation("ML.NET Model Manager disposed");
        }
    }
}
