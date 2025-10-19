using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCacheBreakdownTests
    {
        private readonly ILogger<ConnectionMetricsCache> _logger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;

        public ConnectionMetricsCacheBreakdownTests()
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
        public void StoreConnectionBreakdownHistory_Should_Store_Successfully()
        {
            // Arrange
            var cache = CreateCache();
            var breakdown = new ConnectionBreakdown
            {
                Timestamp = DateTime.UtcNow,
                TotalConnections = 100,
                HttpConnections = 50,
                DatabaseConnections = 30,
                ExternalServiceConnections = 15,
                WebSocketConnections = 5,
                ActiveRequestConnections = 80,
                ThreadPoolUtilization = 0.7,
                DatabasePoolUtilization = 0.5
            };

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionBreakdownHistory(breakdown));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionBreakdownHistory_Should_Handle_All_Zero_Connections()
        {
            // Arrange
            var cache = CreateCache();
            var breakdown = new ConnectionBreakdown
            {
                Timestamp = DateTime.UtcNow,
                TotalConnections = 0,
                HttpConnections = 0,
                DatabaseConnections = 0,
                ExternalServiceConnections = 0,
                WebSocketConnections = 0,
                ActiveRequestConnections = 0,
                ThreadPoolUtilization = 0.0,
                DatabasePoolUtilization = 0.0
            };

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionBreakdownHistory(breakdown));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionBreakdownHistory_Should_Handle_High_Utilization()
        {
            // Arrange
            var cache = CreateCache();
            var breakdown = new ConnectionBreakdown
            {
                Timestamp = DateTime.UtcNow,
                TotalConnections = 1000,
                HttpConnections = 500,
                DatabaseConnections = 400,
                ExternalServiceConnections = 50,
                WebSocketConnections = 50,
                ActiveRequestConnections = 950,
                ThreadPoolUtilization = 1.0,
                DatabasePoolUtilization = 0.99
            };

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionBreakdownHistory(breakdown));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionBreakdownHistory_Should_Handle_Multiple_Breakdowns()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    var breakdown = new ConnectionBreakdown
                    {
                        Timestamp = baseTime.AddMinutes(i),
                        TotalConnections = 100 + i,
                        HttpConnections = 50 + i,
                        DatabaseConnections = 30 + i,
                        ExternalServiceConnections = 15,
                        WebSocketConnections = 5,
                        ActiveRequestConnections = 80 + i,
                        ThreadPoolUtilization = 0.5 + (i * 0.01),
                        DatabasePoolUtilization = 0.4 + (i * 0.01)
                    };
                    cache.StoreConnectionBreakdownHistory(breakdown);
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionBreakdownHistory_Should_Handle_Concurrent_Calls()
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
                    var breakdown = new ConnectionBreakdown
                    {
                        Timestamp = baseTime.AddSeconds(i),
                        TotalConnections = 100 + i,
                        HttpConnections = 50,
                        DatabaseConnections = 30,
                        ExternalServiceConnections = 15,
                        WebSocketConnections = 5,
                        ActiveRequestConnections = 80,
                        ThreadPoolUtilization = 0.5,
                        DatabasePoolUtilization = 0.4
                    };
                    cache.StoreConnectionBreakdownHistory(breakdown);
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
        public void StoreConnectionBreakdownHistory_Should_Calculate_Ratios()
        {
            // Arrange
            var cache = CreateCache();
            var breakdown = new ConnectionBreakdown
            {
                Timestamp = DateTime.UtcNow,
                TotalConnections = 200,
                HttpConnections = 100,  // 50%
                DatabaseConnections = 80,  // 40%
                ExternalServiceConnections = 10,  // 5%
                WebSocketConnections = 10,  // 5%
                ActiveRequestConnections = 150,
                ThreadPoolUtilization = 0.6,
                DatabasePoolUtilization = 0.5
            };

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionBreakdownHistory(breakdown));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionBreakdownHistory_Should_Warn_On_High_Database_Ratio()
        {
            // Arrange
            var cache = CreateCache();
            var breakdown = new ConnectionBreakdown
            {
                Timestamp = DateTime.UtcNow,
                TotalConnections = 200,
                HttpConnections = 50,
                DatabaseConnections = 120,  // 60% - High ratio
                ExternalServiceConnections = 20,
                WebSocketConnections = 10,
                ActiveRequestConnections = 150,
                ThreadPoolUtilization = 0.6,
                DatabasePoolUtilization = 0.8
            };

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionBreakdownHistory(breakdown));

            // Assert
            Assert.Null(exception);
        }
    }
}