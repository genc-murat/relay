using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCollectorFunctionTests
    {
        private readonly ILogger<ConnectionMetricsCollector> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

        public ConnectionMetricsCollectorFunctionTests()
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
        public void GetActiveConnectionCount_Should_Apply_Filter_Correctly()
        {
            // Arrange
            var collector = CreateCollector();
            var originalCount = 0;
            var filteredCount = 0;

            // Act
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 100,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 10,
                filterHealthyConnections: count =>
                {
                    originalCount = count;
                    filteredCount = count / 2;
                    return filteredCount;
                },
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            // Assert
            Assert.True(originalCount > 0);
            Assert.True(result >= 0);
        }

        [Fact]
        public void GetActiveConnectionCount_Should_Handle_Filter_Returning_Zero()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 100,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 10,
                filterHealthyConnections: count => 0,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetActiveConnectionCount_Should_Handle_Filter_Multiplying_Count()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 10,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 5,
                filterHealthyConnections: count => count * 2, // Double the count
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            // Assert
            Assert.True(result >= 0);
        }

        [Fact]
        public void GetActiveConnectionCount_Should_Pass_Correct_Value_To_Cache()
        {
            // Arrange
            var collector = CreateCollector();
            var cachedValue = -1;

            // Act
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 10,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 5,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => cachedValue = count,
                getFallbackConnectionCount: () => 10
            );

            // Assert
            Assert.True(cachedValue >= 0);
            Assert.Equal(result, cachedValue);
        }

        [Fact]
        public void GetActiveConnectionCount_Should_Still_Return_Value_If_Cache_Throws()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 10,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 5,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => throw new InvalidOperationException("Cache error"),
                getFallbackConnectionCount: () => 10
            );

            // Assert - Should still return a value despite cache throwing
            Assert.True(result >= 0);
        }
    }
}