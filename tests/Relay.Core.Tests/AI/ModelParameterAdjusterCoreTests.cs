using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Optimization.Strategies;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ModelParameterAdjusterCoreTests
    {
        private readonly ILogger<ModelParameterAdjuster> _logger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;
        private readonly TimeSeriesDatabase _timeSeriesDb;

        public ModelParameterAdjusterCoreTests()
        {
            _logger = NullLogger<ModelParameterAdjuster>.Instance;
            _timeSeriesLogger = NullLogger<TimeSeriesDatabase>.Instance;
            _options = new AIOptimizationOptions
            {
                MinConfidenceScore = 0.7,
                HighExecutionTimeThreshold = 500.0,
                DefaultBatchSize = 10,
                MaxBatchSize = 100,
                MinCacheTtl = TimeSpan.FromMinutes(1),
                MaxCacheTtl = TimeSpan.FromHours(24)
            };
            _recentPredictions = new ConcurrentQueue<PredictionResult>();
            _timeSeriesDb = TimeSeriesDatabase.Create(_timeSeriesLogger, maxHistorySize: 10000);
        }

        private ModelParameterAdjuster CreateAdjuster()
        {
            return new ModelParameterAdjuster(_logger, _options, _recentPredictions, _timeSeriesDb);
        }

        private ModelStatistics CreateDefaultModelStatistics()
        {
            return new ModelStatistics
            {
                AccuracyScore = 0.85,
                PrecisionScore = 0.80,
                RecallScore = 0.75,
                F1Score = 0.77,
                ModelConfidence = 0.85,
                TotalPredictions = 100,
                CorrectPredictions = 85,
                LastUpdate = DateTime.UtcNow
            };
        }

        #region AdjustModelParameters Tests

        [Fact]
        public void AdjustModelParameters_Should_Execute_Successfully_With_Increase()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => modelStats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void AdjustModelParameters_Should_Execute_Successfully_With_Decrease()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(true, () => modelStats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_GetModelStatistics_Throwing()
        {
            // Arrange
            var adjuster = CreateAdjuster();

            // Act - Should not throw even if getModelStatistics throws
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => throw new InvalidOperationException("Test exception")));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void AdjustModelParameters_Should_Be_Callable_Multiple_Times()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();

            // Act & Assert - Should not throw
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    adjuster.AdjustModelParameters(i % 2 == 0, () => modelStats);
                }
            });

            Assert.Null(exception);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_High_Accuracy()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var highAccuracyStats = new ModelStatistics
            {
                AccuracyScore = 0.95,
                PrecisionScore = 0.93,
                RecallScore = 0.92,
                F1Score = 0.93,
                ModelConfidence = 0.95,
                TotalPredictions = 100,
                CorrectPredictions = 95,
                LastUpdate = DateTime.UtcNow
            };

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => highAccuracyStats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_Low_Accuracy()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var lowAccuracyStats = new ModelStatistics
            {
                AccuracyScore = 0.50,
                PrecisionScore = 0.45,
                RecallScore = 0.40,
                F1Score = 0.42,
                ModelConfidence = 0.50,
                TotalPredictions = 100,
                CorrectPredictions = 50,
                LastUpdate = DateTime.UtcNow
            };

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(true, () => lowAccuracyStats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_Zero_Accuracy()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var zeroAccuracyStats = new ModelStatistics
            {
                AccuracyScore = 0.0,
                PrecisionScore = 0.0,
                RecallScore = 0.0,
                F1Score = 0.0,
                ModelConfidence = 0.0,
                TotalPredictions = 100,
                CorrectPredictions = 0,
                LastUpdate = DateTime.UtcNow
            };

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(true, () => zeroAccuracyStats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_Perfect_Accuracy()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var perfectAccuracyStats = new ModelStatistics
            {
                AccuracyScore = 1.0,
                PrecisionScore = 1.0,
                RecallScore = 1.0,
                F1Score = 1.0,
                ModelConfidence = 1.0,
                TotalPredictions = 100,
                CorrectPredictions = 100,
                LastUpdate = DateTime.UtcNow
            };

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => perfectAccuracyStats));

            // Assert
            Assert.Null(exception);
        }

        #endregion

        #region ModelAdjustmentMetadata Tests

        [Fact]
        public void AdjustModelParameters_Should_Create_ModelAdjustmentMetadata_With_Correct_Properties()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();
            var beforeTime = DateTime.UtcNow.AddSeconds(-1);

            // Act
            adjuster.AdjustModelParameters(false, () => modelStats);
            var afterTime = DateTime.UtcNow.AddSeconds(1);

            // Assert - Check that metadata was stored in TimeSeriesDatabase
            // The metadata itself is internal, but we can verify it was created by checking stored metrics
            var adjustmentFactorMetrics = _timeSeriesDb.GetRecentMetrics("Model_AdjustmentFactor", 1);
            var adjustmentDirectionMetrics = _timeSeriesDb.GetRecentMetrics("Model_AdjustmentDirection", 1);

            Assert.NotEmpty(adjustmentFactorMetrics);
            Assert.NotEmpty(adjustmentDirectionMetrics);

            var adjustmentFactorMetric = adjustmentFactorMetrics[0];
            var adjustmentDirectionMetric = adjustmentDirectionMetrics[0];

            // Factor should be greater than 1.0 for increase
            Assert.True(adjustmentFactorMetric.Value > 1.0);
            // Direction should be 1.0 for increase
            Assert.Equal(1.0, adjustmentDirectionMetric.Value);

            // Timestamp should be recent
            Assert.True(adjustmentFactorMetric.Timestamp >= beforeTime);
            Assert.True(adjustmentFactorMetric.Timestamp <= afterTime);
        }

        [Fact]
        public void AdjustModelParameters_Should_Create_Metadata_With_Increase_Direction()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();

            // Act
            adjuster.AdjustModelParameters(false, () => modelStats); // false = increase

            // Assert
            var directionMetrics = _timeSeriesDb.GetRecentMetrics("Model_AdjustmentDirection", 1);
            Assert.NotEmpty(directionMetrics);
            Assert.Equal(1.0, directionMetrics[0].Value); // 1.0 = increase
        }

        [Fact]
        public void AdjustModelParameters_Should_Create_Metadata_With_Decrease_Direction()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();

            // Act
            adjuster.AdjustModelParameters(true, () => modelStats); // true = decrease

            // Assert
            var directionMetrics = _timeSeriesDb.GetRecentMetrics("Model_AdjustmentDirection", 1);
            Assert.NotEmpty(directionMetrics);
            Assert.Equal(-1.0, directionMetrics[0].Value); // -1.0 = decrease
        }

        [Fact]
        public void AdjustModelParameters_Should_Store_Adjustment_Count_In_TimeSeries()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();

            // Act - Perform multiple adjustments
            adjuster.AdjustModelParameters(false, () => modelStats);
            adjuster.AdjustModelParameters(false, () => modelStats);
            adjuster.AdjustModelParameters(true, () => modelStats);

            // Assert
            var increaseCountMetrics = _timeSeriesDb.GetRecentMetrics("Model_Adjustment_increase_Count", 1);
            var decreaseCountMetrics = _timeSeriesDb.GetRecentMetrics("Model_Adjustment_decrease_Count", 1);

            Assert.NotEmpty(increaseCountMetrics);
            Assert.NotEmpty(decreaseCountMetrics);

            // Should have 2 increases and 1 decrease
            Assert.Equal(2.0, increaseCountMetrics[0].Value);
            Assert.Equal(1.0, decreaseCountMetrics[0].Value);
        }

        [Fact]
        public void AdjustModelParameters_Should_Store_Adjustment_Frequency_In_TimeSeries()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();

            // Act
            adjuster.AdjustModelParameters(false, () => modelStats);

            // Assert
            var frequencyMetrics = _timeSeriesDb.GetRecentMetrics("Model_AdjustmentFrequency", 1);
            Assert.NotEmpty(frequencyMetrics);
            Assert.True(frequencyMetrics[0].Value >= 0); // Frequency should be non-negative
        }

        [Fact]
        public void AdjustModelParameters_Should_Store_Audit_Trail_Metrics()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();

            // Act
            adjuster.AdjustModelParameters(false, () => modelStats);

            // Assert - Check that audit trail metrics are stored
            var timestampMetrics = _timeSeriesDb.GetRecentMetrics("Audit_ModelAdjustment_Timestamp", 1);
            var factorMagnitudeMetrics = _timeSeriesDb.GetRecentMetrics("Audit_ModelAdjustment_FactorMagnitude", 1);
            var isIncreaseMetrics = _timeSeriesDb.GetRecentMetrics("Audit_ModelAdjustment_IsIncrease", 1);

            Assert.NotEmpty(timestampMetrics);
            Assert.NotEmpty(factorMagnitudeMetrics);
            Assert.NotEmpty(isIncreaseMetrics);

            // Timestamp should be a valid tick count
            Assert.True(timestampMetrics[0].Value > 0);
            // Factor magnitude should be non-negative
            Assert.True(factorMagnitudeMetrics[0].Value >= 0);
            // Should be marked as increase (1.0)
            Assert.Equal(1.0, isIncreaseMetrics[0].Value);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_Multiple_Adjustments_With_Different_Directions()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();

            // Act - Mix of increases and decreases
            adjuster.AdjustModelParameters(false, () => modelStats); // increase
            adjuster.AdjustModelParameters(true, () => modelStats);  // decrease
            adjuster.AdjustModelParameters(false, () => modelStats); // increase
            adjuster.AdjustModelParameters(true, () => modelStats);  // decrease

            // Assert
            var increaseCountMetrics = _timeSeriesDb.GetRecentMetrics("Model_Adjustment_increase_Count", 1);
            var decreaseCountMetrics = _timeSeriesDb.GetRecentMetrics("Model_Adjustment_decrease_Count", 1);

            Assert.NotEmpty(increaseCountMetrics);
            Assert.NotEmpty(decreaseCountMetrics);

            Assert.Equal(2.0, increaseCountMetrics[0].Value);
            Assert.Equal(2.0, decreaseCountMetrics[0].Value);
        }

        [Fact]
        public void AdjustModelParameters_Should_Store_Metadata_Even_When_Exception_Occurs_In_Other_Operations()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();

            // Act
            adjuster.AdjustModelParameters(false, () => modelStats);

            // Assert - Even if other operations fail, metadata should still be stored
            var adjustmentFactorMetrics = _timeSeriesDb.GetRecentMetrics("Model_AdjustmentFactor", 1);
            Assert.NotEmpty(adjustmentFactorMetrics);
        }

        #endregion
    }
}