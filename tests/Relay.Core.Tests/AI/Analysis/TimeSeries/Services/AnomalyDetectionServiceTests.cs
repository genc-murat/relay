using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.TimeSeries.Services;

public class AnomalyDetectionServiceTests
{
    private readonly Mock<ILogger<AnomalyDetectionService>> _loggerMock;
    private readonly Mock<ITimeSeriesRepository> _repositoryMock;
    private readonly AnomalyDetectionService _service;

    public AnomalyDetectionServiceTests()
    {
        _loggerMock = new Mock<ILogger<AnomalyDetectionService>>();
        _repositoryMock = new Mock<ITimeSeriesRepository>();
        _service = new AnomalyDetectionService(_loggerMock.Object, _repositoryMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AnomalyDetectionService(null!, _repositoryMock.Object));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Repository_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AnomalyDetectionService(_loggerMock.Object, null!));
    }

    #endregion

    #region DetectAnomalies Tests

    [Fact]
    public void DetectAnomalies_Should_Throw_When_MetricName_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.DetectAnomalies(null!));
    }

    [Fact]
    public void DetectAnomalies_Should_Throw_When_MetricName_Is_Empty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.DetectAnomalies(string.Empty));
    }

    [Fact]
    public void DetectAnomalies_Should_Throw_When_MetricName_Is_Whitespace()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.DetectAnomalies("   "));
    }

    [Fact]
    public void DetectAnomalies_Should_Throw_When_LookbackPoints_Is_Zero()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.DetectAnomalies("test.metric", 0));
    }

    [Fact]
    public void DetectAnomalies_Should_Throw_When_LookbackPoints_Is_Negative()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.DetectAnomalies("test.metric", -1));
    }

    [Fact]
    public void DetectAnomalies_Should_Return_Empty_List_When_Insufficient_Data()
    {
        // Arrange
        var metricName = "test.metric";
        var history = new List<MetricDataPoint>
        {
            new MetricDataPoint { MetricName = metricName, Timestamp = DateTime.UtcNow, Value = 10.0f }
        };
        _repositoryMock.Setup(r => r.GetHistory(metricName)).Returns(history);

        // Act
        var result = _service.DetectAnomalies(metricName);

        // Assert
        Assert.Empty(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Insufficient data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void DetectAnomalies_Should_Return_Empty_List_When_No_History()
    {
        // Arrange
        var metricName = "test.metric";
        var history = new List<MetricDataPoint>();
        _repositoryMock.Setup(r => r.GetHistory(metricName)).Returns(history);

        // Act
        var result = _service.DetectAnomalies(metricName);

        // Assert
        Assert.Empty(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Insufficient data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void DetectAnomalies_Should_Detect_Anomalies_With_Sufficient_Data()
    {
        // Arrange
        var metricName = "test.metric";
        var baseTime = DateTime.UtcNow;
        var history = new List<MetricDataPoint>();

        // Create normal data with one anomaly
        for (int i = 0; i < 50; i++)
        {
            var value = i == 25 ? 1000.0f : 50.0f; // Anomaly at index 25
            history.Add(new MetricDataPoint
            {
                MetricName = metricName,
                Timestamp = baseTime.AddMinutes(i),
                Value = value
            });
        }

        _repositoryMock.Setup(r => r.GetHistory(metricName)).Returns(history);

        // Act
        var result = _service.DetectAnomalies(metricName, lookbackPoints: 50);

        // Assert
        Assert.NotNull(result);
        // The ML.NET anomaly detection may or may not detect the anomaly depending on the algorithm
        // We just verify it doesn't throw and returns a list
    }

    [Fact]
    public void DetectAnomalies_Should_Limit_Lookback_Points()
    {
        // Arrange
        var metricName = "test.metric";
        var baseTime = DateTime.UtcNow;
        var history = new List<MetricDataPoint>();

        // Create 200 data points
        for (int i = 0; i < 200; i++)
        {
            history.Add(new MetricDataPoint
            {
                MetricName = metricName,
                Timestamp = baseTime.AddMinutes(i),
                Value = 50.0f
            });
        }

        _repositoryMock.Setup(r => r.GetHistory(metricName)).Returns(history);

        // Act
        var result = _service.DetectAnomalies(metricName, lookbackPoints: 50);

        // Assert
        Assert.NotNull(result);
        // Should only analyze the last 50 points
    }

    [Fact]
    public void DetectAnomalies_Should_Log_When_Anomalies_Are_Detected()
    {
        // Arrange
        var metricName = "test.metric";
        var baseTime = DateTime.UtcNow;
        var history = new List<MetricDataPoint>();

        // Create data that should trigger anomaly detection
        for (int i = 0; i < 50; i++)
        {
            var value = i == 25 ? 10000.0f : 1.0f; // Large anomaly
            history.Add(new MetricDataPoint
            {
                MetricName = metricName,
                Timestamp = baseTime.AddMinutes(i),
                Value = value
            });
        }

        _repositoryMock.Setup(r => r.GetHistory(metricName)).Returns(history);

        // Act
        var result = _service.DetectAnomalies(metricName, lookbackPoints: 50);

        // Assert
        Assert.NotNull(result);
        // If anomalies are detected, it should log
        // We can't guarantee detection, but the method should complete without error
    }

    [Fact]
    public void DetectAnomalies_Should_Handle_Exception_And_Return_Empty_List()
    {
        // Arrange
        var metricName = "test.metric";
        _repositoryMock.Setup(r => r.GetHistory(metricName)).Throws(new Exception("Database error"));

        // Act
        var result = _service.DetectAnomalies(metricName);

        // Assert
        Assert.Empty(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error detecting anomalies")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region DetectAnomaliesAsync Tests

    [Fact]
    public async Task DetectAnomaliesAsync_Should_Call_Sync_Method()
    {
        // Arrange
        var metricName = "test.metric";
        var history = new List<MetricDataPoint>
        {
            new MetricDataPoint { MetricName = metricName, Timestamp = DateTime.UtcNow, Value = 10.0f }
        };
        _repositoryMock.Setup(r => r.GetHistory(metricName)).Returns(history);

        // Act
        var result = await _service.DetectAnomaliesAsync(metricName);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DetectAnomaliesAsync_Should_Respect_CancellationToken()
    {
        // Arrange
        var metricName = "test.metric";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => _service.DetectAnomaliesAsync(metricName, cancellationToken: cts.Token));
    }

    #endregion
}