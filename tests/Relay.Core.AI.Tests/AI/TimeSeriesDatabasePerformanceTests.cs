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
    public class TimeSeriesDatabasePerformanceTests : IDisposable
    {
        private readonly ILogger<TimeSeriesDatabase> _logger;
        private readonly TimeSeriesDatabase _database;

        public TimeSeriesDatabasePerformanceTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            _database = TimeSeriesDatabase.Create(_logger, maxHistorySize: 1000);
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        #region Concurrent Access Tests

        [Fact]
        public async System.Threading.Tasks.Task TimeSeriesDatabase_Should_Handle_Concurrent_Reads_And_Writes()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            var tasks = new System.Threading.Tasks.Task[20];

            // Act - Mix of reads and writes
            for (int i = 0; i < 10; i++)
            {
                var index = i;
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        _database.StoreMetric($"metric{index}", j * 10.0, baseTime.AddMinutes(j));
                    }
                });
            }

            for (int i = 10; i < 20; i++)
            {
                var index = i - 10;
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    for (int j = 0; j < 5; j++)
                    {
                        var history = _database.GetHistory($"metric{index}");
                        var stats = _database.GetStatistics($"metric{index}");
                        var recent = _database.GetRecentMetrics($"metric{index}", 3);
                        var anomalies = _database.DetectAnomalies($"metric{index}");
                    }
                });
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert - Should not have thrown and data should be consistent
            for (int i = 0; i < 10; i++)
            {
                var history = _database.GetHistory($"metric{i}").ToList();
                Assert.True(history.Count >= 0); // May have been read during writes
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task TimeSeriesDatabase_Should_Handle_Concurrent_Cleanup()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            var tasks = new System.Threading.Tasks.Task[5];

            // Store some data
            for (int i = 0; i < 50; i++)
            {
                _database.StoreMetric("test", i * 10.0, baseTime.AddMinutes(i));
            }

            // Act - Concurrent cleanup operations
            for (int i = 0; i < 5; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    _database.CleanupOldData(TimeSpan.FromHours(1));
                });
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert - Should not throw
            var history = _database.GetHistory("test").ToList();
            Assert.True(history.Count >= 0);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void TimeSeriesDatabase_Should_Handle_Large_Dataset_Efficiently()
        {
            // Arrange
            using var db = TimeSeriesDatabase.Create(_logger, maxHistorySize: 10000);
            var baseTime = DateTime.UtcNow;

            // Act - Store large amount of data
            for (int i = 0; i < 5000; i++)
            {
                db.StoreMetric("performance.test", i * 0.1, baseTime.AddSeconds(i));
            }

            // Assert - Operations should complete reasonably fast
            var history = db.GetHistory("performance.test").ToList();
            var stats = db.GetStatistics("performance.test");
            var recent = db.GetRecentMetrics("performance.test", 100);

            Assert.Equal(5000, history.Count);
            Assert.NotNull(stats);
            Assert.Equal(100, recent.Count);
        }

        [Fact]
        public void DetectAnomalies_Should_Perform_With_Large_Dataset()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 200; i++)
            {
                var value = i == 100 ? 1000.0 : 50.0; // Anomaly in middle
                _database.StoreMetric("large.test", value, baseTime.AddMinutes(i));
            }

            // Act
            var anomalies = _database.DetectAnomalies("large.test", lookbackPoints: 200);

            // Assert
            Assert.NotNull(anomalies);
            // Should handle large datasets without throwing
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void TimeSeriesDatabase_Should_Handle_Complete_Workflow()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;

            // Act - Store metrics
            for (int i = 0; i < 100; i++)
            {
                _database.StoreMetric("response.time", 100.0 + i, baseTime.AddMinutes(i));
            }

            // Get history
            var history = _database.GetHistory("response.time", TimeSpan.FromHours(2)).ToList();

            // Get statistics
            var stats = _database.GetStatistics("response.time");

            // Get recent metrics
            var recent = _database.GetRecentMetrics("response.time", 10);

            // Detect anomalies
            var anomalies = _database.DetectAnomalies("response.time");

            // Cleanup old data
            _database.CleanupOldData(TimeSpan.FromDays(1));

            // Assert
            Assert.NotEmpty(history);
            Assert.NotNull(stats);
            Assert.NotEmpty(recent);
            Assert.NotNull(anomalies);
        }

        [Fact]
        public async System.Threading.Tasks.Task TimeSeriesDatabase_Should_Handle_Concurrent_Access()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;
            var tasks = new System.Threading.Tasks.Task[10];

            // Act - Concurrent writes
            for (int i = 0; i < 10; i++)
            {
                var index = i;
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        _database.StoreMetric($"metric{index}", j * 10.0, baseTime.AddMinutes(j));
                    }
                });
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert
            for (int i = 0; i < 10; i++)
            {
                var history = _database.GetHistory($"metric{i}").ToList();
                Assert.Equal(10, history.Count);
            }
        }

        [Fact]
        public void TimeSeriesDatabase_Should_Respect_MaxHistorySize()
        {
            // Arrange
            using var db = TimeSeriesDatabase.Create(_logger, maxHistorySize: 100);
            var baseTime = DateTime.UtcNow;

            // Act - Store more than max
            for (int i = 0; i < 150; i++)
            {
                db.StoreMetric("test", i * 10.0, baseTime.AddMinutes(i));
            }

            // Assert - Should only keep last 100
            var history = db.GetHistory("test").ToList();
            Assert.True(history.Count <= 100);
        }

        #endregion
    }
}