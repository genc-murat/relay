using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Strategies;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class CachingStrategyManagerTests
    {
        private readonly ILogger<CachingStrategyManager> _logger;
        private readonly ILogger<ConnectionMetricsCache> _cacheLogger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;

        public CachingStrategyManagerTests()
        {
            _logger = NullLogger<CachingStrategyManager>.Instance;
            _cacheLogger = NullLogger<ConnectionMetricsCache>.Instance;
            _timeSeriesLogger = NullLogger<TimeSeriesDatabase>.Instance;
        }

        private CachingStrategyManager CreateManager()
        {
            var timeSeriesDb = new TimeSeriesDatabase(_timeSeriesLogger, 10000);
            var metricsCache = new ConnectionMetricsCache(_cacheLogger, timeSeriesDb);
            return new CachingStrategyManager(_logger, metricsCache);
        }

        [Fact]
        public void Constructor_Should_Initialize_With_Valid_Parameters()
        {
            // Arrange & Act
            var manager = CreateManager();

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange
            var timeSeriesDb = new TimeSeriesDatabase(_timeSeriesLogger, 10000);
            var metricsCache = new ConnectionMetricsCache(_cacheLogger, timeSeriesDb);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CachingStrategyManager(null!, metricsCache));

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_MetricsCache_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CachingStrategyManager(_logger, null!));

            Assert.Equal("metricsCache", exception.ParamName);
        }

        [Fact]
        public void CacheConnectionCount_Should_Cache_Successfully()
        {
            // Arrange
            var manager = CreateManager();
            var connectionCount = 100;

            // Act - Should not throw
            var exception = Record.Exception(() =>
                manager.CacheConnectionCount(
                    connectionCount,
                    () => 50,  // HTTP
                    () => 30,  // Database
                    () => 15,  // External
                    () => 5,   // WebSocket
                    () => 80,  // Active Requests
                    () => 0.7, // Thread Pool
                    () => 0.5  // Database Pool
                ));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionCount_Should_Handle_Zero_Connections()
        {
            // Arrange
            var manager = CreateManager();
            var connectionCount = 0;

            // Act
            var exception = Record.Exception(() =>
                manager.CacheConnectionCount(
                    connectionCount,
                    () => 0,
                    () => 0,
                    () => 0,
                    () => 0,
                    () => 0,
                    () => 0.0,
                    () => 0.0
                ));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionCount_Should_Handle_Large_Connection_Count()
        {
            // Arrange
            var manager = CreateManager();
            var connectionCount = 10000;

            // Act
            var exception = Record.Exception(() =>
                manager.CacheConnectionCount(
                    connectionCount,
                    () => 5000,
                    () => 3000,
                    () => 1500,
                    () => 500,
                    () => 8000,
                    () => 0.95,
                    () => 0.85
                ));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionCount_Should_Call_All_Provider_Functions()
        {
            // Arrange
            var manager = CreateManager();
            var httpCalled = false;
            var dbCalled = false;
            var externalCalled = false;
            var wsCalled = false;
            var activeRequestsCalled = false;
            var threadPoolCalled = false;
            var dbPoolCalled = false;

            // Act
            manager.CacheConnectionCount(
                100,
                () => { httpCalled = true; return 50; },
                () => { dbCalled = true; return 30; },
                () => { externalCalled = true; return 15; },
                () => { wsCalled = true; return 5; },
                () => { activeRequestsCalled = true; return 80; },
                () => { threadPoolCalled = true; return 0.7; },
                () => { dbPoolCalled = true; return 0.5; }
            );

            // Assert
            Assert.True(httpCalled, "HTTP connection count provider should be called");
            Assert.True(dbCalled, "Database connection count provider should be called");
            Assert.True(externalCalled, "External service connection count provider should be called");
            Assert.True(wsCalled, "WebSocket connection count provider should be called");
            Assert.True(activeRequestsCalled, "Active request count provider should be called");
            Assert.True(threadPoolCalled, "Thread pool utilization provider should be called");
            Assert.True(dbPoolCalled, "Database pool utilization provider should be called");
        }

        [Fact]
        public void CacheConnectionCount_Should_Handle_Exception_In_Provider_Functions()
        {
            // Arrange
            var manager = CreateManager();

            // Act - One of the provider functions throws
            var exception = Record.Exception(() =>
                manager.CacheConnectionCount(
                    100,
                    () => throw new InvalidOperationException("Provider error"),
                    () => 30,
                    () => 15,
                    () => 5,
                    () => 80,
                    () => 0.7,
                    () => 0.5
                ));

            // Assert - Exception should be caught and handled
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionCount_Should_Handle_Negative_Connection_Count()
        {
            // Arrange
            var manager = CreateManager();
            var connectionCount = -10; // Negative value (edge case)

            // Act
            var exception = Record.Exception(() =>
                manager.CacheConnectionCount(
                    connectionCount,
                    () => 0,
                    () => 0,
                    () => 0,
                    () => 0,
                    () => 0,
                    () => 0.0,
                    () => 0.0
                ));

            // Assert - Should still process without throwing
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionCount_Should_Handle_High_Utilization_Values()
        {
            // Arrange
            var manager = CreateManager();

            // Act
            var exception = Record.Exception(() =>
                manager.CacheConnectionCount(
                    1000,
                    () => 500,
                    () => 400,
                    () => 50,
                    () => 50,
                    () => 900,
                    () => 1.0,  // 100% thread pool utilization
                    () => 0.99  // 99% database pool utilization
                ));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionCount_Should_Be_Idempotent()
        {
            // Arrange
            var manager = CreateManager();
            var connectionCount = 100;

            // Act - Call multiple times with same data
            var exception = Record.Exception(() =>
            {
                manager.CacheConnectionCount(connectionCount, () => 50, () => 30, () => 15, () => 5, () => 80, () => 0.7, () => 0.5);
                manager.CacheConnectionCount(connectionCount, () => 50, () => 30, () => 15, () => 5, () => 80, () => 0.7, () => 0.5);
                manager.CacheConnectionCount(connectionCount, () => 50, () => 30, () => 15, () => 5, () => 80, () => 0.7, () => 0.5);
            });

            // Assert - Each call should be processed without error
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionCount_Should_Handle_Concurrent_Calls()
        {
            // Arrange
            var manager = CreateManager();
            var callCount = 10;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Act - Make concurrent calls
            System.Threading.Tasks.Parallel.For(0, callCount, i =>
            {
                try
                {
                    manager.CacheConnectionCount(
                        100 + i,
                        () => 50,
                        () => 30,
                        () => 15,
                        () => 5,
                        () => 80,
                        () => 0.7,
                        () => 0.5
                    );
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Assert - All calls should complete without errors
            Assert.Empty(exceptions);
        }

        [Fact]
        public void CacheConnectionCount_Should_Accept_Fractional_Utilization_Values()
        {
            // Arrange
            var manager = CreateManager();

            // Act
            var exception = Record.Exception(() =>
                manager.CacheConnectionCount(
                    100,
                    () => 50,
                    () => 30,
                    () => 15,
                    () => 5,
                    () => 80,
                    () => 0.123456,  // Fractional thread pool utilization
                    () => 0.789012   // Fractional database pool utilization
                ));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionCount_Should_Handle_Maximum_Integer_Connection_Count()
        {
            // Arrange
            var manager = CreateManager();

            // Act
            var exception = Record.Exception(() =>
                manager.CacheConnectionCount(
                    int.MaxValue,
                    () => 1000000,
                    () => 1000000,
                    () => 1000000,
                    () => 1000000,
                    () => 1000000,
                    () => 1.0,
                    () => 1.0
                ));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionCount_Should_Work_With_All_Zero_Values()
        {
            // Arrange
            var manager = CreateManager();

            // Act
            var exception = Record.Exception(() =>
                manager.CacheConnectionCount(
                    0,
                    () => 0,
                    () => 0,
                    () => 0,
                    () => 0,
                    () => 0,
                    () => 0.0,
                    () => 0.0
                ));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionCount_Should_Handle_Mismatched_Connection_Totals()
        {
            // Arrange
            var manager = CreateManager();

            // Act - Total doesn't match sum of individual types (real-world scenario)
            var exception = Record.Exception(() =>
                manager.CacheConnectionCount(
                    100,  // Total
                    () => 60,  // HTTP
                    () => 40,  // Database (sum = 100, but real total might differ)
                    () => 20,  // External
                    () => 10,  // WebSocket
                    () => 80,
                    () => 0.7,
                    () => 0.5
                ));

            // Assert - Should handle gracefully
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionCount_Should_Handle_Different_Connection_Patterns()
        {
            // Arrange
            var manager = CreateManager();

            // Act & Assert - Test various patterns
            var exception1 = Record.Exception(() =>
                manager.CacheConnectionCount(100, () => 100, () => 0, () => 0, () => 0, () => 100, () => 0.5, () => 0.1));
            Assert.Null(exception1);

            var exception2 = Record.Exception(() =>
                manager.CacheConnectionCount(100, () => 0, () => 100, () => 0, () => 0, () => 50, () => 0.2, () => 0.9));
            Assert.Null(exception2);

            var exception3 = Record.Exception(() =>
                manager.CacheConnectionCount(100, () => 0, () => 0, () => 100, () => 0, () => 80, () => 0.8, () => 0.3));
            Assert.Null(exception3);

            var exception4 = Record.Exception(() =>
                manager.CacheConnectionCount(100, () => 0, () => 0, () => 0, () => 100, () => 100, () => 0.6, () => 0.2));
            Assert.Null(exception4);
        }

        [Fact]
        public void CacheConnectionCount_Should_Handle_Rapid_Sequential_Calls()
        {
            // Arrange
            var manager = CreateManager();

            // Act - Make rapid sequential calls
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    manager.CacheConnectionCount(
                        i,
                        () => i / 2,
                        () => i / 3,
                        () => i / 4,
                        () => i / 5,
                        () => i,
                        () => (double)i / 200.0,
                        () => (double)i / 300.0
                    );
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionCount_Should_Handle_Provider_Functions_Returning_Same_Values()
        {
            // Arrange
            var manager = CreateManager();

            // Act - All providers return the same value
            var exception = Record.Exception(() =>
                manager.CacheConnectionCount(
                    100,
                    () => 25,
                    () => 25,
                    () => 25,
                    () => 25,
                    () => 25,
                    () => 0.5,
                    () => 0.5
                ));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionCount_Should_Handle_Boundary_Utilization_Values()
        {
            // Arrange
            var manager = CreateManager();

            // Act - Test with 0% and 100% utilization
            var exception = Record.Exception(() =>
                manager.CacheConnectionCount(
                    100,
                    () => 50,
                    () => 30,
                    () => 15,
                    () => 5,
                    () => 80,
                    () => 0.0,  // 0% utilization
                    () => 1.0   // 100% utilization
                ));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionCount_Should_Be_Called_Multiple_Times_Without_State_Corruption()
        {
            // Arrange
            var manager = CreateManager();

            // Act - Call with varying data
            var exceptions = new System.Collections.Generic.List<Exception>();

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    manager.CacheConnectionCount(
                        i * 10,
                        () => i * 5,
                        () => i * 3,
                        () => i * 2,
                        () => i,
                        () => i * 8,
                        () => i / 20.0,
                        () => i / 30.0
                    );
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            // Assert
            Assert.Empty(exceptions);
        }
    }
}
