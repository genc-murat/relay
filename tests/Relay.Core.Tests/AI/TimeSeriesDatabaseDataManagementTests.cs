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
    public class TimeSeriesDatabaseDataManagementTests : IDisposable
    {
        private readonly ILogger<TimeSeriesDatabase> _logger;
        private readonly TimeSeriesDatabase _database;

        public TimeSeriesDatabaseDataManagementTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            _database = TimeSeriesDatabase.Create(_logger, maxHistorySize: 1000);
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        #region CleanupOldData Tests

        [Fact]
        public void CleanupOldData_Should_Remove_Old_Metrics()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", 10.0, baseTime.AddDays(-10));
            _database.StoreMetric("test", 20.0, baseTime.AddDays(-8));
            _database.StoreMetric("test", 30.0, baseTime.AddDays(-1));
            _database.StoreMetric("test", 40.0, baseTime);

            // Act
            _database.CleanupOldData(TimeSpan.FromDays(7));

            // Assert
            var history = _database.GetHistory("test").ToList();
            Assert.True(history.Count >= 2, $"Expected at least 2 items, got {history.Count}");
            // Should keep items within last 7 days
            Assert.All(history, h => Assert.True(h.Timestamp >= baseTime.AddDays(-7)));
        }

        [Fact]
        public void CleanupOldData_Should_Keep_Recent_Metrics()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("test", 10.0, baseTime.AddHours(-1));
            _database.StoreMetric("test", 20.0, baseTime.AddMinutes(-30));
            _database.StoreMetric("test", 30.0, baseTime);

            // Act
            _database.CleanupOldData(TimeSpan.FromDays(1));

            // Assert
            var history = _database.GetHistory("test").ToList();
            Assert.Equal(3, history.Count); // All within 1 day
        }

        [Fact]
        public void CleanupOldData_Should_Handle_Multiple_Metrics()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            _database.StoreMetric("metric1", 10.0, baseTime.AddDays(-10));
            _database.StoreMetric("metric1", 20.0, baseTime);
            _database.StoreMetric("metric2", 30.0, baseTime.AddDays(-10));
            _database.StoreMetric("metric2", 40.0, baseTime);

            // Act
            _database.CleanupOldData(TimeSpan.FromDays(7));

            // Assert
            var history1 = _database.GetHistory("metric1").ToList();
            var history2 = _database.GetHistory("metric2").ToList();
            Assert.Single(history1);
            Assert.Single(history2);
        }

        [Fact]
        public void CleanupOldData_Should_Not_Throw_When_Empty()
        {
            // Act - Should not throw
            _database.CleanupOldData(TimeSpan.FromDays(1));

            // Assert - No exception
        }

        #endregion

        #region Circular Buffer Tests

        [Fact]
        public void CircularBuffer_Should_Respect_MaxHistorySize()
        {
            // Arrange
            using var db = TimeSeriesDatabase.Create(_logger, maxHistorySize: 10);
            var baseTime = DateTime.UtcNow;

            // Act - Store more than max size
            for (int i = 0; i < 15; i++)
            {
                db.StoreMetric("test", i * 10.0, baseTime.AddMinutes(i));
            }

            // Assert
            var history = db.GetHistory("test").ToList();
            Assert.Equal(10, history.Count); // Should only keep last 10
            Assert.Equal(50.0f, history[0].Value, 2); // First should be 50 (index 5)
            Assert.Equal(140.0f, history[9].Value, 2); // Last should be 140 (index 14)
        }

        [Fact]
        public void CircularBuffer_Should_Maintain_Order_After_Overflow()
        {
            // Arrange
            using var db = TimeSeriesDatabase.Create(_logger, maxHistorySize: 5);
            var baseTime = DateTime.UtcNow;

            // Act
            for (int i = 0; i < 8; i++)
            {
                db.StoreMetric("test", i * 10.0, baseTime.AddMinutes(i));
            }

            // Assert
            var history = db.GetHistory("test").ToList();
            Assert.Equal(5, history.Count);
            // Should be ordered by timestamp
            for (int i = 1; i < history.Count; i++)
            {
                Assert.True(history[i - 1].Timestamp <= history[i].Timestamp);
            }
        }

        #endregion
    }
}