using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

public class AnomalyUpdaterTests
{
    private readonly Mock<ILogger<AnomalyUpdater>> _loggerMock;
    private readonly TrendAnalysisConfig _config;

    public AnomalyUpdaterTests()
    {
        _loggerMock = new Mock<ILogger<AnomalyUpdater>>();
        _config = new TrendAnalysisConfig
        {
            AnomalyZScoreThreshold = 2.0,
            HighAnomalyZScoreThreshold = 3.0
        };
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AnomalyUpdater(null!, _config));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Config_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AnomalyUpdater(_loggerMock.Object, null!));
    }

    [Fact]
    public void UpdateAnomalies_Should_Return_Empty_List_When_No_Anomalies()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0
        };
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData
            {
                MA15 = 75.0, // Same value, no anomaly
                Timestamp = DateTime.UtcNow
            }
        };

        // Act
        var anomalies = updater.UpdateAnomalies(currentMetrics, movingAverages);

        // Assert
        Assert.Empty(anomalies);
    }

    [Fact]
    public void UpdateAnomalies_Should_Detect_Medium_Severity_Anomaly()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 97.5 // 22.5 units above mean, Z-score = 22.5 / (75 * 0.1) = 22.5 / 7.5 = 3.0
        };
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData
            {
                MA15 = 75.0,
                Timestamp = DateTime.UtcNow
            }
        };

        // Act
        var anomalies = updater.UpdateAnomalies(currentMetrics, movingAverages);

        // Assert
        Assert.Single(anomalies);
        var anomaly = anomalies[0];
        Assert.Equal("cpu", anomaly.MetricName);
        Assert.Equal(97.5, anomaly.CurrentValue);
        Assert.Equal(75.0, anomaly.ExpectedValue);
        Assert.Equal(22.5, anomaly.Deviation);
        Assert.Equal(3.0, anomaly.ZScore);
        Assert.Equal(AnomalySeverity.Medium, anomaly.Severity);
        Assert.NotEqual(default(DateTime), anomaly.Timestamp);
    }

    [Fact]
    public void UpdateAnomalies_Should_Detect_High_Severity_Anomaly()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 112.5 // 37.5 units above mean, Z-score = 37.5 / (75 * 0.1) = 37.5 / 7.5 = 5.0
        };
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData
            {
                MA15 = 75.0,
                Timestamp = DateTime.UtcNow
            }
        };

        // Act
        var anomalies = updater.UpdateAnomalies(currentMetrics, movingAverages);

        // Assert
        Assert.Single(anomalies);
        var anomaly = anomalies[0];
        Assert.Equal("cpu", anomaly.MetricName);
        Assert.Equal(112.5, anomaly.CurrentValue);
        Assert.Equal(75.0, anomaly.ExpectedValue);
        Assert.Equal(37.5, anomaly.Deviation);
        Assert.Equal(5.0, anomaly.ZScore);
        Assert.Equal(AnomalySeverity.High, anomaly.Severity);
    }

    [Fact]
    public void UpdateAnomalies_Should_Handle_Multiple_Metrics_With_Anomalies()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 97.5,    // Will trigger anomaly (Z-score = 3.0)
            ["memory"] = 90.0, // Normal
            ["latency"] = 200.0 // Will trigger anomaly (Z-score = 10.0)
        };
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow },
            ["memory"] = new MovingAverageData { MA15 = 90.0, Timestamp = DateTime.UtcNow },
            ["latency"] = new MovingAverageData { MA15 = 100.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = updater.UpdateAnomalies(currentMetrics, movingAverages);

        // Assert
        Assert.Equal(2, anomalies.Count);
        Assert.Contains(anomalies, a => a.MetricName == "cpu");
        Assert.Contains(anomalies, a => a.MetricName == "latency");
        Assert.DoesNotContain(anomalies, a => a.MetricName == "memory");
    }

    [Fact]
    public void UpdateAnomalies_Should_Skip_Metrics_Without_Moving_Average_Data()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 97.5,
            ["memory"] = 85.0
        };
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow }
            // memory not in moving averages
        };

        // Act
        var anomalies = updater.UpdateAnomalies(currentMetrics, movingAverages);

        // Assert
        Assert.Single(anomalies);
        Assert.Equal("cpu", anomalies[0].MetricName);
    }

    [Fact]
    public void UpdateAnomalies_Should_Handle_Invalid_Data_Gracefully()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = double.NaN // Invalid data that doesn't trigger anomalies
        };
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = updater.UpdateAnomalies(currentMetrics, movingAverages);

        // Assert
        Assert.Empty(anomalies);
    }

    [Fact]
    public void UpdateAnomalies_Should_Log_Warning_For_Detected_Anomalies()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 97.5
        };
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        updater.UpdateAnomalies(currentMetrics, movingAverages);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Anomaly detected in cpu")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CalculateZScore_Should_Return_Zero_When_StdDev_Is_Zero()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);

        // Act - Use reflection to test private method
        var method = typeof(AnomalyUpdater).GetMethod("CalculateZScore",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (double)method.Invoke(updater, new object[] { 100.0, 0.0 });

        // Assert
        Assert.Equal(0.0, result);
    }
}