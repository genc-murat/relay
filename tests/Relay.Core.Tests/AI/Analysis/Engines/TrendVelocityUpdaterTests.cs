using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

public class TrendVelocityUpdaterTests
{
    private readonly Mock<ILogger<TrendVelocityUpdater>> _loggerMock;
    private readonly TrendAnalysisConfig _config;

    public TrendVelocityUpdaterTests()
    {
        _loggerMock = new Mock<ILogger<TrendVelocityUpdater>>();
        _config = new TrendAnalysisConfig
        {
            HighVelocityThreshold = 0.1
        };
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TrendVelocityUpdater(null!, _config));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Config_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TrendVelocityUpdater(_loggerMock.Object, null!));
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Throw_When_Metrics_Is_Null()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => updater.UpdateTrendVelocities(null!, DateTime.UtcNow));
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Return_Empty_For_Empty_Metrics()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var metrics = new Dictionary<string, double>();
        var timestamp = DateTime.UtcNow;

        // Act
        var result = updater.UpdateTrendVelocities(metrics, timestamp);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Return_Zero_For_First_Observation()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var metrics = new Dictionary<string, double> { ["cpu"] = 50.0 };
        var timestamp = DateTime.UtcNow;

        // Act
        var result = updater.UpdateTrendVelocities(metrics, timestamp);

        // Assert
        Assert.Single(result);
        Assert.Equal(0.0, result["cpu"]);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Calculate_Positive_Velocity()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(60); // 60 seconds later

        // Act - First observation
        updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 50.0 },
            timestamp1);

        // Second observation - increased by 10 units in 60 seconds = 10/min
        var result = updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 60.0 },
            timestamp2);

        // Assert
        Assert.Single(result);
        Assert.Equal(10.0, result["cpu"], 2);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Calculate_Negative_Velocity()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(60); // 60 seconds later

        // Act - First observation
        updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 100.0 },
            timestamp1);

        // Second observation - decreased by 30 units in 60 seconds = -30/min
        var result = updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 70.0 },
            timestamp2);

        // Assert
        Assert.Single(result);
        Assert.Equal(-30.0, result["cpu"], 2);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Calculate_Velocity_For_Different_Time_Intervals()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(30); // 30 seconds = 0.5 minutes

        // Act - First observation
        updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 50.0 },
            timestamp1);

        // Second observation - increased by 5 units in 30 seconds = 10/min
        var result = updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 55.0 },
            timestamp2);

        // Assert
        Assert.Single(result);
        Assert.Equal(10.0, result["cpu"], 2);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Return_Zero_For_Unchanged_Value()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(60);

        // Act - First observation
        updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 50.0 },
            timestamp1);

        // Second observation - same value
        var result = updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 50.0 },
            timestamp2);

        // Assert
        Assert.Single(result);
        Assert.Equal(0.0, result["cpu"]);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Handle_Multiple_Metrics()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(60);

        // Act - First observation
        updater.UpdateTrendVelocities(
            new Dictionary<string, double>
            {
                ["cpu"] = 50.0,
                ["memory"] = 100.0,
                ["disk"] = 200.0
            },
            timestamp1);

        // Second observation
        var result = updater.UpdateTrendVelocities(
            new Dictionary<string, double>
            {
                ["cpu"] = 60.0,     // +10 per minute
                ["memory"] = 95.0,  // -5 per minute
                ["disk"] = 200.0    // 0 per minute
            },
            timestamp2);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(10.0, result["cpu"], 2);
        Assert.Equal(-5.0, result["memory"], 2);
        Assert.Equal(0.0, result["disk"], 2);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Track_Historical_Data()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Act - Add multiple observations
        for (int i = 0; i < 5; i++)
        {
            updater.UpdateTrendVelocities(
                new Dictionary<string, double> { ["cpu"] = 50.0 + (i * 10) },
                timestamp.AddSeconds(i * 60));
        }

        // Assert
        var historySize = updater.GetHistorySize("cpu");
        Assert.Equal(5, historySize);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Maintain_History_Limit()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Act - Add more than 60 observations
        for (int i = 0; i < 80; i++)
        {
            updater.UpdateTrendVelocities(
                new Dictionary<string, double> { ["cpu"] = 50.0 + (i * 0.1) },
                timestamp.AddSeconds(i * 60));
        }

        // Assert
        var historySize = updater.GetHistorySize("cpu");
        Assert.Equal(60, historySize);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Return_Zero_For_Rapid_Updates()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddMilliseconds(500); // Less than 1 second

        // Act - First observation
        updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 50.0 },
            timestamp1);

        // Second observation - too soon
        var result = updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 60.0 },
            timestamp2);

        // Assert
        Assert.Equal(0.0, result["cpu"]);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Use_Weighted_Velocity_With_Sufficient_History()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Add a series of increasing values to establish a trend
        var values = new[] { 10.0, 15.0, 20.0, 25.0, 30.0 };
        for (int i = 0; i < values.Length; i++)
        {
            updater.UpdateTrendVelocities(
                new Dictionary<string, double> { ["cpu"] = values[i] },
                timestamp.AddSeconds(i * 60));
        }

        // Act - Add a new value
        var result = updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 35.0 },
            timestamp.AddSeconds(values.Length * 60));

        // Assert - Should have a velocity around 5/min (consistent trend)
        Assert.True(Math.Abs(result["cpu"] - 5.0) < 1.0);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Detect_High_Velocity()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TrendVelocityUpdater>>();
        var updater = new TrendVelocityUpdater(loggerMock.Object, _config);
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(60);

        // Act - First observation
        updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 50.0 },
            timestamp1);

        // Second observation - large change (7 per minute > 0.1 threshold)
        var result = updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 57.0 },
            timestamp2);

        // Assert - Should calculate the expected velocity
        Assert.Equal(7.0, result["cpu"], 2);

        // Verify that the debug log was called when high velocity was detected
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("High velocity detected")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Not_Log_High_Velocity_When_Below_Threshold()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TrendVelocityUpdater>>();
        var updater = new TrendVelocityUpdater(loggerMock.Object, _config);
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(60);

        // Act - First observation
        updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 50.0 },
            timestamp1);

        // Second observation - small change (0.05 per minute < 0.1 threshold)
        var result = updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 50.05 },
            timestamp2);

        // Assert - Should calculate the expected velocity
        Assert.Equal(0.05, result["cpu"], 2);

        // Verify that the debug log was NOT called since velocity is below threshold
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("High velocity detected")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
    }

    [Fact]
    public void ClearHistory_Should_Remove_All_Historical_Data()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Add some data
        for (int i = 0; i < 5; i++)
        {
            updater.UpdateTrendVelocities(
                new Dictionary<string, double> { ["cpu"] = 50.0 + (i * 10) },
                timestamp.AddSeconds(i * 60));
        }

        var historySizeBefore = updater.GetHistorySize("cpu");

        // Act
        updater.ClearHistory();

        // Assert
        Assert.True(historySizeBefore > 0);
        Assert.Equal(0, updater.GetHistorySize("cpu"));
        Assert.Null(updater.GetPreviousValue("cpu"));
    }

    [Fact]
    public void GetHistorySize_Should_Return_Zero_For_Unknown_Metric()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);

        // Act
        var size = updater.GetHistorySize("unknown");

        // Assert
        Assert.Equal(0, size);
    }

    [Fact]
    public void GetPreviousValue_Should_Return_Null_For_Unknown_Metric()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);

        // Act
        var previous = updater.GetPreviousValue("unknown");

        // Assert
        Assert.Null(previous);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Handle_Fractional_Velocity()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(120); // 2 minutes

        // Act - First observation
        updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 50.0 },
            timestamp1);

        // Second observation - 2 units change in 2 minutes = 1.0/min
        var result = updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 52.0 },
            timestamp2);

        // Assert
        Assert.Equal(1.0, result["cpu"], 2);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Handle_Very_Small_Changes()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(60);

        // Act - First observation
        updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 50.0 },
            timestamp1);

        // Second observation - tiny change
        var result = updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 50.001 },
            timestamp2);

        // Assert - Should handle small changes
        Assert.True(result["cpu"] >= 0.0 && result["cpu"] < 0.01);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Handle_Extreme_Changes()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(60);

        // Act - First observation
        updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 1.0 },
            timestamp1);

        // Second observation - extreme change
        var result = updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 1000.0 },
            timestamp2);

        // Assert
        Assert.Equal(999.0, result["cpu"], 2);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Handle_Negative_Values()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(60);

        // Act - First observation
        updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["metric"] = -50.0 },
            timestamp1);

        // Second observation - more negative
        var result = updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["metric"] = -60.0 },
            timestamp2);

        // Assert
        Assert.Equal(-10.0, result["metric"], 2);
    }

    [Fact]
    public void UpdateTrendVelocities_Should_Independent_Metrics()
    {
        // Arrange
        var updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(60);

        // Act - Two different metrics with separate trajectories
        updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 50.0, ["memory"] = 100.0 },
            timestamp1);

        var result = updater.UpdateTrendVelocities(
            new Dictionary<string, double> { ["cpu"] = 55.0, ["memory"] = 95.0 },
            timestamp2);

        // Assert - Each metric should have independent velocity
        Assert.Equal(5.0, result["cpu"], 2);
        Assert.Equal(-5.0, result["memory"], 2);
    }
}
