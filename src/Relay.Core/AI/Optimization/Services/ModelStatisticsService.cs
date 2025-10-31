using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Relay.Core.AI.Optimization.Services
{
    /// <summary>
    /// Service for tracking and updating AI model statistics and accuracy
    /// </summary>
    internal class ModelStatisticsService
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<Type, ModelAccuracyData> _modelAccuracyData = new();
        private readonly object _statisticsLock = new();

        // Overall model statistics
        private long _totalPredictions;
        private long _correctPredictions;
        private TimeSpan _totalPredictionTime;
        private DateTime _modelTrainingDate = DateTime.UtcNow;
        private DateTime _lastRetraining = DateTime.UtcNow;
        private string _modelVersion = "1.0.0";

        public ModelStatisticsService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void RecordPrediction(Type requestType)
        {
            if (requestType == null) throw new ArgumentNullException(nameof(requestType));

            lock (_statisticsLock)
            {
                Interlocked.Increment(ref _totalPredictions);
                _logger.LogDebug("Recorded prediction for {RequestType}", requestType.Name);
            }
        }

        public void UpdateModelAccuracy(
            Type requestType,
            OptimizationStrategy[] appliedOptimizations,
            RequestExecutionMetrics actualMetrics,
            bool strategiesMatch)
        {
            if (requestType == null) throw new ArgumentNullException(nameof(requestType));
            if (appliedOptimizations == null) throw new ArgumentNullException(nameof(appliedOptimizations));
            if (actualMetrics == null) throw new ArgumentNullException(nameof(actualMetrics));

            var accuracyData = _modelAccuracyData.GetOrAdd(requestType, _ => new ModelAccuracyData());

            lock (_statisticsLock)
            {
                // Increment total predictions counter
                Interlocked.Increment(ref _totalPredictions);

                // Update per-request-type accuracy
                accuracyData.AddPrediction(appliedOptimizations, actualMetrics);

                // Accuracy calculation: strategies must match prediction AND execution must be successful
                var wasSuccessful = strategiesMatch && actualMetrics.SuccessRate >= 0.8; // Arbitrary threshold
                if (wasSuccessful)
                {
                    Interlocked.Increment(ref _correctPredictions);
                }

                _totalPredictionTime += actualMetrics.AverageExecutionTime;

                _logger.LogDebug("Updated model accuracy for {RequestType}: Match={Match}, Success={Success}, Strategies={Count}",
                    requestType.Name, strategiesMatch, wasSuccessful, appliedOptimizations.Length);
            }
        }

        /// <summary>
        /// Updates model accuracy for an existing prediction without counting as a new prediction
        /// </summary>
        /// <param name="requestType">The request type</param>
        /// <param name="appliedOptimizations">The applied optimizations</param>
        /// <param name="actualMetrics">The actual execution metrics</param>
        /// <param name="strategiesMatch">Whether the applied strategies match the predicted ones</param>
        public void UpdateExistingPredictionAccuracy(
            Type requestType,
            OptimizationStrategy[] appliedOptimizations,
            RequestExecutionMetrics actualMetrics,
            bool strategiesMatch)
        {
            if (requestType == null) throw new ArgumentNullException(nameof(requestType));
            if (appliedOptimizations == null) throw new ArgumentNullException(nameof(appliedOptimizations));
            if (actualMetrics == null) throw new ArgumentNullException(nameof(actualMetrics));

            var accuracyData = _modelAccuracyData.GetOrAdd(requestType, _ => new ModelAccuracyData());

            lock (_statisticsLock)
            {
                // Update per-request-type accuracy
                accuracyData.AddPrediction(appliedOptimizations, actualMetrics);

                // Accuracy calculation: strategies must match prediction AND execution must be successful
                var wasSuccessful = strategiesMatch && actualMetrics.SuccessRate >= 0.8; // Arbitrary threshold
                if (wasSuccessful)
                {
                    Interlocked.Increment(ref _correctPredictions);
                }

                _totalPredictionTime += actualMetrics.AverageExecutionTime;

                _logger.LogDebug("Updated existing prediction accuracy for {RequestType}: Match={Match}, Success={Success}, Strategies={Count}",
                    requestType.Name, strategiesMatch, wasSuccessful, appliedOptimizations.Length);
            }
        }

        public AIModelStatistics GetModelStatistics()
        {
            lock (_statisticsLock)
            {
                var accuracy = _totalPredictions > 0 ? (double)_correctPredictions / _totalPredictions : 0.0;
                var precision = CalculatePrecision();
                var recall = CalculateRecall();
                var f1Score = precision + recall > 0 ? 2 * (precision * recall) / (precision + recall) : 0.0;
                var avgPredictionTime = _totalPredictions > 0 ? _totalPredictionTime / _totalPredictions : TimeSpan.Zero;
                var trainingDataPoints = _modelAccuracyData.Sum(kvp => kvp.Value.TotalPredictions);

                return new AIModelStatistics
                {
                    ModelTrainingDate = _modelTrainingDate,
                    TotalPredictions = _totalPredictions,
                    AccuracyScore = accuracy,
                    PrecisionScore = precision,
                    RecallScore = recall,
                    F1Score = f1Score,
                    AveragePredictionTime = avgPredictionTime,
                    TrainingDataPoints = trainingDataPoints,
                    ModelVersion = _modelVersion,
                    LastRetraining = _lastRetraining,
                    ModelConfidence = CalculateModelConfidence()
                };
            }
        }

        private double CalculatePrecision()
        {
            // Precision = True Positives / (True Positives + False Positives)
            // Simplified: correct predictions / total predictions
            return _totalPredictions > 0 ? (double)_correctPredictions / _totalPredictions : 0.0;
        }

        private double CalculateRecall()
        {
            // Recall = True Positives / (True Positives + False Negatives)
            // Simplified: using same as precision for this basic implementation
            return CalculatePrecision();
        }

        private double CalculateModelConfidence()
        {
            if (_totalPredictions < 10)
                return 0.5; // Low confidence with few predictions

            var accuracy = (double)_correctPredictions / _totalPredictions;
            var dataPoints = _modelAccuracyData.Count;

            // Higher confidence with more data and better accuracy
            var confidence = Math.Min(accuracy + (dataPoints * 0.1), 0.95);
            return Math.Max(confidence, 0.1);
        }

        /// <summary>
        /// Internal class to track accuracy data per request type
        /// </summary>
        private class ModelAccuracyData
        {
            public long TotalPredictions { get; private set; }
            public long SuccessfulPredictions { get; private set; }
            public TimeSpan TotalExecutionTime { get; private set; }
            public readonly Dictionary<OptimizationStrategy, StrategyAccuracy> StrategyAccuracyMap = new();

            public void AddPrediction(OptimizationStrategy[] appliedOptimizations, RequestExecutionMetrics actualMetrics)
            {
                TotalPredictions++;
                TotalExecutionTime += actualMetrics.AverageExecutionTime;

                var wasSuccessful = actualMetrics.SuccessRate >= 0.8;
                if (wasSuccessful)
                {
                    SuccessfulPredictions++;
                }

                // Update strategy-specific accuracy
                foreach (var strategy in appliedOptimizations)
                {
                    if (!StrategyAccuracyMap.TryGetValue(strategy, out var strategyAccuracy))
                    {
                        strategyAccuracy = new StrategyAccuracy();
                        StrategyAccuracyMap[strategy] = strategyAccuracy;
                    }
                    strategyAccuracy.AddResult(wasSuccessful, actualMetrics.AverageExecutionTime);
                }
            }

            public double GetAccuracy() => TotalPredictions > 0 ? (double)SuccessfulPredictions / TotalPredictions : 0.0;

            public OptimizationStrategy GetMostEffectiveStrategy()
            {
                return StrategyAccuracyMap
                    .OrderByDescending(kvp => kvp.Value.Accuracy)
                    .Select(kvp => kvp.Key)
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// Internal class to track accuracy per optimization strategy
        /// </summary>
        private class StrategyAccuracy
        {
            public long TotalUses { get; private set; }
            public long SuccessfulUses { get; private set; }
            public TimeSpan TotalTimeSaved { get; private set; }

            public double Accuracy => TotalUses > 0 ? (double)SuccessfulUses / TotalUses : 0.0;

            public void AddResult(bool wasSuccessful, TimeSpan executionTime)
            {
                TotalUses++;
                if (wasSuccessful)
                {
                    SuccessfulUses++;
                    // Assume successful optimizations save 30% of execution time
                    TotalTimeSaved += TimeSpan.FromMilliseconds(executionTime.TotalMilliseconds * 0.3);
                }
            }
        }
    }
}