using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCacheIntegrationTests
    {
        private readonly ILogger<ConnectionMetricsCache> _logger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;

        public ConnectionMetricsCacheIntegrationTests()
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
        public void Methods_Should_Be_Callable_Multiple_Times_In_Sequence()
        {
            // Arrange
            var cache = CreateCache();
            var timestamp = DateTime.UtcNow;

            // Act & Assert - Call all methods in sequence
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    cache.CacheConnectionMetricWithRollingWindow($"window_{i}", i * 10);
                    cache.StoreConnectionTrendData(i * 10, timestamp.AddMinutes(i));

                    var breakdown = new ConnectionBreakdown
                    {
                        Timestamp = timestamp.AddMinutes(i),
                        TotalConnections = i * 10,
                        HttpConnections = i * 5,
                        DatabaseConnections = i * 3,
                        ExternalServiceConnections = i,
                        WebSocketConnections = i,
                        ActiveRequestConnections = i * 8,
                        ThreadPoolUtilization = i * 0.05,
                        DatabasePoolUtilization = i * 0.04
                    };
                    cache.StoreConnectionBreakdownHistory(breakdown);
                }
            });

            Assert.Null(exception);
        }
    }
}