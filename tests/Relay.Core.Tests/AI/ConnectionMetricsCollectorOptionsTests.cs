using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCollectorOptionsTests
    {
        private readonly ILogger<ConnectionMetricsCollector> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

        public ConnectionMetricsCollectorOptionsTests()
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
        public void GetHttpConnectionCount_Should_Respect_Custom_Max_Connections()
        {
            // Arrange
            var customOptions = new AIOptimizationOptions
            {
                MaxEstimatedHttpConnections = 50
            };
            var collector = new ConnectionMetricsCollector(_logger, customOptions, _requestAnalytics);

            // Act
            var result = collector.GetHttpConnectionCount(
                getActiveRequestCount: () => 1000,
                calculateConnectionThroughputFactor: () => 1000.0,
                estimateKeepAliveConnections: () => 1000
            );

            // Assert
            Assert.True(result <= 50);
        }

        [Fact]
        public void GetHttpConnectionCount_Should_Work_With_Zero_Max_Connections()
        {
            // Arrange
            var customOptions = new AIOptimizationOptions
            {
                MaxEstimatedHttpConnections = 0
            };
            var collector = new ConnectionMetricsCollector(_logger, customOptions, _requestAnalytics);

            // Act
            var result = collector.GetHttpConnectionCount(
                getActiveRequestCount: () => 100,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 10
            );

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void Collector_Should_Work_With_Empty_Request_Analytics()
        {
            // Arrange
            var emptyAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var collector = new ConnectionMetricsCollector(_logger, _options, emptyAnalytics);

            // Act
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 10,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 5,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            // Assert
            Assert.True(result >= 0);
        }

        [Fact]
        public void Collector_Should_Work_With_Populated_Request_Analytics()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();

            // Add some test data with actual types
            analytics.TryAdd(typeof(string), new RequestAnalysisData());
            analytics.TryAdd(typeof(int), new RequestAnalysisData());
            analytics.TryAdd(typeof(double), new RequestAnalysisData());

            var collector = new ConnectionMetricsCollector(_logger, _options, analytics);

            // Act
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 10,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 5,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            // Assert
            Assert.True(result >= 0);
        }
    }
}