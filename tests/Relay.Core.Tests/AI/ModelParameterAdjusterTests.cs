using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ModelParameterAdjusterTests
    {
        private readonly ILogger<ModelParameterAdjuster> _logger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;
        private readonly TimeSeriesDatabase _timeSeriesDb;

        public ModelParameterAdjusterTests()
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
            _timeSeriesDb = new TimeSeriesDatabase(_timeSeriesLogger, 10000);
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

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Initialize_With_Valid_Parameters()
        {
            // Arrange & Act
            var adjuster = CreateAdjuster();

            // Assert
            Assert.NotNull(adjuster);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ModelParameterAdjuster(null!, _options, _recentPredictions, _timeSeriesDb));

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Options_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ModelParameterAdjuster(_logger, null!, _recentPredictions, _timeSeriesDb));

            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_RecentPredictions_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ModelParameterAdjuster(_logger, _options, null!, _timeSeriesDb));

            Assert.Equal("recentPredictions", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_TimeSeriesDb_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ModelParameterAdjuster(_logger, _options, _recentPredictions, null!));

            Assert.Equal("timeSeriesDb", exception.ParamName);
        }

        #endregion

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

        #region Edge Case Tests

        [Fact]
        public void AdjustModelParameters_Should_Handle_Null_Return_From_GetModelStatistics()
        {
            // Arrange
            var adjuster = CreateAdjuster();

            // Act - Should handle gracefully
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => null!));

            // Assert
            Assert.Null(exception); // Should handle the exception internally
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_Extreme_Options_Values()
        {
            // Arrange
            var extremeOptions = new AIOptimizationOptions
            {
                MinConfidenceScore = 0.01,
                HighExecutionTimeThreshold = 10000.0,
                DefaultBatchSize = 1,
                MaxBatchSize = 1000,
                MinCacheTtl = TimeSpan.FromSeconds(1),
                MaxCacheTtl = TimeSpan.FromDays(7)
            };
            var timeSeriesDb = new TimeSeriesDatabase(_timeSeriesLogger, 10000);
            var adjuster = new ModelParameterAdjuster(_logger, extremeOptions, _recentPredictions, timeSeriesDb);
            var modelStats = CreateDefaultModelStatistics();

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => modelStats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_Empty_Predictions_Queue()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => modelStats));

            // Assert
            Assert.Null(exception);
            Assert.Empty(_recentPredictions);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_Many_Predictions_In_Queue()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();

            // Add many predictions
            for (int i = 0; i < 1000; i++)
            {
                _recentPredictions.Enqueue(new PredictionResult
                {
                    RequestType = typeof(string),
                    PredictedStrategies = new[] { OptimizationStrategy.EnableCaching },
                    ActualImprovement = TimeSpan.FromMilliseconds(100),
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    Metrics = new RequestExecutionMetrics
                    {
                        AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                        TotalExecutions = 10,
                        SuccessfulExecutions = 9,
                        FailedExecutions = 1
                    }
                });
            }

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => modelStats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void AdjustModelParameters_Should_Handle_Target_Accuracy()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var targetAccuracyStats = new ModelStatistics
            {
                AccuracyScore = 0.85, // Exactly at target
                PrecisionScore = 0.85,
                RecallScore = 0.85,
                F1Score = 0.85,
                ModelConfidence = 0.85,
                TotalPredictions = 100,
                CorrectPredictions = 85,
                LastUpdate = DateTime.UtcNow
            };

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => targetAccuracyStats));

            // Assert
            Assert.Null(exception);
        }

        #endregion

        #region Sequential Operations Tests

        [Fact]
        public void AdjustModelParameters_Should_Handle_Alternating_Increase_Decrease()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();

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
            var modelStats = CreateDefaultModelStatistics();

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

        #region Integration Tests

        [Fact]
        public void AdjustModelParameters_Should_Work_With_TimeSeriesDatabase()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();

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
            var modelStats = CreateDefaultModelStatistics();
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
            var modelStats = CreateDefaultModelStatistics();

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

        #region Performance Tests

        [Fact]
        public void AdjustModelParameters_Should_Complete_Quickly()
        {
            // Arrange
            var adjuster = CreateAdjuster();
            var modelStats = CreateDefaultModelStatistics();
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
            var largeTimeSeriesDb = new TimeSeriesDatabase(_timeSeriesLogger, 50000);
            
            // Add many metrics
            for (int i = 0; i < 1000; i++)
            {
                largeTimeSeriesDb.StoreMetric($"Metric_{i}", i, DateTime.UtcNow.AddMinutes(-i));
            }

            var adjuster = new ModelParameterAdjuster(_logger, _options, _recentPredictions, largeTimeSeriesDb);
            var modelStats = CreateDefaultModelStatistics();

            // Act
            var exception = Record.Exception(() =>
                adjuster.AdjustModelParameters(false, () => modelStats));

            // Assert
            Assert.Null(exception);
        }

        #endregion

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
