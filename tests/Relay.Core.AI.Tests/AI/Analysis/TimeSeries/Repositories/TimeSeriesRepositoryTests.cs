using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.TimeSeries.Repositories;

public class TimeSeriesRepositoryTests
{
    private Mock<ILogger<TimeSeriesRepository>> _loggerMock;
    private TimeSeriesRepository _repository;

    public TimeSeriesRepositoryTests()
    {
        _loggerMock = new Mock<ILogger<TimeSeriesRepository>>();
        _repository = new TimeSeriesRepository(_loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Initialize_With_Default_MaxHistorySize()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TimeSeriesRepository>>();

        // Act
        var repository = new TimeSeriesRepository(loggerMock.Object);

        // Assert
        Assert.NotNull(repository);
        // Verify logger was called for initialization
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Time-series repository initialized")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Custom_MaxHistorySize()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TimeSeriesRepository>>();
        const int customSize = 5000;

        // Act
        var repository = new TimeSeriesRepository(loggerMock.Object, customSize);

        // Assert
        Assert.NotNull(repository);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("5000")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimeSeriesRepository(null!));
    }

    #endregion

    #region StoreMetric Tests

    [Fact]
    public void StoreMetric_Should_Store_Valid_Data()
    {
        // Arrange
        var metricName = "test_metric";
        var value = 42.5;
        var timestamp = DateTime.UtcNow;

        // Act
        _repository.StoreMetric(metricName, value, timestamp);

        // Assert
        var history = _repository.GetHistory(metricName).ToList();
        Assert.Single(history);
        Assert.Equal(metricName, history[0].MetricName);
        Assert.Equal(value, history[0].Value);
        Assert.Equal(timestamp, history[0].Timestamp);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(metricName) && o.ToString().Contains(value.ToString())),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void StoreMetric_Should_Store_With_MovingAverages_And_Trend()
    {
        // Arrange
        var metricName = "test_metric";
        var value = 42.5;
        var timestamp = DateTime.UtcNow;
        var ma5 = 40.0;
        var ma15 = 38.0;
        var trend = TrendDirection.Increasing;

        // Act
        _repository.StoreMetric(metricName, value, timestamp, ma5, ma15, trend);

        // Assert
        var history = _repository.GetHistory(metricName).ToList();
        Assert.Single(history);
        Assert.Equal(ma5, history[0].MA5);
        Assert.Equal(ma15, history[0].MA15);
        Assert.Equal((int)trend, history[0].Trend);
    }

    [Fact]
    public void StoreMetric_Should_Throw_When_MetricName_Is_Null()
    {
        // Arrange
        var value = 42.5;
        var timestamp = DateTime.UtcNow;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _repository.StoreMetric(null!, value, timestamp));
        Assert.Contains("Metric name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void StoreMetric_Should_Throw_When_MetricName_Is_Empty()
    {
        // Arrange
        var value = 42.5;
        var timestamp = DateTime.UtcNow;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _repository.StoreMetric("", value, timestamp));
        Assert.Contains("Metric name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void StoreMetric_Should_Throw_When_MetricName_Is_Whitespace()
    {
        // Arrange
        var value = 42.5;
        var timestamp = DateTime.UtcNow;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _repository.StoreMetric("   ", value, timestamp));
        Assert.Contains("Metric name cannot be null or empty", exception.Message);
    }

    #endregion

    #region StoreBatch Tests

    [Fact]
    public void StoreBatch_Should_Store_Multiple_Metrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["metric1"] = 10.0,
            ["metric2"] = 20.0
        };
        var timestamp = DateTime.UtcNow;

        // Act
        _repository.StoreBatch(metrics, timestamp);

