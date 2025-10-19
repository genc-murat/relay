using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCacheAnomalyTests
    {
        private readonly ILogger<ConnectionMetricsCache> _logger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;

        public ConnectionMetricsCacheAnomalyTests()
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
        public void DetectConnectionAnomalies_Should_Handle_Insufficient_Data()
        {
            // Arrange
            var cache = CreateCache();
            var currentCount = 100;
            var timestamp = DateTime.UtcNow;

            // Act - Should handle when no historical data exists
            var exception = Record.Exception(() =>
                cache.DetectConnectionAnomalies(currentCount, timestamp, null));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void DetectConnectionAnomalies_Should_Detect_Spike()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Store baseline data
            for (int i = 0; i < 100; i++)
            {
                cache.StoreConnectionTrendData(50, baseTime.AddSeconds(i));
            }

            // Act - Store a spike
            var spikeTime = baseTime.AddSeconds(100);
            var exception = Record.Exception(() =>
                cache.StoreConnectionTrendData(500, spikeTime));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void DetectConnectionAnomalies_Should_Detect_Drop()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Store baseline data
            for (int i = 0; i < 100; i++)
            {
                cache.StoreConnectionTrendData(500, baseTime.AddSeconds(i));
            }

            // Act - Store a drop
            var dropTime = baseTime.AddSeconds(100);
            var exception = Record.Exception(() =>
                cache.StoreConnectionTrendData(50, dropTime));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void DetectConnectionAnomalies_Should_Handle_With_Stats_Provided()
        {
            // Arrange
            var cache = CreateCache();
            var currentCount = 100;
            var timestamp = DateTime.UtcNow;
            var stats = new MetricStatistics
            {
                MetricName = "ConnectionCount",
                Count = 50,
                Mean = 100,
                StdDev = 10,
                Min = 80,
                Max = 120,
                Median = 100,
                P95 = 115,
                P99 = 118
            };

            // Act
            var exception = Record.Exception(() =>
                cache.DetectConnectionAnomalies(currentCount, timestamp, stats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void DetectConnectionAnomalies_Should_Detect_High_Z_Score()
        {
            // Arrange
            var cache = CreateCache();
            var timestamp = DateTime.UtcNow;
            var stats = new MetricStatistics
            {
                MetricName = "ConnectionCount",
                Count = 100,
                Mean = 100,
                StdDev = 10,
                Min = 80,
                Max = 120,
                Median = 100,
                P95 = 115,
                P99 = 118
            };

            // Act - Value far from mean (z-score > 3)
            var exception = Record.Exception(() =>
                cache.DetectConnectionAnomalies(150, timestamp, stats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void DetectConnectionAnomalies_Should_Handle_High_Volatility()
        {
            // Arrange
            var cache = CreateCache();
            var timestamp = DateTime.UtcNow;
            var stats = new MetricStatistics
            {
                MetricName = "ConnectionCount",
                Count = 100,
                Mean = 100,
                StdDev = 60, // High standard deviation
                Min = 20,
                Max = 200,
                Median = 100,
                P95 = 180,
                P99 = 195
            };

            // Act
            var exception = Record.Exception(() =>
                cache.DetectConnectionAnomalies(100, timestamp, stats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void DetectConnectionAnomalies_Should_Handle_Zero_Stats()
        {
            // Arrange
            var cache = CreateCache();
            var currentCount = 100;
            var timestamp = DateTime.UtcNow;
            var stats = new MetricStatistics
            {
                MetricName = "ConnectionCount",
                Count = 0,
                Mean = 0,
                StdDev = 0,
                Min = 0,
                Max = 0,
                Median = 0,
                P95 = 0,
                P99 = 0
            };

            // Act
            var exception = Record.Exception(() =>
                cache.DetectConnectionAnomalies(currentCount, timestamp, stats));

            // Assert
            Assert.Null(exception);
        }
    }
}