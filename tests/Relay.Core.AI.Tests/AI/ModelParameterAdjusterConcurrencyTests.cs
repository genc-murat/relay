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
    public class ModelParameterAdjusterConcurrencyTests
    {
        private readonly ILogger<ModelParameterAdjuster> _logger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;
        private readonly TimeSeriesDatabase _timeSeriesDb;

        public ModelParameterAdjusterConcurrencyTests()
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

        #region Concurrent Access Tests

        [Fact]
        public void AdjustModelParameters_Should_Handle_Concurrent_Calls()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();
            var exceptions = new ConcurrentBag<Exception>();

            // Act
            System.Threading.Tasks.Parallel.For(0, 10, i =>
            {
                try
                {
                    adjuster.AdjustModelParameters(i % 2 == 0, () => modelStats);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Assert
            Assert.Empty(exceptions);
        }

        [Fact]
        public void AdjustModelParameters_Should_Be_Thread_Safe_With_Different_Stats()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var exceptions = new ConcurrentBag<Exception>();

            // Act
            System.Threading.Tasks.Parallel.For(0, 10, i =>
            {
                try
                {
                    var stats = new ModelStatistics
                    {
                        AccuracyScore = 0.5 + (i * 0.05),
                        PrecisionScore = 0.5,
                        RecallScore = 0.5,
                        F1Score = 0.5,
                        ModelConfidence = 0.5,
                        TotalPredictions = 100,
                        CorrectPredictions = 50,
                        LastUpdate = DateTime.UtcNow
                    };
                    adjuster.AdjustModelParameters(i % 2 == 0, () => stats);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Assert
            Assert.Empty(exceptions);
        }

        #endregion
    }
}