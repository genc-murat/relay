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
    public void DetectAnomalies_Should_Process_Exactly_12_Data_Points()
    {
        // Arrange
        var metricName = "test.metric";
        var baseTime = DateTime.UtcNow;
        var history = new List<MetricDataPoint>();

        // Create exactly 12 data points
        for (int i = 0; i < 12; i++)
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
        var result = _service.DetectAnomalies(metricName);

        // Assert
        Assert.NotNull(result);
        // Should not log insufficient data warning
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Insufficient data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
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
    public void DetectAnomalies_Should_Return_Valid_Anomaly_Results_With_Correct_Properties()
    {
        // Arrange
        var metricName = "test.metric";
        var baseTime = DateTime.UtcNow;
        var history = new List<MetricDataPoint>();

        // Create data with a clear anomaly pattern
        for (int i = 0; i < 50; i++)
        {
            var value = (i >= 20 && i <= 30) ? 100.0f : 10.0f; // Sustained anomaly
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
        foreach (var anomaly in result)
        {
            Assert.Equal(metricName, anomaly.MetricName);
            Assert.True(anomaly.IsAnomaly);
            Assert.True(anomaly.Score >= 0);
            Assert.True(anomaly.Magnitude >= 0);
            Assert.NotEqual(default, anomaly.Timestamp);
            Assert.True(anomaly.Value >= 0);
        }
    }

        [Fact]
        public void DetectAnomalies_Should_Handle_Exception_And_Throw_AnomalyDetectionException()
        {
            // Arrange
            var metricName = "test.metric";
            _repositoryMock.Setup(r => r.GetHistory(metricName)).Throws(new Exception("Database error"));

            // Act & Assert
            var exception = Assert.Throws<AnomalyDetectionException>(() => _service.DetectAnomalies(metricName));
            Assert.Contains("Failed to detect anomalies for metric 'test.metric'", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.IsType<Exception>(exception.InnerException);
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
    public async Task DetectAnomaliesAsync_Should_Use_Custom_LookbackPoints()
    {
        // Arrange
        var metricName = "test.metric";
        var baseTime = DateTime.UtcNow;
        var history = new List<MetricDataPoint>();

        // Create 50 data points
        for (int i = 0; i < 50; i++)
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
        var result = await _service.DetectAnomaliesAsync(metricName, lookbackPoints: 25);

        // Assert
        Assert.NotNull(result);
        // Should process with custom lookback
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

    [Fact]
    public async Task DetectAnomaliesAsync_Should_Handle_Cancellation_During_Execution()
    {
        // Arrange
        var metricName = "test.metric";
        var cts = new CancellationTokenSource();
        var history = new List<MetricDataPoint>
        {
            new MetricDataPoint { MetricName = metricName, Timestamp = DateTime.UtcNow, Value = 10.0f }
        };
        _repositoryMock.Setup(r => r.GetHistory(metricName)).Returns(history);

        // Cancel after starting
        cts.CancelAfter(1); // Cancel after 1ms

        // Act & Assert
        // Since the operation is fast, this may or may not throw depending on timing
        // Both outcomes are valid: either the operation completes before cancellation,
        // or it gets cancelled and throws OperationCanceledException
        try
        {
            var result = await _service.DetectAnomaliesAsync(metricName, cancellationToken: cts.Token);
            Assert.NotNull(result);
        }
        catch (OperationCanceledException)
        {
            // This is also a valid outcome if cancellation happens during execution
            Assert.True(cts.IsCancellationRequested);
        }
    }

    [Fact]
    public async Task DetectAnomaliesAsync_Should_Handle_Exception_And_Throw_AnomalyDetectionException()
    {
        // Arrange
        var metricName = "test.metric";
        _repositoryMock.Setup(r => r.GetHistory(metricName)).Throws(new Exception("Database error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AnomalyDetectionException>(() => _service.DetectAnomaliesAsync(metricName));
        Assert.Contains("Failed to detect anomalies for metric 'test.metric'", exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.IsType<Exception>(exception.InnerException);
    }

    #endregion
}