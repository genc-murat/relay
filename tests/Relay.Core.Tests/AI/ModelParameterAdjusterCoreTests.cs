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
    }
}