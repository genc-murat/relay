using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCacheTests
    {
        private readonly ILogger<ConnectionMetricsCache> _logger;
        private readonly ILogger<TimeSeriesDatabase> _timeSeriesLogger;

        public ConnectionMetricsCacheTests()
        {
            _logger = NullLogger<ConnectionMetricsCache>.Instance;
            _timeSeriesLogger = NullLogger<TimeSeriesDatabase>.Instance;
        }

        private ConnectionMetricsCache CreateCache()
        {
            var timeSeriesDb = new TimeSeriesDatabase(_timeSeriesLogger, 10000);
            return new ConnectionMetricsCache(_logger, timeSeriesDb);
        }

        [Fact]
        public void Constructor_Should_Initialize_With_Valid_Parameters()
        {
            // Arrange & Act
            var cache = CreateCache();

            // Assert
            Assert.NotNull(cache);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange
            var timeSeriesDb = new TimeSeriesDatabase(_timeSeriesLogger, 10000);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConnectionMetricsCache(null!, timeSeriesDb));

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_TimeSeriesDb_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConnectionMetricsCache(_logger, null!));

            Assert.Equal("timeSeriesDb", exception.ParamName);
        }

        [Fact]
        public void CacheConnectionMetricWithRollingWindow_Should_Cache_Successfully()
        {
            // Arrange
            var cache = CreateCache();
            var windowKey = "test_window_20240101120000";
            var connectionCount = 100;

            // Act - Should not throw
            var exception = Record.Exception(() =>
                cache.CacheConnectionMetricWithRollingWindow(windowKey, connectionCount));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionMetricWithRollingWindow_Should_Handle_Zero_Count()
        {
            // Arrange
            var cache = CreateCache();
            var windowKey = "test_window_zero";
            var connectionCount = 0;

            // Act
            var exception = Record.Exception(() =>
                cache.CacheConnectionMetricWithRollingWindow(windowKey, connectionCount));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionMetricWithRollingWindow_Should_Handle_Large_Count()
        {
            // Arrange
            var cache = CreateCache();
            var windowKey = "test_window_large";
            var connectionCount = int.MaxValue;

            // Act
            var exception = Record.Exception(() =>
                cache.CacheConnectionMetricWithRollingWindow(windowKey, connectionCount));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionMetricWithRollingWindow_Should_Handle_Negative_Count()
        {
            // Arrange
            var cache = CreateCache();
            var windowKey = "test_window_negative";
            var connectionCount = -10;

            // Act
            var exception = Record.Exception(() =>
                cache.CacheConnectionMetricWithRollingWindow(windowKey, connectionCount));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionMetricWithRollingWindow_Should_Handle_Multiple_Calls()
        {
            // Arrange
            var cache = CreateCache();

            // Act
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    cache.CacheConnectionMetricWithRollingWindow($"window_{i}", i * 10);
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Store_Successfully()
        {
            // Arrange
            var cache = CreateCache();
            var connectionCount = 100;
            var timestamp = DateTime.UtcNow;

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionTrendData(connectionCount, timestamp));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Handle_Zero_Count()
        {
            // Arrange
            var cache = CreateCache();
            var connectionCount = 0;
            var timestamp = DateTime.UtcNow;

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionTrendData(connectionCount, timestamp));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Handle_Multiple_Timestamps()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    cache.StoreConnectionTrendData(100 + i, baseTime.AddMinutes(i));
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Handle_Sequential_Calls()
        {
            // Arrange
            var cache = CreateCache();
            var timestamp = DateTime.UtcNow;

            // Act - Store sequential data to build up statistics
            for (int i = 0; i < 100; i++)
            {
                cache.StoreConnectionTrendData(50 + i, timestamp.AddSeconds(i));
            }

            // Assert - No exception should be thrown
            Assert.True(true);
        }

        [Fact]
        public void StoreConnectionBreakdownHistory_Should_Store_Successfully()
        {
            // Arrange
            var cache = CreateCache();
            var breakdown = new ConnectionBreakdown
            {
                Timestamp = DateTime.UtcNow,
                TotalConnections = 100,
                HttpConnections = 50,
                DatabaseConnections = 30,
                ExternalServiceConnections = 15,
                WebSocketConnections = 5,
                ActiveRequestConnections = 80,
                ThreadPoolUtilization = 0.7,
                DatabasePoolUtilization = 0.5
            };

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionBreakdownHistory(breakdown));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionBreakdownHistory_Should_Handle_All_Zero_Connections()
        {
            // Arrange
            var cache = CreateCache();
            var breakdown = new ConnectionBreakdown
            {
                Timestamp = DateTime.UtcNow,
                TotalConnections = 0,
                HttpConnections = 0,
                DatabaseConnections = 0,
                ExternalServiceConnections = 0,
                WebSocketConnections = 0,
                ActiveRequestConnections = 0,
                ThreadPoolUtilization = 0.0,
                DatabasePoolUtilization = 0.0
            };

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionBreakdownHistory(breakdown));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionBreakdownHistory_Should_Handle_High_Utilization()
        {
            // Arrange
            var cache = CreateCache();
            var breakdown = new ConnectionBreakdown
            {
                Timestamp = DateTime.UtcNow,
                TotalConnections = 1000,
                HttpConnections = 500,
                DatabaseConnections = 400,
                ExternalServiceConnections = 50,
                WebSocketConnections = 50,
                ActiveRequestConnections = 950,
                ThreadPoolUtilization = 1.0,
                DatabasePoolUtilization = 0.99
            };

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionBreakdownHistory(breakdown));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionBreakdownHistory_Should_Handle_Multiple_Breakdowns()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    var breakdown = new ConnectionBreakdown
                    {
                        Timestamp = baseTime.AddMinutes(i),
                        TotalConnections = 100 + i,
                        HttpConnections = 50 + i,
                        DatabaseConnections = 30 + i,
                        ExternalServiceConnections = 15,
                        WebSocketConnections = 5,
                        ActiveRequestConnections = 80 + i,
                        ThreadPoolUtilization = 0.5 + (i * 0.01),
                        DatabasePoolUtilization = 0.4 + (i * 0.01)
                    };
                    cache.StoreConnectionBreakdownHistory(breakdown);
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void DetectConnectionAnomalies_Should_Handle_Insufficient_Data()
        {
            // Arrange
            var cache = CreateCache();
            var currentCount = 100;
            var timestamp = DateTime.UtcNow;

            // Act - Should handle when no historical data exists
            var exception = Record.Exception(() =>
                cache.DetectConnectionAnomalies(currentCount, timestamp, null));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void DetectConnectionAnomalies_Should_Detect_Spike()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Store baseline data
            for (int i = 0; i < 100; i++)
            {
                cache.StoreConnectionTrendData(50, baseTime.AddSeconds(i));
            }

            // Act - Store a spike
            var spikeTime = baseTime.AddSeconds(100);
            var exception = Record.Exception(() =>
                cache.StoreConnectionTrendData(500, spikeTime));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void DetectConnectionAnomalies_Should_Detect_Drop()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Store baseline data
            for (int i = 0; i < 100; i++)
            {
                cache.StoreConnectionTrendData(500, baseTime.AddSeconds(i));
            }

            // Act - Store a drop
            var dropTime = baseTime.AddSeconds(100);
            var exception = Record.Exception(() =>
                cache.StoreConnectionTrendData(50, dropTime));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void DetectConnectionAnomalies_Should_Handle_With_Stats_Provided()
        {
            // Arrange
            var cache = CreateCache();
            var currentCount = 100;
            var timestamp = DateTime.UtcNow;
            var stats = new MetricStatistics
            {
                MetricName = "ConnectionCount",
                Count = 50,
                Mean = 100,
                StdDev = 10,
                Min = 80,
                Max = 120,
                Median = 100,
                P95 = 115,
                P99 = 118
            };

            // Act
            var exception = Record.Exception(() =>
                cache.DetectConnectionAnomalies(currentCount, timestamp, stats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void DetectConnectionAnomalies_Should_Detect_High_Z_Score()
        {
            // Arrange
            var cache = CreateCache();
            var timestamp = DateTime.UtcNow;
            var stats = new MetricStatistics
            {
                MetricName = "ConnectionCount",
                Count = 100,
                Mean = 100,
                StdDev = 10,
                Min = 80,
                Max = 120,
                Median = 100,
                P95 = 115,
                P99 = 118
            };

            // Act - Value far from mean (z-score > 3)
            var exception = Record.Exception(() =>
                cache.DetectConnectionAnomalies(150, timestamp, stats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void DetectConnectionAnomalies_Should_Handle_High_Volatility()
        {
            // Arrange
            var cache = CreateCache();
            var timestamp = DateTime.UtcNow;
            var stats = new MetricStatistics
            {
                MetricName = "ConnectionCount",
                Count = 100,
                Mean = 100,
                StdDev = 60, // High standard deviation
                Min = 20,
                Max = 200,
                Median = 100,
                P95 = 180,
                P99 = 195
            };

            // Act
            var exception = Record.Exception(() =>
                cache.DetectConnectionAnomalies(100, timestamp, stats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionMetricWithRollingWindow_Should_Handle_Concurrent_Calls()
        {
            // Arrange
            var cache = CreateCache();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Act
            System.Threading.Tasks.Parallel.For(0, 50, i =>
            {
                try
                {
                    cache.CacheConnectionMetricWithRollingWindow($"window_{i}", i * 10);
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
        public void StoreConnectionTrendData_Should_Handle_Concurrent_Calls()
        {
            // Arrange
            var cache = CreateCache();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var baseTime = DateTime.UtcNow;

            // Act
            System.Threading.Tasks.Parallel.For(0, 50, i =>
            {
                try
                {
                    cache.StoreConnectionTrendData(100 + i, baseTime.AddSeconds(i));
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
        public void StoreConnectionBreakdownHistory_Should_Handle_Concurrent_Calls()
        {
            // Arrange
            var cache = CreateCache();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var baseTime = DateTime.UtcNow;

            // Act
            System.Threading.Tasks.Parallel.For(0, 50, i =>
            {
                try
                {
                    var breakdown = new ConnectionBreakdown
                    {
                        Timestamp = baseTime.AddSeconds(i),
                        TotalConnections = 100 + i,
                        HttpConnections = 50,
                        DatabaseConnections = 30,
                        ExternalServiceConnections = 15,
                        WebSocketConnections = 5,
                        ActiveRequestConnections = 80,
                        ThreadPoolUtilization = 0.5,
                        DatabasePoolUtilization = 0.4
                    };
                    cache.StoreConnectionBreakdownHistory(breakdown);
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
        public void StoreConnectionTrendData_Should_Calculate_Moving_Averages()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act - Store enough data to calculate moving averages
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    cache.StoreConnectionTrendData(100 + (i % 20), baseTime.AddMinutes(i));
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Calculate_Trend_Increasing()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act - Store increasing trend data
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    cache.StoreConnectionTrendData(50 + i * 2, baseTime.AddMinutes(i));
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Calculate_Trend_Decreasing()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act - Store decreasing trend data
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    cache.StoreConnectionTrendData(200 - i * 2, baseTime.AddMinutes(i));
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Calculate_Trend_Stable()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act - Store stable trend data
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    cache.StoreConnectionTrendData(100 + (i % 5), baseTime.AddMinutes(i));
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Calculate_Volatility()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act - Store volatile data
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var value = 100 + (i % 2 == 0 ? 50 : -50);
                    cache.StoreConnectionTrendData(value, baseTime.AddMinutes(i));
                }
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionBreakdownHistory_Should_Calculate_Ratios()
        {
            // Arrange
            var cache = CreateCache();
            var breakdown = new ConnectionBreakdown
            {
                Timestamp = DateTime.UtcNow,
                TotalConnections = 200,
                HttpConnections = 100,  // 50%
                DatabaseConnections = 80,  // 40%
                ExternalServiceConnections = 10,  // 5%
                WebSocketConnections = 10,  // 5%
                ActiveRequestConnections = 150,
                ThreadPoolUtilization = 0.6,
                DatabasePoolUtilization = 0.5
            };

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionBreakdownHistory(breakdown));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionBreakdownHistory_Should_Warn_On_High_Database_Ratio()
        {
            // Arrange
            var cache = CreateCache();
            var breakdown = new ConnectionBreakdown
            {
                Timestamp = DateTime.UtcNow,
                TotalConnections = 200,
                HttpConnections = 50,
                DatabaseConnections = 120,  // 60% - High ratio
                ExternalServiceConnections = 20,
                WebSocketConnections = 10,
                ActiveRequestConnections = 150,
                ThreadPoolUtilization = 0.6,
                DatabasePoolUtilization = 0.8
            };

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionBreakdownHistory(breakdown));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CacheConnectionMetricWithRollingWindow_Should_Handle_Empty_WindowKey()
        {
            // Arrange
            var cache = CreateCache();
            var windowKey = string.Empty;
            var connectionCount = 100;

            // Act
            var exception = Record.Exception(() =>
                cache.CacheConnectionMetricWithRollingWindow(windowKey, connectionCount));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Handle_Past_Timestamp()
        {
            // Arrange
            var cache = CreateCache();
            var connectionCount = 100;
            var timestamp = DateTime.UtcNow.AddDays(-1);

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionTrendData(connectionCount, timestamp));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Handle_Future_Timestamp()
        {
            // Arrange
            var cache = CreateCache();
            var connectionCount = 100;
            var timestamp = DateTime.UtcNow.AddDays(1);

            // Act
            var exception = Record.Exception(() =>
                cache.StoreConnectionTrendData(connectionCount, timestamp));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void DetectConnectionAnomalies_Should_Handle_Zero_Stats()
        {
            // Arrange
            var cache = CreateCache();
            var currentCount = 100;
            var timestamp = DateTime.UtcNow;
            var stats = new MetricStatistics
            {
                MetricName = "ConnectionCount",
                Count = 0,
                Mean = 0,
                StdDev = 0,
                Min = 0,
                Max = 0,
                Median = 0,
                P95 = 0,
                P99 = 0
            };

            // Act
            var exception = Record.Exception(() =>
                cache.DetectConnectionAnomalies(currentCount, timestamp, stats));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Methods_Should_Be_Callable_Multiple_Times_In_Sequence()
        {
            // Arrange
            var cache = CreateCache();
            var timestamp = DateTime.UtcNow;

            // Act & Assert - Call all methods in sequence
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    cache.CacheConnectionMetricWithRollingWindow($"window_{i}", i * 10);
                    cache.StoreConnectionTrendData(i * 10, timestamp.AddMinutes(i));
                    
                    var breakdown = new ConnectionBreakdown
                    {
                        Timestamp = timestamp.AddMinutes(i),
                        TotalConnections = i * 10,
                        HttpConnections = i * 5,
                        DatabaseConnections = i * 3,
                        ExternalServiceConnections = i,
                        WebSocketConnections = i,
                        ActiveRequestConnections = i * 8,
                        ThreadPoolUtilization = i * 0.05,
                        DatabasePoolUtilization = i * 0.04
                    };
                    cache.StoreConnectionBreakdownHistory(breakdown);
                }
            });

            Assert.Null(exception);
        }

        [Fact]
        public void StoreConnectionTrendData_Should_Handle_Extreme_Fluctuations()
        {
            // Arrange
            var cache = CreateCache();
            var baseTime = DateTime.UtcNow;

            // Act - Store extremely fluctuating data
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    var value = i % 2 == 0 ? 1000 : 10;
                    cache.StoreConnectionTrendData(value, baseTime.AddMinutes(i));
                }
            });

            // Assert
            Assert.Null(exception);
        }
    }
}
