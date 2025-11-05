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
    public class ModelParameterAdjusterSequentialTests
    {
        private readonly ILogger<ModelParameterAdjuster> _logger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;
        private readonly TimeSeriesDatabase _timeSeriesDb;

        public ModelParameterAdjusterSequentialTests()
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

        #region Sequential Operations Tests

        [Fact]
        public void AdjustModelParameters_Should_Handle_Alternating_Increase_Decrease()
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
                for (int i = 0; i < 20; i++)
                {
                    adjuster.AdjustModelParameters(i % 2 == 0, () => modelStats);
                }
            });

            Assert.Null(exception);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_Rapid_Sequential_Calls()
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

            // Act
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    adjuster.AdjustModelParameters(false, () => modelStats);
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_Varying_Model_Statistics()
        {
            // Arrange
            var adjuster = CreateAdjuster();

            // Act
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var stats = new ModelStatistics
                    {
                        AccuracyScore = 0.5 + (i * 0.05),
                        PrecisionScore = 0.5 + (i * 0.04),
                        RecallScore = 0.5 + (i * 0.03),
                        F1Score = 0.5 + (i * 0.035),
                        ModelConfidence = 0.5 + (i * 0.05),
                        TotalPredictions = 100 + (i * 10),
                        CorrectPredictions = 50 + (i * 10),
                        LastUpdate = DateTime.UtcNow
                    };
                    adjuster.AdjustModelParameters(false, () => stats);
                }
            });

            // Assert
            Assert.Null(exception);
        }

        #endregion
    }
}