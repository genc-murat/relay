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
    public class ModelParameterAdjusterPerformanceTests
    {
        private readonly ILogger<ModelParameterAdjuster> _logger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;
        private readonly TimeSeriesDatabase _timeSeriesDb;

        public ModelParameterAdjusterPerformanceTests()
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

        #region Performance Tests

        [Fact]
        public void AdjustModelParameters_Should_Complete_Quickly()
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
            var startTime = DateTime.UtcNow;

            // Act
            adjuster.AdjustModelParameters(false, () => modelStats);

            var duration = DateTime.UtcNow - startTime;

            // Assert - Should complete in reasonable time (< 1 second)
            Assert.True(duration.TotalSeconds < 1);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_Large_TimeSeriesDatabase()
        {
            // Arrange
            var largeTimeSeriesDb = TimeSeriesDatabase.Create(_timeSeriesLogger, maxHistorySize: 50000);

            // Add many metrics
            for (int i = 0; i < 1000; i++)
            {
                largeTimeSeriesDb.StoreMetric($"Metric_{i}", i, DateTime.UtcNow.AddMinutes(-i));
            }

            var adjuster = new ModelParameterAdjuster(_logger, _options, _recentPredictions, largeTimeSeriesDb);
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

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => modelStats));

            // Assert
            Assert.Null(exception);
        }

        #endregion
    }
}