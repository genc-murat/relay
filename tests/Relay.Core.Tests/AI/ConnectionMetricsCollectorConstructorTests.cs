using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCollectorConstructorTests
    {
        private readonly ILogger<ConnectionMetricsCollector> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

        public ConnectionMetricsCollectorConstructorTests()
        {
            _logger = NullLogger<ConnectionMetricsCollector>.Instance;
            _options = new AIOptimizationOptions
            {
                MaxEstimatedHttpConnections = 200,
                MaxEstimatedDbConnections = 50,
                EstimatedMaxDbConnections = 100,
                MaxEstimatedExternalConnections = 30,
                MaxEstimatedWebSocketConnections = 1000
            };
            _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
        }

        private ConnectionMetricsCollector CreateCollector()
        {
            return new ConnectionMetricsCollector(_logger, _options, _requestAnalytics);
        }

        [Fact]
        public void Constructor_Should_Initialize_With_Valid_Parameters()
        {
            // Arrange & Act
            var collector = CreateCollector();

            // Assert
            Assert.NotNull(collector);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConnectionMetricsCollector(null!, _options, _requestAnalytics));

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Options_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConnectionMetricsCollector(_logger, null!, _requestAnalytics));

            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConnectionMetricsCollector(_logger, _options, null!));

            Assert.Equal("requestAnalytics", exception.ParamName);
        }
    }
}