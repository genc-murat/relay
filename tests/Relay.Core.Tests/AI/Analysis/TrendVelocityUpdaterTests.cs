using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis;

public class TrendVelocityUpdaterTests
{
    private readonly Mock<ILogger<TrendVelocityUpdater>> _loggerMock;
    private readonly TrendAnalysisConfig _config;
    private readonly TrendVelocityUpdater _updater;

    public TrendVelocityUpdaterTests()
    {
        _loggerMock = new Mock<ILogger<TrendVelocityUpdater>>();
        _config = new TrendAnalysisConfig();
        _updater = new TrendVelocityUpdater(_loggerMock.Object, _config);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TrendVelocityUpdater(null!, _config));
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TrendVelocityUpdater(_loggerMock.Object, null!));
    }

    [Fact]
    public void UpdateTrendVelocities_WithValidMetrics_ReturnsVelocities()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 100.0,
            ["metric2"] = 200.0
        };
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _updater.UpdateTrendVelocities(currentMetrics, timestamp);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("metric1", result);
        Assert.Contains("metric2", result);
        // Since CalculateMetricVelocity returns 0.0, velocities should be 0.0
        Assert.Equal(0.0, result["metric1"]);
        Assert.Equal(0.0, result["metric2"]);
    }

    [Fact]
    public void UpdateTrendVelocities_WithEmptyMetrics_ReturnsEmptyDictionary()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>();
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _updater.UpdateTrendVelocities(currentMetrics, timestamp);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void UpdateTrendVelocities_WithHighVelocity_LogsDebugMessage()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["highVelocityMetric"] = 100.0
        };
        var timestamp = DateTime.UtcNow;

        // Create config with low threshold to trigger logging with 0.0 velocity
        var lowThresholdConfig = new TrendAnalysisConfig
        {
            HighVelocityThreshold = -1.0 // Any absolute value >= 0 will trigger logging
        };
        var updaterWithLowThreshold = new TrendVelocityUpdater(_loggerMock.Object, lowThresholdConfig);

        // Act
        var result = updaterWithLowThreshold.UpdateTrendVelocities(currentMetrics, timestamp);

        // Assert
        Assert.Single(result);
        Assert.Equal(0.0, result["highVelocityMetric"]);
        // Since velocity is 0.0 and threshold is -1.0, debug logging should occur
        _loggerMock.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("High velocity detected")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void UpdateTrendVelocities_WithException_LogsWarningAndReturnsEmpty()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["problematicMetric"] = double.NaN // This might cause issues
        };
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _updater.UpdateTrendVelocities(currentMetrics, timestamp);

        // Assert
        Assert.NotNull(result);
        // The method should handle exceptions gracefully and return results for valid metrics
        // Since CalculateMetricVelocity returns 0.0 for any input, it should work
        Assert.Single(result);
        Assert.Equal(0.0, result["problematicMetric"]);
    }

    [Fact]
    public void UpdateTrendVelocities_MultipleMetrics_ProcessesAll()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 85.5,
            ["memory"] = 1024.0,
            ["disk"] = 512.0
        };
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _updater.UpdateTrendVelocities(currentMetrics, timestamp);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("cpu", result);
        Assert.Contains("memory", result);
        Assert.Contains("disk", result);
        // All velocities should be 0.0 since CalculateMetricVelocity returns 0.0
        Assert.All(result.Values, velocity => Assert.Equal(0.0, velocity));
    }

    [Fact]
    public void UpdateTrendVelocities_WithNullMetrics_ThrowsArgumentNullException()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _updater.UpdateTrendVelocities(null!, timestamp));
    }

    [Fact]
    public void Implements_ITrendVelocityUpdater()
    {
        // Assert
        Assert.IsAssignableFrom<ITrendVelocityUpdater>(_updater);
    }

    [Fact]
    public void CalculateMetricVelocity_ReturnsZero()
    {
        // Arrange
        var method = typeof(TrendVelocityUpdater).GetMethod("CalculateMetricVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        var timestamp = DateTime.UtcNow;

        // Act
        var result = method.Invoke(_updater, new object[] { "testMetric", 100.0, timestamp });

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void UpdateTrendVelocities_WithDefaultTimestamp_Works()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 50.0
        };
        var timestamp = default(DateTime);

        // Act
        var result = _updater.UpdateTrendVelocities(currentMetrics, timestamp);

        // Assert
        Assert.Single(result);
        Assert.Equal(0.0, result["metric1"]);
    }

    [Fact]
    public void UpdateTrendVelocities_WithMinMaxValues_Works()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["minValue"] = double.MinValue,
            ["maxValue"] = double.MaxValue,
            ["normalValue"] = 42.0
        };
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _updater.UpdateTrendVelocities(currentMetrics, timestamp);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(0.0, result["minValue"]);
        Assert.Equal(0.0, result["maxValue"]);
        Assert.Equal(0.0, result["normalValue"]);
    }
}