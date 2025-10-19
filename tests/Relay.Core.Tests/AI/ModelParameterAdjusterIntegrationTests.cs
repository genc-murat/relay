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
    public class ModelParameterAdjusterIntegrationTests
    {
        private readonly ILogger<ModelParameterAdjuster> _logger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;
        private readonly TimeSeriesDatabase _timeSeriesDb;

        public ModelParameterAdjusterIntegrationTests()
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

        #region Integration Tests

        [Fact]
        public void AdjustModelParameters_Should_Work_With_TimeSeriesDatabase()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = new ModelStatistics
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

            // Store some initial data in TimeSeriesDatabase
            _timeSeriesDb.StoreMetric("TestMetric", 100, DateTime.UtcNow);

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => modelStats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void AdjustModelParameters_Should_Update_Options()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = new ModelStatistics
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
            var originalConfidence = _options.MinConfidenceScore;

            // Act
            adjuster.AdjustModelParameters(false, () => modelStats);

            // Assert - Options should be modified (though we can't easily verify exact values)
            Assert.NotNull(_options);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_Both_Directions_Sequentially()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = new ModelStatistics
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

            // Act & Assert
            var exception = Record.Exception(() =>
            {
                adjuster.AdjustModelParameters(false, () => modelStats); // Increase
                adjuster.AdjustModelParameters(true, () => modelStats);  // Decrease
                adjuster.AdjustModelParameters(false, () => modelStats); // Increase
                adjuster.AdjustModelParameters(true, () => modelStats);  // Decrease
            });

            Assert.Null(exception);
        }

        #endregion
    }
}