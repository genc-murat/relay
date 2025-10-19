using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCollectorActiveConnectionTests
    {
        private readonly ILogger<ConnectionMetricsCollector> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

        public ConnectionMetricsCollectorActiveConnectionTests()
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
        public void GetActiveConnectionCount_Should_Return_Valid_Count()
        {
            // Arrange
            var collector = CreateCollector();
            var cacheCallCount = 0;

            // Act
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 10,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 5,
                filterHealthyConnections: count => (int)(count * 0.9),
                cacheConnectionCount: count => cacheCallCount++,
                getFallbackConnectionCount: () => 10
            );

            // Assert
            Assert.True(result >= 0);
            Assert.Equal(1, cacheCallCount);
        }

        [Fact]
        public void GetActiveConnectionCount_Should_Return_Non_Negative()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 0,
                calculateConnectionThroughputFactor: () => 0.0,
                estimateKeepAliveConnections: () => 0,
                filterHealthyConnections: count => -100, // Returns negative
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 5
            );

            // Assert
            Assert.True(result >= 0, "Connection count should never be negative");
        }

        [Fact]
        public void GetActiveConnectionCount_Should_Call_Cache_Function()
        {
            // Arrange
            var collector = CreateCollector();
            var cacheWasCalled = false;
            var cachedValue = 0;

            // Act
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 10,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 5,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { cacheWasCalled = true; cachedValue = count; },
                getFallbackConnectionCount: () => 10
            );

            // Assert
            Assert.True(cacheWasCalled);
            Assert.True(cachedValue > 0);
        }

        [Fact]
        public void GetActiveConnectionCount_Should_Handle_Exception_Gracefully()
        {
            // Arrange
            var collector = CreateCollector();
            var fallbackValue = 42;

            // Act - getActiveRequestCount throws but it's used inside GetHttpConnectionCount
            // which has its own fallback, so the outer function continues
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => throw new InvalidOperationException("Test exception"),
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 5,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => fallbackValue
            );

            // Assert - Should use fallback but may not equal exact fallback value
            Assert.True(result >= 0);
        }

        [Fact]
        public void GetActiveConnectionCount_Should_Handle_High_Traffic()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 1000,
                calculateConnectionThroughputFactor: () => 500.0,
                estimateKeepAliveConnections: () => 100,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            // Assert
            Assert.True(result > 0);
        }

        [Fact]
        public void GetActiveConnectionCount_Should_Handle_Zero_Traffic()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 0,
                calculateConnectionThroughputFactor: () => 0.0,
                estimateKeepAliveConnections: () => 0,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 0
            );

            // Assert
            Assert.True(result >= 0);
        }

        [Fact]
        public void GetActiveConnectionCount_Should_Handle_Negative_Filter_Result()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 100,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 5,
                filterHealthyConnections: count => -100,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            // Assert
            Assert.Equal(0, result); // Math.Max(0, connectionCount) should clamp to 0
        }

        [Fact]
        public void GetActiveConnectionCount_Should_Handle_All_Functions_Throwing()
        {
            // Arrange
            var collector = CreateCollector();
            var fallbackValue = 25;

            // Act
            var result = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => throw new InvalidOperationException(),
                calculateConnectionThroughputFactor: () => throw new InvalidOperationException(),
                estimateKeepAliveConnections: () => throw new InvalidOperationException(),
                filterHealthyConnections: count => throw new InvalidOperationException(),
                cacheConnectionCount: count => throw new InvalidOperationException(),
                getFallbackConnectionCount: () => fallbackValue
            );

            // Assert
            Assert.Equal(fallbackValue, result);
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