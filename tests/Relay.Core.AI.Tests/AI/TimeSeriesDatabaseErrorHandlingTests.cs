using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class TimeSeriesDatabaseErrorHandlingTests : IDisposable
    {
        private readonly ILogger<TimeSeriesDatabase> _logger;
        private readonly TimeSeriesDatabase _database;

        public TimeSeriesDatabaseErrorHandlingTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            _database = TimeSeriesDatabase.Create(_logger, maxHistorySize: 1000);
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        #region Error Handling Tests

        [Fact]
        public void StoreMetric_Should_Handle_Exceptions_Gracefully()
        {
            // Arrange - Create a database with a mock logger to verify graceful handling
            var loggerMock = new Moq.Mock<ILogger<TimeSeriesDatabase>>();
            using var db = TimeSeriesDatabase.Create(loggerMock.Object);

            // Act - This should not throw even with problematic values
            db.StoreMetric("test", double.NaN, DateTime.UtcNow); // NaN values are handled gracefully

            // Assert - Should not throw, method handles NaN values without exceptions
            var history = db.GetHistory("test").ToList();
            Assert.Single(history);
            Assert.True(float.IsNaN(history[0].Value)); // NaN is preserved
        }

        [Fact]
        public void TrainForecastModel_Should_Handle_Exceptions_Gracefully()
        {
            // Arrange
            var loggerMock = new Moq.Mock<ILogger<TimeSeriesDatabase>>();
            using var db = TimeSeriesDatabase.Create(loggerMock.Object);

            // Store insufficient data that will cause InsufficientDataException
            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 5; i++) // Less than minimum 10 required
            {
                db.StoreMetric("test", 10.0 + i, baseTime.AddHours(i));
            }

            // Act - Should not throw but log error
            db.TrainForecastModel("test");

            // Assert - Should have logged the error due to insufficient data
            loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error training") && o.ToString()!.Contains("forecast model")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Forecast_Should_Handle_Exceptions_Gracefully()
        {
            // Arrange
            var loggerMock = new Moq.Mock<ILogger<TimeSeriesDatabase>>();
            using var db = TimeSeriesDatabase.Create(loggerMock.Object);

            // Store data that should work but might cause internal issues
            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 10; i++)
            {
                // Mix of normal and problematic values
                var value = i == 5 ? double.NaN : 50.0;
                db.StoreMetric("test", value, baseTime.AddMinutes(i));
            }

            // Act - Should not throw even with NaN values
            var result = db.Forecast("test");

            // Assert - Should complete without throwing
            Assert.Null(result); // Returns null due to insufficient data for forecasting
        }

        [Fact]
        public void DetectAnomalies_Should_Handle_Exceptions_Gracefully()
        {
            // Arrange
            var loggerMock = new Moq.Mock<ILogger<TimeSeriesDatabase>>();
            using var db = TimeSeriesDatabase.Create(loggerMock.Object);

            // Store data that should work but might cause internal issues
            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 50; i++)
            {
                // Mix of normal and problematic values
                var value = i == 25 ? double.NaN : 50.0;
                db.StoreMetric("test", value, baseTime.AddMinutes(i));
            }

            // Act - Should not throw even with NaN values
            var anomalies = db.DetectAnomalies("test");

            // Assert - Should complete without throwing
            Assert.NotNull(anomalies);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_Exceptions_Gracefully()
        {
            // Arrange
            var loggerMock = new Moq.Mock<ILogger<TimeSeriesDatabase>>();
            using var db = TimeSeriesDatabase.Create(loggerMock.Object);

            // Store some data
            db.StoreMetric("test", 10.0, DateTime.UtcNow);

            // Act - Should not throw even if cleanup fails
            db.CleanupOldData(TimeSpan.FromDays(1));

            // Assert - Should not throw (cleanup is best-effort)
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void GetStatistics_Should_Handle_Single_Data_Point()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", 42.0, baseTime);

            // Act
            var stats = _database.GetStatistics("test");

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(1, stats.Count);
            Assert.Equal(42.0f, stats.Mean, 2);
            Assert.Equal(42.0f, stats.Min, 2);
            Assert.Equal(42.0f, stats.Max, 2);
            Assert.Equal(42.0f, stats.Median, 2);
            Assert.Equal(0.0f, stats.StdDev, 2); // No variation with single point
        }

        [Fact]
        public void GetStatistics_Should_Handle_Zero_Values()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", 0.0, baseTime);
            _database.StoreMetric("test", 0.0, baseTime.AddMinutes(1));

            // Act
            var stats = _database.GetStatistics("test");

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(0.0f, stats.Mean, 2);
            Assert.Equal(0.0f, stats.Min, 2);
            Assert.Equal(0.0f, stats.Max, 2);
            Assert.Equal(0.0f, stats.StdDev, 2);
        }

        [Fact]
        public void GetStatistics_Should_Handle_Negative_Values()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", -10.0, baseTime);
            _database.StoreMetric("test", -5.0, baseTime.AddMinutes(1));
            _database.StoreMetric("test", 5.0, baseTime.AddMinutes(2));

            // Act
            var stats = _database.GetStatistics("test");

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(-10.0f, stats.Min, 2);
            Assert.Equal(5.0f, stats.Max, 2);
            Assert.Equal(-3.33f, stats.Mean, 2);
        }

        [Fact]
        public void StoreMetric_Should_Handle_Extreme_Values()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;

            // Act
            _database.StoreMetric("test", double.MaxValue, timestamp);
            _database.StoreMetric("test", double.MinValue, timestamp.AddMinutes(1));

            // Assert
            var history = _database.GetHistory("test").ToList();
            Assert.Equal(2, history.Count);
            Assert.Equal(float.PositiveInfinity, history[0].Value); // double.MaxValue becomes float.PositiveInfinity when cast
            Assert.Equal(float.NegativeInfinity, history[1].Value); // double.MinValue becomes float.NegativeInfinity when cast
        }

        [Fact]
        public void StoreBatch_Should_Handle_Null_MovingAverages_Dictionary()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var metrics = new Dictionary<string, double>
            {
                ["test"] = 100.0
            };

            // Act
            _database.StoreBatch(metrics, timestamp, movingAverages: null);

            // Assert
            var history = _database.GetHistory("test").ToList();
            Assert.Single(history);
            Assert.Equal(100.0f, history[0].Value, 2);
            Assert.Equal(100.0f, history[0].MA5, 2); // Should default to value
            Assert.Equal(100.0f, history[0].MA15, 2); // Should default to value
        }

        [Fact]
        public void StoreBatch_Should_Handle_Null_TrendDirections_Dictionary()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var metrics = new Dictionary<string, double>
            {
                ["test"] = 100.0
            };

            // Act
            _database.StoreBatch(metrics, timestamp, trendDirections: null);

            // Assert
            var history = _database.GetHistory("test").ToList();
            Assert.Single(history);
            Assert.Equal((int)TrendDirection.Stable, history[0].Trend); // Should default to Stable
        }

        [Fact]
        public void GetHistory_Should_Handle_Very_Old_Lookback_Period()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", 10.0, baseTime.AddYears(-1));
            _database.StoreMetric("test", 20.0, baseTime);

            // Act
            var history = _database.GetHistory("test", TimeSpan.FromDays(365 * 2)).ToList();

            // Assert
            Assert.Equal(2, history.Count); // Should return all data
        }

        [Fact]
        public void GetRecentMetrics_Should_Handle_Count_Zero()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", 10.0, baseTime);

            // Act
            var recent = _database.GetRecentMetrics("test", 0);

            // Assert
            Assert.Empty(recent);
        }

        [Fact]
        public void DetectAnomalies_Should_Handle_Exactly_12_Data_Points()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 12; i++)
            {
                _database.StoreMetric("test", 50.0, baseTime.AddMinutes(i));
            }

            // Act
            var anomalies = _database.DetectAnomalies("test");

            // Assert
            Assert.NotNull(anomalies);
            // May or may not detect anomalies, but shouldn't throw
        }

        [Fact]
        public void TrainForecastModel_Should_Handle_Exactly_48_Data_Points()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddDays(-1);
            for (int i = 0; i < 48; i++)
            {
                _database.StoreMetric("test", 50.0 + i, baseTime.AddHours(i));
            }

            // Act - Should not throw
            _database.TrainForecastModel("test");

            // Assert - Should handle exactly the minimum required data
        }

        #endregion
    }
}