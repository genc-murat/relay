using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCollectorHttpConnectionTests
    {
        private readonly ILogger<ConnectionMetricsCollector> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

        public ConnectionMetricsCollectorHttpConnectionTests()
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
        public void GetHttpConnectionCount_Should_Return_Valid_Count()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetHttpConnectionCount(
                getActiveRequestCount: () => 10,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 5
            );

            // Assert
            Assert.True(result >= 0);
            Assert.True(result <= _options.MaxEstimatedHttpConnections);
        }

        [Fact]
        public void GetHttpConnectionCount_Should_Respect_Max_Limit()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetHttpConnectionCount(
                getActiveRequestCount: () => 10000, // Very high
                calculateConnectionThroughputFactor: () => 50000.0, // Very high
                estimateKeepAliveConnections: () => 5000 // Very high
            );

            // Assert
            Assert.True(result <= _options.MaxEstimatedHttpConnections);
        }

        [Fact]
        public void GetHttpConnectionCount_Should_Use_Fallback_On_Exception()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetHttpConnectionCount(
                getActiveRequestCount: () => throw new InvalidOperationException("Test exception"),
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 5
            );

            // Assert
            Assert.True(result >= 0);
            Assert.True(result <= Environment.ProcessorCount * 4);
        }

        [Fact]
        public void GetHttpConnectionCount_Should_Handle_Zero_Active_Requests()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetHttpConnectionCount(
                getActiveRequestCount: () => 0,
                calculateConnectionThroughputFactor: () => 0.0,
                estimateKeepAliveConnections: () => 0
            );

            // Assert
            Assert.True(result >= 0);
        }

        [Fact]
        public void GetHttpConnectionCount_Should_Calculate_With_Throughput_Factor()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetHttpConnectionCount(
                getActiveRequestCount: () => 0,
                calculateConnectionThroughputFactor: () => 100.0,
                estimateKeepAliveConnections: () => 0
            );

            // Assert
            Assert.True(result > 0, "Should use throughput factor when no connections detected");
        }

        [Fact]
        public void GetHttpConnectionCount_Should_Include_Keep_Alive_Connections()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetHttpConnectionCount(
                getActiveRequestCount: () => 0,
                calculateConnectionThroughputFactor: () => 0.0,
                estimateKeepAliveConnections: () => 50
            );

            // Assert
            Assert.True(result > 0, "Should include keep-alive connections");
        }

        [Fact]
        public void GetHttpConnectionCount_Should_Handle_Extreme_Values()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - These extreme values may cause overflow, but should be handled
            var exception = Record.Exception(() =>
            {
                var result = collector.GetHttpConnectionCount(
                    getActiveRequestCount: () => int.MaxValue,
                    calculateConnectionThroughputFactor: () => double.MaxValue,
                    estimateKeepAliveConnections: () => int.MaxValue
                );

                // Result should be clamped to max
                Assert.True(result <= _options.MaxEstimatedHttpConnections);
            });

            // Assert - Should handle gracefully
            Assert.Null(exception);
        }

        [Fact]
        public void GetHttpConnectionCount_Should_Handle_Negative_Throughput()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetHttpConnectionCount(
                getActiveRequestCount: () => 0,
                calculateConnectionThroughputFactor: () => -100.0,
                estimateKeepAliveConnections: () => 0
            );

            // Assert
            Assert.True(result >= 0);
        }

        [Fact]
        public void GetHttpConnectionCount_Should_Handle_Negative_Keep_Alive()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetHttpConnectionCount(
                getActiveRequestCount: () => 10,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => -10
            );

            // Assert
            Assert.True(result >= 0);
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
    }
}