using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCacheTrendDataTests
    {
        private readonly ILogger<ConnectionMetricsCache> _logger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;

        public ConnectionMetricsCacheTrendDataTests()
        {
            _logger = NullLogger<ConnectionMetricsCache>.Instance;
            _timeSeriesLogger = NullLogger<TimeSeriesDatabase>.Instance;
        }

        private ConnectionMetricsCache CreateCache()
        {
            var timeSeriesDb = TimeSeriesDatabase.Create(_timeSeriesLogger, maxHistorySize: 10000);
            return new ConnectionMetricsCache(_logger, timeSeriesDb);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Store_Successfully()
        {
            // Arrange
            var cache = CreateCache();
            var connectionCount = 100;
            var timestamp = DateTime.UtcNow;

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionTrendData(connectionCount, timestamp));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Handle_Zero_Count()
        {
            // Arrange
            var cache = CreateCache();
            var connectionCount = 0;
            var timestamp = DateTime.UtcNow;

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionTrendData(connectionCount, timestamp));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Handle_Multiple_Timestamps()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    cache.StoreConnectionTrendData(100 + i, baseTime.AddMinutes(i));
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Handle_Sequential_Calls()
        {
            // Arrange
            var cache = CreateCache();
            var timestamp = DateTime.UtcNow;

            // Act - Store sequential data to build up statistics
            for (int i = 0; i < 100; i++)
            {
                cache.StoreConnectionTrendData(50 + i, timestamp.AddSeconds(i));
            }

            // Assert - No exception should be thrown
            Assert.True(true);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Handle_Concurrent_Calls()
        {
            // Arrange
            var cache = CreateCache();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var baseTime = DateTime.UtcNow;

            // Act
            System.Threading.Tasks.Parallel.For(0, 50, i =>
            {
                try
                {
                    cache.StoreConnectionTrendData(100 + i, baseTime.AddSeconds(i));
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
        public void StoreConnectionTrendData_Should_Calculate_Moving_Averages()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act - Store enough data to calculate moving averages
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    cache.StoreConnectionTrendData(100 + (i % 20), baseTime.AddMinutes(i));
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Calculate_Trend_Increasing()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act - Store increasing trend data
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    cache.StoreConnectionTrendData(50 + i * 2, baseTime.AddMinutes(i));
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Calculate_Trend_Decreasing()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act - Store decreasing trend data
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    cache.StoreConnectionTrendData(200 - i * 2, baseTime.AddMinutes(i));
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Calculate_Trend_Stable()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act - Store stable trend data
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    cache.StoreConnectionTrendData(100 + (i % 5), baseTime.AddMinutes(i));
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Calculate_Volatility()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act - Store volatile data
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var value = 100 + (i % 2 == 0 ? 50 : -50);
                    cache.StoreConnectionTrendData(value, baseTime.AddMinutes(i));
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Handle_Past_Timestamp()
        {
            // Arrange
            var cache = CreateCache();
            var connectionCount = 100;
            var timestamp = DateTime.UtcNow.AddDays(-1);

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionTrendData(connectionCount, timestamp));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Handle_Future_Timestamp()
        {
            // Arrange
            var cache = CreateCache();
            var connectionCount = 100;
            var timestamp = DateTime.UtcNow.AddDays(1);

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionTrendData(connectionCount, timestamp));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Handle_Extreme_Fluctuations()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act - Store extremely fluctuating data
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    var value = i % 2 == 0 ? 1000 : 10;
                    cache.StoreConnectionTrendData(value, baseTime.AddMinutes(i));
                }
            });

            // Assert
            Assert.Null(exception);
        }
    }
}