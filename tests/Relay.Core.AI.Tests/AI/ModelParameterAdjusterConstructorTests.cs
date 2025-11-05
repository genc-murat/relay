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
    public class ModelParameterAdjusterConstructorTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Initialize_With_Valid_Parameters()
        {
            // Arrange
            var logger = NullLogger<ModelParameterAdjuster>.Instance;
            var timeSeriesLogger = NullLogger<TimeSeriesDatabase>.Instance;
            var options = new AIOptimizationOptions
            {
                MinConfidenceScore = 0.7,
                HighExecutionTimeThreshold = 500.0,
                DefaultBatchSize = 10,
                MaxBatchSize = 100,
                MinCacheTtl = TimeSpan.FromMinutes(1),
                MaxCacheTtl = TimeSpan.FromHours(24)
            };
            var recentPredictions = new ConcurrentQueue<PredictionResult>();
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger, maxHistorySize: 10000);

            // Act
            var adjuster = new ModelParameterAdjuster(logger, options, recentPredictions, timeSeriesDb);

            // Assert
            Assert.NotNull(adjuster);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange
            var timeSeriesLogger = NullLogger<TimeSeriesDatabase>.Instance;
            var options = new AIOptimizationOptions
            {
                MinConfidenceScore = 0.7,
                HighExecutionTimeThreshold = 500.0,
                DefaultBatchSize = 10,
                MaxBatchSize = 100,
                MinCacheTtl = TimeSpan.FromMinutes(1),
                MaxCacheTtl = TimeSpan.FromHours(24)
            };
            var recentPredictions = new ConcurrentQueue<PredictionResult>();
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger, maxHistorySize: 10000);

            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ModelParameterAdjuster(null!, options, recentPredictions, timeSeriesDb));

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Options_Is_Null()
        {
            // Arrange
            var logger = NullLogger<ModelParameterAdjuster>.Instance;
            var timeSeriesLogger = NullLogger<TimeSeriesDatabase>.Instance;
            var recentPredictions = new ConcurrentQueue<PredictionResult>();
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger, maxHistorySize: 10000);

            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ModelParameterAdjuster(logger, null!, recentPredictions, timeSeriesDb));

            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_RecentPredictions_Is_Null()
        {
            // Arrange
            var logger = NullLogger<ModelParameterAdjuster>.Instance;
            var timeSeriesLogger = NullLogger<TimeSeriesDatabase>.Instance;
            var options = new AIOptimizationOptions
            {
                MinConfidenceScore = 0.7,
                HighExecutionTimeThreshold = 500.0,
                DefaultBatchSize = 10,
                MaxBatchSize = 100,
                MinCacheTtl = TimeSpan.FromMinutes(1),
                MaxCacheTtl = TimeSpan.FromHours(24)
            };
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger, maxHistorySize: 10000);

            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ModelParameterAdjuster(logger, options, null!, timeSeriesDb));

            Assert.Equal("recentPredictions", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_TimeSeriesDb_Is_Null()
        {
            // Arrange
            var logger = NullLogger<ModelParameterAdjuster>.Instance;
            var options = new AIOptimizationOptions
            {
                MinConfidenceScore = 0.7,
                HighExecutionTimeThreshold = 500.0,
                DefaultBatchSize = 10,
                MaxBatchSize = 100,
                MinCacheTtl = TimeSpan.FromMinutes(1),
                MaxCacheTtl = TimeSpan.FromHours(24)
            };
            var recentPredictions = new ConcurrentQueue<PredictionResult>();

            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ModelParameterAdjuster(logger, options, recentPredictions, null!));

            Assert.Equal("timeSeriesDb", exception.ParamName);
        }

        #endregion
    }
}