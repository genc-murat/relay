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
    public class ModelParameterAdjusterStatisticsTests
    {
        private readonly ILogger<ModelParameterAdjuster> _logger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;
        private readonly TimeSeriesDatabase _timeSeriesDb;

        public ModelParameterAdjusterStatisticsTests()
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

        #region Model Statistics Variations Tests

        [Fact]
        public void AdjustModelParameters_Should_Handle_Inconsistent_Statistics()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var inconsistentStats = new ModelStatistics
            {
                AccuracyScore = 0.90,
                PrecisionScore = 0.20,
                RecallScore = 0.10,
                F1Score = 0.14,
                ModelConfidence = 0.50,
                TotalPredictions = 100,
                CorrectPredictions = 90, // Inconsistent with low precision/recall
                LastUpdate = DateTime.UtcNow
            };

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => inconsistentStats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_Old_Statistics()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var oldStats = new ModelStatistics
            {
                AccuracyScore = 0.85,
                PrecisionScore = 0.80,
                RecallScore = 0.75,
                F1Score = 0.77,
                ModelConfidence = 0.85,
                TotalPredictions = 100,
                CorrectPredictions = 85,
                LastUpdate = DateTime.UtcNow.AddDays(-30) // Old stats
            };

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => oldStats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_Zero_Predictions()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var zeroPredictionsStats = new ModelStatistics
            {
                AccuracyScore = 0.0,
                PrecisionScore = 0.0,
                RecallScore = 0.0,
                F1Score = 0.0,
                ModelConfidence = 0.0,
                TotalPredictions = 0,
                CorrectPredictions = 0,
                LastUpdate = DateTime.UtcNow
            };

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => zeroPredictionsStats));

            // Assert
            Assert.Null(exception);
        }

        #endregion
    }
}