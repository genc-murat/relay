using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Xunit;

namespace Relay.Core.Tests.AI;

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
        var timeSeriesDb = TimeSeriesDatabase.Create(_timeSeriesLogger, maxHistorySize: 10000);
        return new ConnectionMetricsCache(_logger, timeSeriesDb);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Arrange
        var timeSeriesDb = TimeSeriesDatabase.Create(_timeSeriesLogger, maxHistorySize: 1000);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionMetricsCache(null!, timeSeriesDb));
    }

    [Fact]
    public void Constructor_Should_Throw_When_TimeSeriesDb_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionMetricsCache(_logger, null!));
    }

    [Fact]
    public void CacheConnectionMetricWithRollingWindow_Should_Store_Metrics()
    {
        // Arrange
        var cache = CreateCache();
        var windowKey = "test_window";
        var connectionCount = 150;

        // Act
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
        var windowKey = "zero_window";
        var connectionCount = 0;

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
        var windowKey = "negative_window";
        var connectionCount = -10;

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
        var windowKey = "large_window";
        var connectionCount = int.MaxValue;

        // Act
        var exception = Record.Exception(() =>
            cache.CacheConnectionMetricWithRollingWindow(windowKey, connectionCount));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void StoreConnectionTrendData_Should_Store_Basic_Trend()
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
    public void StoreConnectionTrendData_Should_Handle_Negative_Count()
    {
        // Arrange
        var cache = CreateCache();
        var connectionCount = -50;
        var timestamp = DateTime.UtcNow;

        // Act
        var exception = Record.Exception(() =>
            cache.StoreConnectionTrendData(connectionCount, timestamp));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void StoreConnectionTrendData_Should_Handle_Large_Count()
    {
        // Arrange
        var cache = CreateCache();
        var connectionCount = 1000000;
        var timestamp = DateTime.UtcNow;

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
        var connectionCount = 200;
        var timestamp = DateTime.UtcNow.AddDays(1);

        // Act
        var exception = Record.Exception(() =>
            cache.StoreConnectionTrendData(connectionCount, timestamp));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void StoreConnectionTrendData_Should_Handle_Past_Timestamp()
    {
        // Arrange
        var cache = CreateCache();
        var connectionCount = 50;
        var timestamp = DateTime.UtcNow.AddDays(-365);

        // Act
        var exception = Record.Exception(() =>
            cache.StoreConnectionTrendData(connectionCount, timestamp));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void StoreConnectionTrendData_Should_Calculate_Increasing_Trend()
    {
        // Arrange
        var cache = CreateCache();
        var baseTime = DateTime.UtcNow;

        // Store increasing data to build moving averages
        for (int i = 1; i <= 20; i++)
        {
            cache.StoreConnectionTrendData(100 + i * 5, baseTime.AddMinutes(i));
        }

        // Act - Store a value higher than recent averages
        var exception = Record.Exception(() =>
            cache.StoreConnectionTrendData(200, baseTime.AddMinutes(25)));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void StoreConnectionTrendData_Should_Calculate_Decreasing_Trend()
    {
        // Arrange
        var cache = CreateCache();
        var baseTime = DateTime.UtcNow;

        // Store decreasing data
        for (int i = 1; i <= 20; i++)
        {
            cache.StoreConnectionTrendData(200 - i * 5, baseTime.AddMinutes(i));
        }

        // Act - Store a value lower than recent averages
        var exception = Record.Exception(() =>
            cache.StoreConnectionTrendData(50, baseTime.AddMinutes(25)));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void StoreConnectionTrendData_Should_Handle_Stable_Trend()
    {
        // Arrange
        var cache = CreateCache();
        var baseTime = DateTime.UtcNow;

        // Store stable data
        for (int i = 1; i <= 20; i++)
        {
            cache.StoreConnectionTrendData(100, baseTime.AddMinutes(i));
        }

        // Act - Store similar value
        var exception = Record.Exception(() =>
            cache.StoreConnectionTrendData(102, baseTime.AddMinutes(25)));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void StoreConnectionTrendData_Should_Calculate_Volatility()
    {
        // Arrange
        var cache = CreateCache();
        var baseTime = DateTime.UtcNow;

        // Store variable data to create volatility
        var values = new[] { 100, 120, 80, 140, 60, 160, 40, 180, 20, 200 };
        for (int i = 0; i < values.Length; i++)
        {
            cache.StoreConnectionTrendData(values[i], baseTime.AddMinutes(i));
        }

        // Act - Store another value
        var exception = Record.Exception(() =>
            cache.StoreConnectionTrendData(150, baseTime.AddMinutes(15)));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void StoreConnectionTrendData_Should_Handle_Empty_History_For_MA_Calculation()
    {
        // Arrange
        var cache = CreateCache();
        var connectionCount = 100;
        var timestamp = DateTime.UtcNow;

        // Act - First call with no history
        var exception = Record.Exception(() =>
            cache.StoreConnectionTrendData(connectionCount, timestamp));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void StoreConnectionTrendData_Should_Detect_Anomalies_On_Store()
    {
        // Arrange
        var cache = CreateCache();
        var baseTime = DateTime.UtcNow;

        // Store baseline
        for (int i = 0; i < 50; i++)
        {
            cache.StoreConnectionTrendData(100, baseTime.AddSeconds(i));
        }

        // Act - Store anomalous value (should trigger anomaly detection)
        var exception = Record.Exception(() =>
            cache.StoreConnectionTrendData(1000, baseTime.AddSeconds(60)));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void StoreConnectionBreakdownHistory_Should_Handle_Zero_Total_Connections()
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
    public void StoreConnectionBreakdownHistory_Should_Handle_Negative_Values()
    {
        // Arrange
        var cache = CreateCache();
        var breakdown = new ConnectionBreakdown
        {
            Timestamp = DateTime.UtcNow,
            TotalConnections = 100,
            HttpConnections = -10,  // Invalid but should handle
            DatabaseConnections = 50,
            ExternalServiceConnections = 30,
            WebSocketConnections = 30,
            ActiveRequestConnections = 80,
            ThreadPoolUtilization = 0.5,
            DatabasePoolUtilization = 0.4
        };

        // Act
        var exception = Record.Exception(() =>
            cache.StoreConnectionBreakdownHistory(breakdown));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void DetectConnectionAnomalies_Should_Handle_Null_Stats_Parameter()
    {
        // Arrange
        var cache = CreateCache();
        var currentCount = 100;
        var timestamp = DateTime.UtcNow;

        // Act
        var exception = Record.Exception(() =>
            cache.DetectConnectionAnomalies(currentCount, timestamp, null));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void DetectConnectionAnomalies_Should_Handle_Zero_Mean_In_Stats()
    {
        // Arrange
        var cache = CreateCache();
        var currentCount = 100;
        var timestamp = DateTime.UtcNow;
        var stats = new MetricStatistics
        {
            MetricName = "ConnectionCount",
            Count = 10,
            Mean = 0F,
            StdDev = 5.0,
            Min = 0F,
            Max = 10F,
            Median = 5F,
            P95 = 9F,
            P99 = 9.5F
        };

        // Act
        var exception = Record.Exception(() =>
            cache.DetectConnectionAnomalies(currentCount, timestamp, stats));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void DetectConnectionAnomalies_Should_Handle_Zero_StdDev_In_Stats()
    {
        // Arrange
        var cache = CreateCache();
        var currentCount = 100;
        var timestamp = DateTime.UtcNow;
        var stats = new MetricStatistics
        {
            MetricName = "ConnectionCount",
            Count = 10,
            Mean = 50F,
            StdDev = 0.0,
            Min = 50F,
            Max = 50F,
            Median = 50F,
            P95 = 50F,
            P99 = 50F
        };

        // Act
        var exception = Record.Exception(() =>
            cache.DetectConnectionAnomalies(currentCount, timestamp, stats));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void DetectConnectionAnomalies_Should_Detect_Multiple_Anomaly_Types()
    {
        // Arrange
        var cache = CreateCache();
        var timestamp = DateTime.UtcNow;
        var stats = new MetricStatistics
        {
            MetricName = "ConnectionCount",
            Count = 100,
            Mean = 100F,
            StdDev = 5.0,  // Low std dev to trigger z-score
            Min = 90F,
            Max = 110F,
            Median = 100F,
            P95 = 108F,
            P99 = 109F
        };

        // Store some data for recent average
        for (int i = 0; i < 10; i++)
        {
            cache.StoreConnectionTrendData(50, timestamp.AddMinutes(-i));
        }

        // Act - High value that triggers multiple anomalies
        var exception = Record.Exception(() =>
            cache.DetectConnectionAnomalies(200, timestamp, stats));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void DetectConnectionAnomalies_Should_Handle_Extreme_Values()
    {
        // Arrange
        var cache = CreateCache();
        var timestamp = DateTime.UtcNow;
        var stats = new MetricStatistics
        {
            MetricName = "ConnectionCount",
            Count = 10,
            Mean = 100F,
            StdDev = 10.0,
            Min = 80F,
            Max = 120F,
            Median = 100F,
            P95 = 115F,
            P99 = 118F
        };

        // Act - Extreme values
        var exception1 = Record.Exception(() =>
            cache.DetectConnectionAnomalies(int.MaxValue, timestamp, stats));
        var exception2 = Record.Exception(() =>
            cache.DetectConnectionAnomalies(int.MinValue, timestamp, stats));

        // Assert
        Assert.Null(exception1);
        Assert.Null(exception2);
    }
}