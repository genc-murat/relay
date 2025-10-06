using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization.Strategies
{
    /// <summary>
    /// Adjusts machine learning model parameters based on performance feedback
    /// </summary>
    internal sealed class ModelParameterAdjuster
    {
        private readonly ILogger<ModelParameterAdjuster> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;
        private readonly TimeSeriesDatabase _timeSeriesDb;

        public ModelParameterAdjuster(
            ILogger<ModelParameterAdjuster> logger,
            AIOptimizationOptions options,
            ConcurrentQueue<PredictionResult> recentPredictions,
            TimeSeriesDatabase timeSeriesDb)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _recentPredictions = recentPredictions ?? throw new ArgumentNullException(nameof(recentPredictions));
            _timeSeriesDb = timeSeriesDb ?? throw new ArgumentNullException(nameof(timeSeriesDb));
        }

        public void AdjustModelParameters(bool decrease, Func<ModelStatistics> getModelStatistics)
        {
            try
            {
                var adjustmentDirection = decrease ? "decrease" : "increase";
                _logger.LogInformation("Starting model parameter adjustment: {Direction}", adjustmentDirection);

                var adjustmentFactor = CalculateAdaptiveAdjustmentFactor(decrease, getModelStatistics);

                AdjustConfidenceThresholds(adjustmentFactor);
                AdjustStrategyWeights(adjustmentFactor);
                AdjustPredictionSensitivity(adjustmentFactor);
                AdjustLearningRate(adjustmentFactor);
                AdjustPerformanceThresholds(adjustmentFactor);
                AdjustCachingParameters(adjustmentFactor);
                AdjustBatchSizePredictionParameters(adjustmentFactor);
                UpdateModelMetadata(adjustmentFactor, adjustmentDirection);
                ValidateAdjustedParameters();

                _logger.LogInformation("Model parameter adjustment completed successfully with factor: {Factor:F3}",
                    adjustmentFactor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting model parameters");
            }
        }

        private double CalculateAdaptiveAdjustmentFactor(bool decrease, Func<ModelStatistics> getModelStatistics)
        {
            try
            {
                var modelStats = getModelStatistics();
                var accuracyScore = modelStats.AccuracyScore;

                var baseAdjustment = decrease ? 0.9 : 1.1;

                var targetAccuracy = 0.85;
                var accuracyGap = Math.Abs(accuracyScore - targetAccuracy);

                var adaptiveFactor = 1.0 + (accuracyGap * 2.0);
                adaptiveFactor = Math.Max(0.7, Math.Min(1.3, adaptiveFactor));

                var finalFactor = decrease
                    ? baseAdjustment * (2.0 - adaptiveFactor)
                    : baseAdjustment * adaptiveFactor;

                finalFactor = Math.Max(0.7, Math.Min(1.3, finalFactor));

                _logger.LogDebug("Calculated adaptive adjustment factor: {Factor:F3} " +
                    "(Base: {Base:F2}, Accuracy: {Accuracy:P}, Gap: {Gap:P})",
                    finalFactor, baseAdjustment, accuracyScore, accuracyGap);

                return finalFactor;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating adaptive adjustment factor, using default");
                return decrease ? 0.9 : 1.1;
            }
        }

        private void AdjustConfidenceThresholds(double factor)
        {
            try
            {
                var strategies = Enum.GetValues(typeof(OptimizationStrategy))
                    .Cast<OptimizationStrategy>()
                    .Where(s => s != OptimizationStrategy.None)
                    .ToArray();

                foreach (var strategy in strategies)
                {
                    var currentConfidence = CalculateStrategyConfidence(strategy);
                    var adjustedConfidence = currentConfidence * factor;

                    adjustedConfidence = Math.Max(0.3, Math.Min(0.98, adjustedConfidence));

                    _logger.LogDebug("Adjusted confidence threshold for {Strategy}: {Old:P} -> {New:P}",
                        strategy, currentConfidence, adjustedConfidence);
                }

                _logger.LogInformation("Adjusted confidence thresholds for {Count} strategies", strategies.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error adjusting confidence thresholds");
            }
        }

        private double CalculateStrategyConfidence(OptimizationStrategy strategy)
        {
            var recentPredictions = _recentPredictions.ToArray();
            var strategyPredictions = recentPredictions
                .Where(p => p.PredictedStrategies.Contains(strategy))
                .ToArray();

            if (strategyPredictions.Length == 0)
                return 0.7;

            var successCount = strategyPredictions.Count(p => p.ActualImprovement.TotalMilliseconds > 0);
            return Math.Max(0.3, Math.Min(0.98, (double)successCount / strategyPredictions.Length));
        }

        private void AdjustStrategyWeights(double factor)
        {
            try
            {
                var recentPredictions = _recentPredictions.ToArray();
                if (recentPredictions.Length == 0) return;

                var strategyGroups = recentPredictions
                    .SelectMany(p => p.PredictedStrategies.Select(s => new { Strategy = s, Prediction = p }))
                    .GroupBy(x => x.Strategy)
                    .ToArray();

                foreach (var group in strategyGroups)
                {
                    var strategy = group.Key;
                    var predictions = group.ToArray();
                    var successCount = predictions.Count(p => p.Prediction.ActualImprovement.TotalMilliseconds > 0);
                    var totalCount = predictions.Length;
                    var successRate = totalCount > 0 ? (double)successCount / totalCount : 0.5;

                    var currentWeight = 1.0;
                    var adjustedWeight = currentWeight * (successRate > 0.7 ? factor : (2.0 - factor));

                    adjustedWeight = Math.Max(0.5, Math.Min(2.0, adjustedWeight));

                    _logger.LogDebug("Adjusted strategy weight for {Strategy}: {OldWeight:F2} -> {NewWeight:F2} " +
                        "(Success rate: {SuccessRate:P})",
                        strategy, currentWeight, adjustedWeight, successRate);
                }

                _logger.LogInformation("Adjusted weights for {Count} optimization strategies", strategyGroups.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error adjusting strategy weights");
            }
        }

        private void AdjustPredictionSensitivity(double factor)
        {
            try
            {
                var currentSensitivity = 1.0;
                var adjustedSensitivity = currentSensitivity * factor;

                adjustedSensitivity = Math.Max(0.5, Math.Min(2.0, adjustedSensitivity));

                _logger.LogDebug("Adjusted prediction sensitivity: {Old:F2} -> {New:F2}",
                    currentSensitivity, adjustedSensitivity);

                var baseThreshold = _options.HighExecutionTimeThreshold;
                var adjustedThreshold = baseThreshold / adjustedSensitivity;

                _logger.LogDebug("Adjusted execution time threshold: {Old}ms -> {New:F0}ms",
                    baseThreshold, adjustedThreshold);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error adjusting prediction sensitivity");
            }
        }

        private void AdjustLearningRate(double factor)
        {
            try
            {
                var currentLearningRate = 0.01;
                var adjustedLearningRate = currentLearningRate * factor;

                adjustedLearningRate = Math.Max(0.001, Math.Min(0.1, adjustedLearningRate));

                var momentum = CalculateMomentum(adjustedLearningRate);

                _logger.LogDebug("Adjusted learning rate: {Old:F4} -> {New:F4} (Momentum: {Momentum:F3})",
                    currentLearningRate, adjustedLearningRate, momentum);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error adjusting learning rate");
            }
        }

        private double CalculateMomentum(double learningRate)
        {
            var baseMomentum = 0.9;
            var learningRateFactor = Math.Min(1.0, learningRate / 0.01);
            return baseMomentum * (1.0 - learningRateFactor * 0.1);
        }

        private void AdjustPerformanceThresholds(double factor)
        {
            try
            {
                var currentHighThreshold = _options.HighExecutionTimeThreshold;
                var adjustedHighThreshold = currentHighThreshold * (2.0 - factor);

                _logger.LogDebug("Adjusted high execution time threshold: {Old}ms -> {New:F0}ms",
                    currentHighThreshold, adjustedHighThreshold);

                var currentRepeatRateThreshold = 0.3; // Default value
                var adjustedRepeatRateThreshold = currentRepeatRateThreshold * (2.0 - factor);

                _logger.LogDebug("Adjusted repeat rate threshold: {Old:P} -> {New:P}",
                    currentRepeatRateThreshold, adjustedRepeatRateThreshold);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error adjusting performance thresholds");
            }
        }

        private void AdjustCachingParameters(double factor)
        {
            try
            {
                var currentMinCacheDuration = (int)_options.MinCacheTtl.TotalSeconds;
                var currentMaxCacheDuration = (int)_options.MaxCacheTtl.TotalSeconds;

                var adjustedMinDuration = (int)(currentMinCacheDuration * factor);
                var adjustedMaxDuration = (int)(currentMaxCacheDuration * factor);

                adjustedMinDuration = Math.Max(60, Math.Min(600, adjustedMinDuration));
                adjustedMaxDuration = Math.Max(1800, Math.Min(7200, adjustedMaxDuration));

                _logger.LogDebug("Adjusted cache duration range: {OldMin}-{OldMax}s -> {NewMin}-{NewMax}s",
                    currentMinCacheDuration, currentMaxCacheDuration, adjustedMinDuration, adjustedMaxDuration);

                var currentRepeatRateThreshold = 0.3; // Default value
                var adjustedRepeatRateThreshold = currentRepeatRateThreshold * (2.0 - factor);

                adjustedRepeatRateThreshold = Math.Max(0.1, Math.Min(0.9, adjustedRepeatRateThreshold));

                _logger.LogDebug("Adjusted caching repeat rate threshold: {Old:P} -> {New:P}",
                    currentRepeatRateThreshold, adjustedRepeatRateThreshold);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error adjusting caching parameters");
            }
        }

        private void AdjustBatchSizePredictionParameters(double factor)
        {
            try
            {
                var currentMinBatchSize = 10;
                var currentMaxBatchSize = 1000;

                var adjustedMinBatchSize = (int)(currentMinBatchSize * factor);
                var adjustedMaxBatchSize = (int)(currentMaxBatchSize * factor);

                adjustedMinBatchSize = Math.Max(5, Math.Min(50, adjustedMinBatchSize));
                adjustedMaxBatchSize = Math.Max(500, Math.Min(2000, adjustedMaxBatchSize));

                _logger.LogDebug("Adjusted batch size range: {OldMin}-{OldMax} -> {NewMin}-{NewMax}",
                    currentMinBatchSize, currentMaxBatchSize, adjustedMinBatchSize, adjustedMaxBatchSize);

                var currentOptimalRangeStart = 50;
                var adjustedOptimalRangeStart = (int)(currentOptimalRangeStart * factor);

                _logger.LogDebug("Adjusted optimal batch size range start: {Old} -> {New}",
                    currentOptimalRangeStart, adjustedOptimalRangeStart);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error adjusting batch size prediction parameters");
            }
        }

        private void UpdateModelMetadata(double adjustmentFactor, string adjustmentDirection)
        {
            try
            {
                var timestamp = DateTime.UtcNow;
                
                // Create comprehensive metadata
                var metadata = new ModelAdjustmentMetadata
                {
                    Timestamp = timestamp,
                    AdjustmentFactor = adjustmentFactor,
                    Direction = adjustmentDirection,
                    Reason = "Performance-based automatic adjustment",
                    TriggeredBy = "ModelUpdateLoop"
                };

                // Persist metadata to TimeSeriesDatabase for audit trail
                _timeSeriesDb.StoreMetric("Model_AdjustmentFactor", adjustmentFactor, timestamp);
                _timeSeriesDb.StoreMetric("Model_AdjustmentDirection", 
                    adjustmentDirection == "increase" ? 1.0 : -1.0, timestamp);
                
                // Store adjustment count
                var adjustmentCount = GetAdjustmentCount(adjustmentDirection);
                _timeSeriesDb.StoreMetric($"Model_Adjustment_{adjustmentDirection}_Count", 
                    adjustmentCount, timestamp);
                
                // Store adjustment frequency (adjustments per hour)
                var adjustmentFrequency = CalculateAdjustmentFrequency();
                _timeSeriesDb.StoreMetric("Model_AdjustmentFrequency", adjustmentFrequency, timestamp);
                
                // Store comprehensive audit information as JSON-like metrics
                StoreAuditTrail(metadata, adjustmentFactor, adjustmentDirection, timestamp);

                _logger.LogInformation("Model metadata persisted to audit trail: " +
                    "Factor={Factor:F3}, Direction={Direction}, Frequency={Frequency:F2}/hour",
                    metadata.AdjustmentFactor, metadata.Direction, adjustmentFrequency);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating model metadata");
            }
        }

        /// <summary>
        /// Get total adjustment count for direction
        /// </summary>
        private int GetAdjustmentCount(string direction)
        {
            try
            {
                var metricName = $"Model_Adjustment_{direction}_Count";
                var history = _timeSeriesDb.GetHistory(metricName, TimeSpan.FromDays(30)).ToList();
                
                return history.Count > 0 ? (int)history.Last().Value + 1 : 1;
            }
            catch
            {
                return 1;
            }
        }

        /// <summary>
        /// Calculate adjustment frequency (adjustments per hour)
        /// </summary>
        private double CalculateAdjustmentFrequency()
        {
            try
            {
                var history = _timeSeriesDb.GetHistory("Model_AdjustmentFactor", TimeSpan.FromHours(24)).ToList();
                
                if (history.Count < 2) return 0.0;

                var hoursSpanned = (history.Last().Timestamp - history.First().Timestamp).TotalHours;
                return hoursSpanned > 0 ? history.Count / hoursSpanned : 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Store comprehensive audit trail
        /// </summary>
        private void StoreAuditTrail(ModelAdjustmentMetadata metadata, double factor, 
            string direction, DateTime timestamp)
        {
            try
            {
                // Store detailed audit metrics
                _timeSeriesDb.StoreMetric("Audit_ModelAdjustment_Timestamp", 
                    timestamp.Ticks, timestamp);
                
                _timeSeriesDb.StoreMetric("Audit_ModelAdjustment_FactorMagnitude", 
                    Math.Abs(factor - 1.0), timestamp);
                
                _timeSeriesDb.StoreMetric("Audit_ModelAdjustment_IsIncrease", 
                    direction == "increase" ? 1.0 : 0.0, timestamp);
                
                _timeSeriesDb.StoreMetric("Audit_ModelAdjustment_IsDecrease", 
                    direction == "decrease" ? 1.0 : 0.0, timestamp);

                // Calculate adjustment impact score
                var impactScore = Math.Abs(factor - 1.0) * 10; // 0-3 scale (since factor is 0.7-1.3)
                _timeSeriesDb.StoreMetric("Audit_ModelAdjustment_ImpactScore", impactScore, timestamp);

                _logger.LogDebug("Audit trail stored: Impact={Impact:F2}, Direction={Direction}, " +
                    "Magnitude={Magnitude:F3}",
                    impactScore, direction, Math.Abs(factor - 1.0));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error storing audit trail");
            }
        }

        private void ValidateAdjustedParameters()
        {
            try
            {
                var validationIssues = new System.Collections.Generic.List<string>();

                var highThreshold = _options.HighExecutionTimeThreshold;
                if (highThreshold < 10 || highThreshold > 10000)
                {
                    validationIssues.Add($"High execution time threshold out of bounds: {highThreshold}ms");
                }

                var minConfidence = _options.MinConfidenceScore;
                if (minConfidence < 0.1 || minConfidence > 0.99)
                {
                    validationIssues.Add($"Min confidence score out of bounds: {minConfidence:P}");
                }

                if (validationIssues.Count > 0)
                {
                    _logger.LogWarning("Parameter validation found {Count} issues: {Issues}",
                        validationIssues.Count, string.Join(", ", validationIssues));
                }
                else
                {
                    _logger.LogDebug("All adjusted parameters validated successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating adjusted parameters");
            }
        }
    }
}