        // Assert
        var history1 = _repository.GetHistory("metric1").ToList();
        var history2 = _repository.GetHistory("metric2").ToList();
        Assert.Single(history1);
        Assert.Single(history2);
        Assert.Equal(10.0, history1[0].Value);
        Assert.Equal(20.0, history2[0].Value);
    }

    [Fact]
    public void StoreBatch_Should_Handle_Null_Metrics()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        _repository.StoreBatch(null!, timestamp);

        // Assert - Should not throw, just return early
    }

    [Fact]
    public void StoreBatch_Should_Handle_Empty_Metrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>();
        var timestamp = DateTime.UtcNow;

        // Act
        _repository.StoreBatch(metrics, timestamp);

        // Assert - Should not throw, just return early
    }

    [Fact]
    public void StoreBatch_Should_Store_With_MovingAverages_And_Trends()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["metric1"] = 10.0
        };
        var timestamp = DateTime.UtcNow;
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["metric1"] = new MovingAverageData { MA5 = 8.0, MA15 = 7.0 }
        };
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["metric1"] = TrendDirection.Decreasing
        };

        // Act
        _repository.StoreBatch(metrics, timestamp, movingAverages, trendDirections);

        // Assert
        var history = _repository.GetHistory("metric1").ToList();
        Assert.Single(history);
        Assert.Equal(8.0, history[0].MA5);
        Assert.Equal(7.0, history[0].MA15);
        Assert.Equal((int)TrendDirection.Decreasing, history[0].Trend);
    }

    #endregion

    #region GetHistory Tests

    [Fact]
    public void GetHistory_Should_Throw_When_MetricName_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _repository.GetHistory(null!));
        Assert.Contains("Metric name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void GetHistory_Should_Throw_When_MetricName_Is_Empty()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _repository.GetHistory(""));
        Assert.Contains("Metric name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void GetHistory_Should_Return_Empty_For_NonExistent_Metric()
    {
        // Act
        var result = _repository.GetHistory("nonexistent");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetHistory_Should_Return_All_Data_Ordered_By_Timestamp()
    {
        // Arrange
        var metricName = "test_metric";
        var timestamp1 = DateTime.UtcNow.AddMinutes(-2);
        var timestamp2 = DateTime.UtcNow.AddMinutes(-1);
        var timestamp3 = DateTime.UtcNow;

        _repository.StoreMetric(metricName, 1.0, timestamp1);
        _repository.StoreMetric(metricName, 2.0, timestamp3); // Out of order
        _repository.StoreMetric(metricName, 3.0, timestamp2);

        // Act
        var result = _repository.GetHistory(metricName).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(timestamp1, result[0].Timestamp);
        Assert.Equal(timestamp2, result[1].Timestamp);
        Assert.Equal(timestamp3, result[2].Timestamp);
    }

    [Fact]
    public void GetHistory_Should_Filter_By_LookbackPeriod()
    {
        // Arrange
        var metricName = "test_metric";
        var now = DateTime.UtcNow;
        var oldTimestamp = now.AddHours(-2);
        var recentTimestamp = now.AddMinutes(-30);

        _repository.StoreMetric(metricName, 1.0, oldTimestamp);
        _repository.StoreMetric(metricName, 2.0, recentTimestamp);

        // Act
        var result = _repository.GetHistory(metricName, TimeSpan.FromHours(1)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(recentTimestamp, result[0].Timestamp);
    }

    #endregion

    #region GetRecentMetrics Tests

    [Fact]
    public void GetRecentMetrics_Should_Throw_When_MetricName_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _repository.GetRecentMetrics(null!, 5));
        Assert.Contains("Metric name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void GetRecentMetrics_Should_Throw_When_MetricName_Is_Empty()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _repository.GetRecentMetrics("", 5));
        Assert.Contains("Metric name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void GetRecentMetrics_Should_Return_Empty_List_When_Count_Is_Zero_Or_Negative()
    {
        // Arrange
        var metricName = "test_metric";
        _repository.StoreMetric(metricName, 1.0, DateTime.UtcNow);

        // Act
        var resultZero = _repository.GetRecentMetrics(metricName, 0);
        var resultNegative = _repository.GetRecentMetrics(metricName, -1);

        // Assert
        Assert.Empty(resultZero);
        Assert.Empty(resultNegative);
    }

    [Fact]
    public void GetRecentMetrics_Should_Return_Empty_For_NonExistent_Metric()
    {
        // Act
        var result = _repository.GetRecentMetrics("nonexistent", 5);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetRecentMetrics_Should_Return_Recent_Metrics_Ordered_Chronologically()
    {
        // Arrange
        var metricName = "test_metric";
        var timestamp1 = DateTime.UtcNow.AddMinutes(-3);
        var timestamp2 = DateTime.UtcNow.AddMinutes(-2);
        var timestamp3 = DateTime.UtcNow.AddMinutes(-1);

        _repository.StoreMetric(metricName, 1.0, timestamp1);
        _repository.StoreMetric(metricName, 2.0, timestamp2);
        _repository.StoreMetric(metricName, 3.0, timestamp3);

        // Act
        var result = _repository.GetRecentMetrics(metricName, 2);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(timestamp2, result[0].Timestamp); // Most recent first, then re-ordered
        Assert.Equal(timestamp3, result[1].Timestamp);
    }

    [Fact]
    public void GetRecentMetrics_Should_Return_All_If_Count_Exceeds_Available()
    {
        // Arrange
        var metricName = "test_metric";
        _repository.StoreMetric(metricName, 1.0, DateTime.UtcNow.AddMinutes(-1));
        _repository.StoreMetric(metricName, 2.0, DateTime.UtcNow);

        // Act
        var result = _repository.GetRecentMetrics(metricName, 10);

        // Assert
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region CleanupOldData Tests

    [Fact]
    public void CleanupOldData_Should_Remove_Old_Data()
    {
        // Arrange
        var metricName = "test_metric";
        var now = DateTime.UtcNow;
        var oldTimestamp = now.AddHours(-3);
        var recentTimestamp = now.AddMinutes(-30);

        _repository.StoreMetric(metricName, 1.0, oldTimestamp);
        _repository.StoreMetric(metricName, 2.0, recentTimestamp);

        // Act
        _repository.CleanupOldData(TimeSpan.FromHours(1));

        // Assert
        var history = _repository.GetHistory(metricName).ToList();
        Assert.Single(history);
        Assert.Equal(recentTimestamp, history[0].Timestamp);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("1") && o.ToString().Contains("old time-series data points")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void CleanupOldData_Should_Log_When_No_Data_Removed()
    {
        // Arrange
        var metricName = "test_metric";
        _repository.StoreMetric(metricName, 1.0, DateTime.UtcNow.AddMinutes(-30));

        // Act
        _repository.CleanupOldData(TimeSpan.FromHours(1));

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("old time-series data points")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_Should_Remove_All_Data()
    {
        // Arrange
        _repository.StoreMetric("metric1", 1.0, DateTime.UtcNow);
        _repository.StoreMetric("metric2", 2.0, DateTime.UtcNow);

        // Act
        _repository.Clear();

        // Assert
        Assert.Empty(_repository.GetHistory("metric1"));
        Assert.Empty(_repository.GetHistory("metric2"));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Cleared all time-series data")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion
}