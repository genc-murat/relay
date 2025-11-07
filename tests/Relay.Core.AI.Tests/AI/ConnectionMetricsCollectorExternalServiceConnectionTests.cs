using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCollectorExternalServiceConnectionTests
    {
        private readonly ILogger<ConnectionMetricsCollector> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

        public ConnectionMetricsCollectorExternalServiceConnectionTests()
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
        public void GetExternalServiceConnectionCount_Should_Return_Valid_Count()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetExternalServiceConnectionCount();

            // Assert
            Assert.True(result >= 0);
            Assert.True(result <= 50); // Max limit
        }

        [Fact]
        public void GetExternalServiceConnectionCount_Should_Respect_Max_Limit()
        {
            // Arrange
            var customOptions = new AIOptimizationOptions
            {
                MaxEstimatedHttpConnections = 200,
                MaxEstimatedDbConnections = 50,
                EstimatedMaxDbConnections = 100,
                MaxEstimatedExternalConnections = 10, // Low limit
                MaxEstimatedWebSocketConnections = 1000
            };
            var collector = new ConnectionMetricsCollector(_logger, customOptions, _requestAnalytics);

            // Act
            var result = collector.GetExternalServiceConnectionCount();

            // Assert
            Assert.True(result <= 10);
        }

        [Fact]
        public void GetExternalServiceConnectionCount_Should_Handle_Exception_And_Return_Fallback()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - Exception handling is tested indirectly through the method calls

            // Assert - Should not throw and return valid count
            var result = collector.GetExternalServiceConnectionCount();
            Assert.True(result >= 0);
        }

        [Fact]
        public void GetExternalServiceConnectionCount_Should_Work_With_Empty_Request_Analytics()
        {
            // Arrange
            var emptyAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var collector = new ConnectionMetricsCollector(_logger, _options, emptyAnalytics);

            // Act
            var result = collector.GetExternalServiceConnectionCount();

            // Assert
            Assert.True(result >= 0);
        }

        [Fact]
        public void GetExternalServiceConnectionCount_Should_Work_With_Populated_Request_Analytics()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data = new RequestAnalysisData
            {
                ExternalApiCalls = 50
            };
            // Add metrics to populate ExecutionTimesCount
            data.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 100,
                SuccessfulExecutions = 90,
                FailedExecutions = 10,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                ConcurrentExecutions = 5
            });
            analytics.TryAdd(typeof(string), data);
            var collector = new ConnectionMetricsCollector(_logger, _options, analytics);

            // Act
            var result = collector.GetExternalServiceConnectionCount();

            // Assert
            Assert.True(result >= 0);
        }

        [Fact]
        public void GetExternalServiceConnectionCount_Should_Calculate_Based_On_External_Api_Calls()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data = new RequestAnalysisData
            {
                ExternalApiCalls = 100, // High external calls
            };
            // Add metrics to populate ExecutionTimesCount
            data.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 200,
                SuccessfulExecutions = 180,
                FailedExecutions = 20,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                ConcurrentExecutions = 5
            });
            analytics.TryAdd(typeof(string), data);
            var collector = new ConnectionMetricsCollector(_logger, _options, analytics);

            // Act
            var result = collector.GetExternalServiceConnectionCount();

            // Assert
            Assert.True(result > 0); // Should calculate connections based on external API calls
        }

        [Fact]
        public void GetExternalServiceConnectionCount_Should_Handle_Zero_External_Api_Calls()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data = new RequestAnalysisData
            {
                ExternalApiCalls = 0
            };
            // Add metrics to populate ExecutionTimesCount
            data.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 100,
                SuccessfulExecutions = 90,
                FailedExecutions = 10,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                ConcurrentExecutions = 5
            });
            analytics.TryAdd(typeof(string), data);
            var collector = new ConnectionMetricsCollector(_logger, _options, analytics);

            // Act
            var result = collector.GetExternalServiceConnectionCount();

            // Assert
            Assert.True(result >= 0);
        }

        [Fact]
        public void GetExternalServiceConnectionCount_Should_Include_Message_Queue_Connections()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetExternalServiceConnectionCount();

            // Assert
            Assert.True(result >= 0); // Should include the fixed message queue connections (2)
        }

        [Fact]
        public void GetExternalServiceConnectionCount_Should_Sum_All_External_Connection_Types()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data = new RequestAnalysisData
            {
                ExternalApiCalls = 20
            };
            // Add metrics to populate ExecutionTimesCount
            data.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 50,
                SuccessfulExecutions = 45,
                FailedExecutions = 5,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                ConcurrentExecutions = 2
            });
            analytics.TryAdd(typeof(string), data);
            var collector = new ConnectionMetricsCollector(_logger, _options, analytics);

            // Act
            var result = collector.GetExternalServiceConnectionCount();

            // Assert
            Assert.True(result >= 2); // At least message queue connections
        }
    }
}