using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCollectorTests
    {
        private readonly ILogger<ConnectionMetricsCollector> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

        public ConnectionMetricsCollectorTests()
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

        #region Constructor Tests

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

        #endregion

        #region GetActiveConnectionCount Tests

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

        #endregion

        #region GetHttpConnectionCount Tests

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

        #endregion

        #region GetDatabaseConnectionCount Tests

        // Note: GetDatabaseConnectionCount is a private method, so we test it indirectly
        // through GetActiveConnectionCount

        #endregion

        #region GetExternalServiceConnectionCount Tests

        // Note: GetExternalServiceConnectionCount is a private method, so we test it indirectly
        // through GetActiveConnectionCount

        #endregion

        #region GetWebSocketConnectionCount Tests

        // Note: GetWebSocketConnectionCount is a private method, so we test it indirectly
        // through GetActiveConnectionCount

        #endregion

        #region Concurrent Access Tests

        [Fact]
        public void GetActiveConnectionCount_Should_Handle_Concurrent_Calls()
        {
            // Arrange
            var collector = CreateCollector();
            var exceptions = new ConcurrentBag<Exception>();

            // Act
            System.Threading.Tasks.Parallel.For(0, 50, i =>
            {
                try
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
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Assert
            Assert.Empty(exceptions);
        }

        [Fact]
        public void GetHttpConnectionCount_Should_Handle_Concurrent_Calls()
        {
            // Arrange
            var collector = CreateCollector();
            var exceptions = new ConcurrentBag<Exception>();

            // Act
            System.Threading.Tasks.Parallel.For(0, 50, i =>
            {
                try
                {
                    var result = collector.GetHttpConnectionCount(
                        getActiveRequestCount: () => i,
                        calculateConnectionThroughputFactor: () => i * 2.0,
                        estimateKeepAliveConnections: () => i / 2
                    );
                    Assert.True(result >= 0);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Assert
            Assert.Empty(exceptions);
        }

        #endregion

        #region Edge Case Tests

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

        #endregion

        #region Sequential Operations Tests

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

        #endregion

        #region Custom Options Tests

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

        #endregion

        #region Request Analytics Tests

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

        #endregion

        #region Filter Function Tests

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

        #endregion

        #region Cache Function Tests

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

        #endregion
    }
}
