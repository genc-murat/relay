using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCollectorSequentialTests
    {
        private readonly ILogger<ConnectionMetricsCollector> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

        public ConnectionMetricsCollectorSequentialTests()
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
        public void GetActiveConnectionCount_Should_Be_Callable_Multiple_Times()
        {
            // Arrange
            var collector = CreateCollector();

            // Act & Assert
            for (int i = 0; i < 100; i++)
            {
                var result = collector.GetActiveConnectionCount(
                    getActiveRequestCount: () => i,
                    calculateConnectionThroughputFactor: () => i * 2.0,
                    estimateKeepAliveConnections: () => i / 2,
                    filterHealthyConnections: count => count,
                    cacheConnectionCount: count => { },
                    getFallbackConnectionCount: () => 10
                );
                Assert.True(result >= 0);
            }
        }

        [Fact]
        public void GetHttpConnectionCount_Should_Be_Callable_Multiple_Times()
        {
            // Arrange
            var collector = CreateCollector();

            // Act & Assert
            for (int i = 0; i < 100; i++)
            {
                var result = collector.GetHttpConnectionCount(
                    getActiveRequestCount: () => i,
                    calculateConnectionThroughputFactor: () => i * 2.0,
                    estimateKeepAliveConnections: () => i / 2
                );
                Assert.True(result >= 0);
                Assert.True(result <= _options.MaxEstimatedHttpConnections);
            }
        }
    }
}