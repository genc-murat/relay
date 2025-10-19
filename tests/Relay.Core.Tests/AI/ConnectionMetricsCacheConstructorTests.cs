using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCacheConstructorTests
    {
        private readonly ILogger<ConnectionMetricsCache> _logger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;

        public ConnectionMetricsCacheConstructorTests()
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
        public void Constructor_Should_Initialize_With_Valid_Parameters()
        {
            // Arrange & Act
            var cache = CreateCache();

            // Assert
            Assert.NotNull(cache);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange
            var timeSeriesDb = TimeSeriesDatabase.Create(_timeSeriesLogger, maxHistorySize: 10000);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConnectionMetricsCache(null!, timeSeriesDb));

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_TimeSeriesDb_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConnectionMetricsCache(_logger, null!));

            Assert.Equal("timeSeriesDb", exception.ParamName);
        }
    }
}