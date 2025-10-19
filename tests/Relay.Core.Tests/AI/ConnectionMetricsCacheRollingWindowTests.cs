using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCacheRollingWindowTests
    {
        private readonly ILogger<ConnectionMetricsCache> _logger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;

        public ConnectionMetricsCacheRollingWindowTests()
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
        public void CacheConnectionMetricWithRollingWindow_Should_Cache_Successfully()
        {
            // Arrange
            var cache = CreateCache();
            var windowKey = "test_window_20240101120000";
            var connectionCount = 100;

            // Act - Should not throw
            var exception = Record.Exception(() =>
                cache.CacheConnectionMetricWithRollingWindow(windowKey, connectionCount));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionMetricWithRollingWindow_Should_Handle_Zero_Count()
        {
            // Arrange
            var cache = CreateCache();
            var windowKey = "test_window_zero";
            var connectionCount = 0;

            // Act
            var exception = Record.Exception(() =>
                cache.CacheConnectionMetricWithRollingWindow(windowKey, connectionCount));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionMetricWithRollingWindow_Should_Handle_Large_Count()
        {
            // Arrange
            var cache = CreateCache();
            var windowKey = "test_window_large";
            var connectionCount = int.MaxValue;

            // Act
            var exception = Record.Exception(() =>
                cache.CacheConnectionMetricWithRollingWindow(windowKey, connectionCount));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionMetricWithRollingWindow_Should_Handle_Negative_Count()
        {
            // Arrange
            var cache = CreateCache();
            var windowKey = "test_window_negative";
            var connectionCount = -10;

            // Act
            var exception = Record.Exception(() =>
                cache.CacheConnectionMetricWithRollingWindow(windowKey, connectionCount));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionMetricWithRollingWindow_Should_Handle_Multiple_Calls()
        {
            // Arrange
            var cache = CreateCache();

            // Act
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    cache.CacheConnectionMetricWithRollingWindow($"window_{i}", i * 10);
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionMetricWithRollingWindow_Should_Handle_Concurrent_Calls()
        {
            // Arrange
            var cache = CreateCache();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Act
            System.Threading.Tasks.Parallel.For(0, 50, i =>
            {
                try
                {
                    cache.CacheConnectionMetricWithRollingWindow($"window_{i}", i * 10);
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
        public void CacheConnectionMetricWithRollingWindow_Should_Handle_Empty_WindowKey()
        {
            // Arrange
            var cache = CreateCache();
            var windowKey = string.Empty;
            var connectionCount = 100;

            // Act
            var exception = Record.Exception(() =>
                cache.CacheConnectionMetricWithRollingWindow(windowKey, connectionCount));

            // Assert
            Assert.Null(exception);
        }
    }
}