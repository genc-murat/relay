using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.TimeSeries.Services;

public class TimeSeriesStatisticsServiceTests
{
    private readonly Mock<ILogger<TimeSeriesStatisticsService>> _loggerMock;
    private readonly Mock<ITimeSeriesRepository> _repositoryMock;
    private readonly TimeSeriesStatisticsService _service;

    public TimeSeriesStatisticsServiceTests()
    {
        _loggerMock = new Mock<ILogger<TimeSeriesStatisticsService>>();
        _repositoryMock = new Mock<ITimeSeriesRepository>();
        _service = new TimeSeriesStatisticsService(_loggerMock.Object, _repositoryMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimeSeriesStatisticsService(null!, _repositoryMock.Object));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Repository_Is_Null()
    {
        // Arrange
        var logger = new Mock<ILogger<TimeSeriesStatisticsService>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimeSeriesStatisticsService(logger, null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Valid_Parameters()
    {
        // Arrange
        var logger = new Mock<ILogger<TimeSeriesStatisticsService>>().Object;
        var repository = new Mock<ITimeSeriesRepository>().Object;

        // Act
        var service = new TimeSeriesStatisticsService(logger, repository);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region GetStatistics Tests

    [Fact]
    public void GetStatistics_Should_Throw_When_MetricName_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.GetStatistics(null!));
        Assert.Contains("Metric name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void GetStatistics_Should_Throw_When_MetricName_Is_Empty()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.GetStatistics(""));
        Assert.Contains("Metric name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void GetStatistics_Should_Throw_When_MetricName_Is_Whitespace()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.GetStatistics("   "));
        Assert.Contains("Metric name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void GetStatistics_Should_Return_Null_When_No_Data_Available()
    {
        // Arrange
        var metricName = "empty_metric";
        _repositoryMock.Setup(r => r.GetHistory(metricName, null)).Returns(Enumerable.Empty<MetricDataPoint>());

        // Act
        var result = _service.GetStatistics(metricName);

        // Assert
        Assert.Null(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("No data available for statistics calculation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetStatistics_Should_Return_Null_When_No_Data_Available_With_Period()
    {
        // Arrange
        var metricName = "empty_metric";
        var period = TimeSpan.FromHours(1);
        _repositoryMock.Setup(r => r.GetHistory(metricName, period)).Returns(Enumerable.Empty<MetricDataPoint>());

        // Act
        var result = _service.GetStatistics(metricName, period);

        // Assert
        Assert.Null(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("No data available for statistics calculation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetStatistics_Should_Calculate_Statistics_Correctly()
    {
        // Arrange
        var metricName = "test_metric";
        var baseTime = DateTime.UtcNow;
        var dataPoints = new List<MetricDataPoint>
        {
            new MetricDataPoint { MetricName = metricName, Timestamp = baseTime, Value = 10.0f },
            new MetricDataPoint { MetricName = metricName, Timestamp = baseTime.AddMinutes(1), Value = 20.0f },
            new MetricDataPoint { MetricName = metricName, Timestamp = baseTime.AddMinutes(2), Value = 30.0f }
        };
        _repositoryMock.Setup(r => r.GetHistory(metricName, null)).Returns(dataPoints);

        // Act
        var result = _service.GetStatistics(metricName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(metricName, result.MetricName);
        Assert.Equal(3, result.Count);
        Assert.Equal(20.0, result.Mean, 1); // (10+20+30)/3 = 20
        Assert.Equal(10.0, result.Min);
        Assert.Equal(30.0, result.Max);
        Assert.True(result.StdDev > 0); // Should have some variance
        Assert.Equal(20.0, result.Median); // Middle value

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Calculated statistics")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetStatistics_Should_Use_Period_Parameter()
    {
        // Arrange
        var metricName = "test_metric";
        var period = TimeSpan.FromHours(1);
        var dataPoints = new List<MetricDataPoint>
        {
            new MetricDataPoint { MetricName = metricName, Timestamp = DateTime.UtcNow, Value = 15.0f }
        };
        _repositoryMock.Setup(r => r.GetHistory(metricName, period)).Returns(dataPoints);

        // Act
        var result = _service.GetStatistics(metricName, period);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(metricName, result.MetricName);
        Assert.Equal(1, result.Count);
        Assert.Equal(15.0, result.Mean);
        Assert.Equal(15.0, result.Min);
        Assert.Equal(15.0, result.Max);
        Assert.Equal(15.0, result.Median);
    }

    [Fact]
    public void GetStatistics_Should_Calculate_Percentiles_Correctly()
    {
        // Arrange
        var metricName = "test_metric";
        var dataPoints = new List<MetricDataPoint>();
        for (int i = 1; i <= 100; i++)
        {
            dataPoints.Add(new MetricDataPoint
            {
                MetricName = metricName,
                Timestamp = DateTime.UtcNow.AddMinutes(i),
                Value = i * 1.0f // Values: 1, 2, 3, ..., 100
            });
        }
        _repositoryMock.Setup(r => r.GetHistory(metricName, null)).Returns(dataPoints);

        // Act
        var result = _service.GetStatistics(metricName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.Count);
        Assert.Equal(50.5, result.Mean, 1); // (1+100)/2 = 50.5
        Assert.Equal(1.0, result.Min);
        Assert.Equal(100.0, result.Max);
        Assert.Equal(50.5, result.Median); // 50th percentile (average of 50 and 51)
        Assert.True(result.P95 >= 95.0); // 95th percentile should be >= 95
        Assert.True(result.P99 >= 99.0); // 99th percentile should be >= 99
    }

    [Fact]
    public void GetStatistics_Should_Handle_Exception_And_Throw_TimeSeriesException()
    {
        // Arrange
        var metricName = "error_metric";
        _repositoryMock.Setup(r => r.GetHistory(metricName, null)).Throws(new Exception("Database error"));

        // Act & Assert
        var exception = Assert.Throws<TimeSeriesException>(() => _service.GetStatistics(metricName));
        Assert.Contains($"Failed to calculate statistics for {metricName}", exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.IsType<Exception>(exception.InnerException);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error calculating statistics")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}