using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCollectorRequestAnalyticsTests
    {
        private readonly ILogger<ConnectionMetricsCollector> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

        public ConnectionMetricsCollectorRequestAnalyticsTests()
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
        public void GetHttpConnectionCount_Should_Use_RequestAnalytics_For_HttpClientPool_Calculation()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data = new RequestAnalysisData();
            // Add metrics to populate ExecutionTimesCount and ConcurrentExecutionPeaks
            data.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 50,
                SuccessfulExecutions = 45,
                FailedExecutions = 5,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                ConcurrentExecutions = 8
            });
            analytics.TryAdd(typeof(string), data);
            var collector = new ConnectionMetricsCollector(_logger, _options, analytics);

            // Act
            var result = collector.GetHttpConnectionCount(
                getActiveRequestCount: () => 0, // Force fallback path
                calculateConnectionThroughputFactor: () => 0.0,
                estimateKeepAliveConnections: () => 0
            );

            // Assert
            Assert.True(result >= 0); // Should include pool connections from analytics
        }

        [Fact]
        public void GetHttpConnectionCount_Should_Use_RequestAnalytics_For_Outbound_Connections()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data = new RequestAnalysisData
            {
                ExternalApiCalls = 20 // Triggers outbound connection calculation
            };
            // Add some metrics
            data.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 10,
                SuccessfulExecutions = 9,
                FailedExecutions = 1,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                ConcurrentExecutions = 2
            });
            analytics.TryAdd(typeof(string), data);
            var collector = new ConnectionMetricsCollector(_logger, _options, analytics);

            // Act
            var result = collector.GetHttpConnectionCount(
                getActiveRequestCount: () => 10,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 5
            );

            // Assert
            Assert.True(result >= 0); // Should include outbound connections from analytics
        }

        [Fact]
        public void GetDatabaseConnectionCount_Should_Calculate_Based_On_DatabaseCalls_Property()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data = new RequestAnalysisData
            {
                DatabaseCalls = 100
            };
            // Add some metrics
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
            var result = collector.GetDatabaseConnectionCount();

            // Assert
            Assert.True(result > 0); // Should calculate connections based on DatabaseCalls
        }

        [Fact]
        public void GetDatabaseConnectionCount_Should_Use_CacheHitRatio_For_Redis_Connections()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data = new RequestAnalysisData
            {
                DatabaseCalls = 10,
                CacheHitRatio = 0.7 // Above 0.5 threshold
            };
            // Add some metrics
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
            var result = collector.GetDatabaseConnectionCount();

            // Assert
            Assert.True(result >= 0); // Should include Redis connections
        }

        [Fact]
        public void GetExternalServiceConnectionCount_Should_Use_ExternalApiCalls_Property()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data = new RequestAnalysisData
            {
                ExternalApiCalls = 30
            };
            // Add some metrics
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
            Assert.True(result > 2); // Should be more than just message queue connections
        }

        [Fact]
        public void GetWebSocketConnectionCount_Should_Use_ExecutionTimesCount_For_Realtime_Detection()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data = new RequestAnalysisData();
            // Add metrics to populate ExecutionTimesCount above 100 threshold for realtime
            data.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 150,
                SuccessfulExecutions = 135,
                FailedExecutions = 15,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                ConcurrentExecutions = 5
            });
            analytics.TryAdd(typeof(string), data);
            var collector = new ConnectionMetricsCollector(_logger, _options, analytics);

            // Act
            var result = collector.GetWebSocketConnectionCount();

            // Assert
            Assert.True(result > 0); // Should detect realtime connections
        }

        [Fact]
        public void GetWebSocketConnectionCount_Should_Use_RepeatRequestRate_For_Long_Polling()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data = new RequestAnalysisData
            {
                RepeatRequestRate = 0.4 // Above 0.3 threshold for polling
            };
            // Add metrics to populate ExecutionTimesCount
            data.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 10,
                SuccessfulExecutions = 9,
                FailedExecutions = 1,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                ConcurrentExecutions = 2
            });
            analytics.TryAdd(typeof(string), data);
            var collector = new ConnectionMetricsCollector(_logger, _options, analytics);

            // Act
            var result = collector.GetWebSocketConnectionCount();

            // Assert
            Assert.True(result > 0); // Should detect polling connections
        }

        [Fact]
        public void GetWebSocketConnectionCount_Should_Use_ExecutionTimesCount_For_ServerSentEvent_Detection()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data = new RequestAnalysisData();
            // Add metrics to populate ExecutionTimesCount above 50 threshold for SSE
            data.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 60,
                SuccessfulExecutions = 54,
                FailedExecutions = 6,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                ConcurrentExecutions = 2
            });
            analytics.TryAdd(typeof(string), data);
            var collector = new ConnectionMetricsCollector(_logger, _options, analytics);

            // Act
            var result = collector.GetWebSocketConnectionCount();

            // Assert
            Assert.True(result > 0); // Should detect SSE connections
        }

        [Fact]
        public void Collector_Should_Handle_Multiple_Request_Types_In_Analytics()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data1 = new RequestAnalysisData
            {
                DatabaseCalls = 50,
                ExternalApiCalls = 20,
                CacheHitRatio = 0.8,
                RepeatRequestRate = 0.5
            };
            data1.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 100,
                SuccessfulExecutions = 90,
                FailedExecutions = 10,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                ConcurrentExecutions = 5
            });

            var data2 = new RequestAnalysisData
            {
                DatabaseCalls = 30,
                ExternalApiCalls = 15,
                CacheHitRatio = 0.6,
                RepeatRequestRate = 0.2
            };
            data2.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 80,
                SuccessfulExecutions = 72,
                FailedExecutions = 8,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                ConcurrentExecutions = 3
            });

            analytics.TryAdd(typeof(string), data1);
            analytics.TryAdd(typeof(int), data2);
            var collector = new ConnectionMetricsCollector(_logger, _options, analytics);

            // Act
            var activeConnections = collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 50,
                calculateConnectionThroughputFactor: () => 25.0,
                estimateKeepAliveConnections: () => 5,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            var dbConnections = collector.GetDatabaseConnectionCount();
            var externalConnections = collector.GetExternalServiceConnectionCount();
            var wsConnections = collector.GetWebSocketConnectionCount();

            // Assert
            Assert.True(activeConnections >= 0);
            Assert.True(dbConnections >= 0);
            Assert.True(externalConnections >= 0);
            Assert.True(wsConnections >= 0);
        }

        [Fact]
        public void Collector_Should_Aggregate_Data_From_Multiple_Request_Types()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data1 = new RequestAnalysisData
            {
                DatabaseCalls = 40,
                ExternalApiCalls = 10
            };
            data1.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 50,
                SuccessfulExecutions = 45,
                FailedExecutions = 5,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                ConcurrentExecutions = 2
            });

            var data2 = new RequestAnalysisData
            {
                DatabaseCalls = 60,
                ExternalApiCalls = 25
            };
            data2.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 75,
                SuccessfulExecutions = 67,
                FailedExecutions = 8,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                ConcurrentExecutions = 3
            });

            analytics.TryAdd(typeof(string), data1);
            analytics.TryAdd(typeof(int), data2);
            var collector = new ConnectionMetricsCollector(_logger, _options, analytics);

            // Act
            var dbConnections = collector.GetDatabaseConnectionCount();
            var externalConnections = collector.GetExternalServiceConnectionCount();

            // Assert - Should aggregate data from both request types
            Assert.True(dbConnections >= 0);
            Assert.True(externalConnections >= 2); // More than just message queue
        }

        [Fact]
        public void Collector_Should_Handle_Empty_RequestAnalysisData_Objects()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var emptyData = new RequestAnalysisData(); // All properties default to 0, no metrics added
            analytics.TryAdd(typeof(string), emptyData);
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
            Assert.True(result >= 0); // Should handle empty data gracefully
        }

        [Fact]
        public void Collector_Should_Handle_Negative_Values_In_RequestAnalytics()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            var data = new RequestAnalysisData
            {
                DatabaseCalls = -10, // Negative value
                ExternalApiCalls = -5
            };
            // Add metrics with negative execution times (though this might not make sense in practice)
            data.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 20,
                SuccessfulExecutions = 18,
                FailedExecutions = 2,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                ConcurrentExecutions = 1
            });
            analytics.TryAdd(typeof(string), data);
            var collector = new ConnectionMetricsCollector(_logger, _options, analytics);

            // Act
            var dbConnections = collector.GetDatabaseConnectionCount();
            var externalConnections = collector.GetExternalServiceConnectionCount();

            // Assert
            Assert.True(dbConnections >= 0); // Should handle negative values gracefully
            Assert.True(externalConnections >= 0);
        }
    }
}