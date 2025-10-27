using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

public class RegressionUpdaterTests
{
    private readonly Mock<ILogger<RegressionUpdater>> _loggerMock;

    public RegressionUpdaterTests()
    {
        _loggerMock = new Mock<ILogger<RegressionUpdater>>();
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RegressionUpdater(null!));
    }

    [Fact]
    public void UpdateRegressionResults_Should_Return_Empty_For_Empty_Metrics()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);
        var metrics = new Dictionary<string, double>();
        var timestamp = DateTime.UtcNow;

        // Act
        var result = updater.UpdateRegressionResults(metrics, timestamp);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void UpdateRegressionResults_Should_Return_Zero_Regression_On_First_Call()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 50.0
        };
        var timestamp = DateTime.UtcNow;

        // Act
        var result = updater.UpdateRegressionResults(metrics, timestamp);

        // Assert
        Assert.Single(result);
        var regression = result["cpu"];
        Assert.Equal(0.0, regression.Slope);
        Assert.Equal(0.0, regression.Intercept);
        Assert.Equal(0.0, regression.RSquared);
    }

    [Fact]
    public void UpdateRegressionResults_Should_Calculate_Perfect_Linear_Relationship()
    {
        // Arrange: Create a perfect linear relationship y = 2x + 10
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add points: (1,12), (2,14), (3,16), (4,18), (5,20)
        // This represents y = 2x + 10
        for (int i = 1; i <= 5; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["value"] = (2 * i) + 10.0
            };
            updater.UpdateRegressionResults(metrics, timestamp);
        }

        // Act
        var result = updater.UpdateRegressionResults(
            new Dictionary<string, double> { ["value"] = 22.0 }, // 6th point at (6,22)
            timestamp);

        // Assert
        var regression = result["value"];
        // For perfect linear data, slope should be 2.0 and intercept should be 10.0
        Assert.Equal(2.0, regression.Slope, 4);
        Assert.Equal(10.0, regression.Intercept, 4);
        // R² should be very close to 1.0 for perfect linear data
        Assert.True(regression.RSquared >= 0.99);
    }

    [Fact]
    public void UpdateRegressionResults_Should_Detect_Increasing_Trend()
    {
        // Arrange: Create increasing trend
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add increasing values
        double[] values = { 10, 12, 14, 16, 18, 20, 22, 24 };
        foreach (var val in values)
        {
            updater.UpdateRegressionResults(
                new Dictionary<string, double> { ["metric"] = val },
                timestamp);
        }

        // Act
        var result = updater.UpdateRegressionResults(
            new Dictionary<string, double> { ["metric"] = 26.0 },
            timestamp);

        // Assert
        var regression = result["metric"];
        // Slope should be positive for increasing trend
        Assert.True(regression.Slope > 0);
    }

    [Fact]
    public void UpdateRegressionResults_Should_Detect_Decreasing_Trend()
    {
        // Arrange: Create decreasing trend
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add decreasing values
        double[] values = { 50, 48, 46, 44, 42, 40, 38, 36 };
        foreach (var val in values)
        {
            updater.UpdateRegressionResults(
                new Dictionary<string, double> { ["metric"] = val },
                timestamp);
        }

        // Act
        var result = updater.UpdateRegressionResults(
            new Dictionary<string, double> { ["metric"] = 34.0 },
            timestamp);

        // Assert
        var regression = result["metric"];
        // Slope should be negative for decreasing trend
        Assert.True(regression.Slope < 0);
    }

    [Fact]
    public void UpdateRegressionResults_Should_Detect_Stable_Trend()
    {
        // Arrange: Create stable (flat) trend
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add constant values
        double[] values = { 50.0, 50.0, 50.0, 50.0, 50.0, 50.0 };
        foreach (var val in values)
        {
            updater.UpdateRegressionResults(
                new Dictionary<string, double> { ["metric"] = val },
                timestamp);
        }

        // Act
        var result = updater.UpdateRegressionResults(
            new Dictionary<string, double> { ["metric"] = 50.0 },
            timestamp);

        // Assert
        var regression = result["metric"];
        // Slope should be very close to 0 for stable trend
        Assert.Equal(0.0, regression.Slope, 4);
    }

    [Fact]
    public void UpdateRegressionResults_Should_Handle_Multiple_Metrics()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add data for multiple metrics
        for (int i = 1; i <= 5; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["cpu"] = i * 10.0,      // 10, 20, 30, 40, 50
                ["memory"] = i * 5.0,    // 5, 10, 15, 20, 25
                ["disk"] = 100 - i * 5.0 // 95, 90, 85, 80, 75
            };
            updater.UpdateRegressionResults(metrics, timestamp);
        }

        // Act
        var result = updater.UpdateRegressionResults(
            new Dictionary<string, double>
            {
                ["cpu"] = 60.0,
                ["memory"] = 30.0,
                ["disk"] = 70.0
            },
            timestamp);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.True(result["cpu"].Slope > 0);
        Assert.True(result["memory"].Slope > 0);
        Assert.True(result["disk"].Slope < 0);
    }

    [Fact]
    public void UpdateRegressionResults_Should_Maintain_History_Within_Limit()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add more than 60 data points
        for (int i = 1; i <= 75; i++)
        {
            updater.UpdateRegressionResults(
                new Dictionary<string, double> { ["metric"] = i * 1.0 },
                timestamp);
        }

        // Act - verify history is limited to 60
        var historySize = updater.GetHistorySize("metric");

        // Assert
        Assert.Equal(60, historySize);
    }

    [Fact]
    public void GetTrendDirection_Should_Classify_Positive_Slope_As_Increasing()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);

        // Act
        var direction = updater.GetTrendDirection(0.5);

        // Assert
        Assert.Equal(RegressionUpdater.TrendDirection.Increasing, direction);
    }

    [Fact]
    public void GetTrendDirection_Should_Classify_Negative_Slope_As_Decreasing()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);

        // Act
        var direction = updater.GetTrendDirection(-0.5);

        // Assert
        Assert.Equal(RegressionUpdater.TrendDirection.Decreasing, direction);
    }

    [Fact]
    public void GetTrendDirection_Should_Classify_Near_Zero_Slope_As_Stable()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);

        // Act
        var direction = updater.GetTrendDirection(0.0001);

        // Assert
        Assert.Equal(RegressionUpdater.TrendDirection.Stable, direction);
    }

    [Fact]
    public void GetConfidenceLevel_Should_Classify_High_RSquared_As_VeryHigh()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);

        // Act
        var confidence = updater.GetConfidenceLevel(0.95);

        // Assert
        Assert.Equal(RegressionUpdater.ConfidenceLevel.VeryHigh, confidence);
    }

    [Fact]
    public void GetConfidenceLevel_Should_Classify_Medium_RSquared_As_High()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);

        // Act
        var confidence = updater.GetConfidenceLevel(0.75);

        // Assert
        Assert.Equal(RegressionUpdater.ConfidenceLevel.High, confidence);
    }

    [Fact]
    public void GetConfidenceLevel_Should_Classify_Low_RSquared_As_Medium()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);

        // Act
        var confidence = updater.GetConfidenceLevel(0.6);

        // Assert
        Assert.Equal(RegressionUpdater.ConfidenceLevel.Medium, confidence);
    }

    [Fact]
    public void GetConfidenceLevel_Should_Classify_Very_Low_RSquared_As_VeryLow()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);

        // Act
        var confidence = updater.GetConfidenceLevel(0.1);

        // Assert
        Assert.Equal(RegressionUpdater.ConfidenceLevel.VeryLow, confidence);
    }

    [Fact]
    public void UpdateRegressionResults_Should_Handle_NaN_Values_Gracefully()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Act - add NaN should be handled gracefully
        var result = updater.UpdateRegressionResults(
            new Dictionary<string, double> { ["metric"] = double.NaN },
            timestamp);

        // Assert - should still return valid result (not throw)
        Assert.Single(result);
        // The result should have a value, even if it's not useful
        var regression = result["metric"];
        Assert.NotNull(regression);
    }

    [Fact]
    public void UpdateRegressionResults_Should_Calculate_RSquared_Correctly()
    {
        // Arrange: Data with known R² value
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add data with perfect linear fit (R² = 1)
        // y = 3x + 5: points (1,8), (2,11), (3,14), (4,17), (5,20)
        var values = new[] { 8.0, 11.0, 14.0, 17.0, 20.0 };
        foreach (var val in values)
        {
            updater.UpdateRegressionResults(
                new Dictionary<string, double> { ["metric"] = val },
                timestamp);
        }

        // Act
        var result = updater.UpdateRegressionResults(
            new Dictionary<string, double> { ["metric"] = 23.0 },
            timestamp);

        // Assert
        var regression = result["metric"];
        // Perfect linear data should have R² very close to 1.0
        Assert.True(regression.RSquared > 0.99);
    }

    [Fact]
    public void UpdateRegressionResults_Should_Calculate_Noisy_Data_RSquared()
    {
        // Arrange: Data with some noise
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add noisy data: general trend y = 2x but with noise
        var values = new[] { 2.5, 4.8, 6.2, 8.1, 10.3, 12.0, 14.2 };
        foreach (var val in values)
        {
            updater.UpdateRegressionResults(
                new Dictionary<string, double> { ["metric"] = val },
                timestamp);
        }

        // Act
        var result = updater.UpdateRegressionResults(
            new Dictionary<string, double> { ["metric"] = 16.1 },
            timestamp);

        // Assert
        var regression = result["metric"];
        // Noisy data should have R² between 0 and 1
        Assert.True(regression.RSquared >= 0.0);
        Assert.True(regression.RSquared <= 1.0);
    }

    [Fact]
    public void ClearHistory_Should_Remove_All_Data()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add some data
        for (int i = 1; i <= 5; i++)
        {
            updater.UpdateRegressionResults(
                new Dictionary<string, double> { ["metric"] = i * 10.0 },
                timestamp);
        }

        var historySizeBefore = updater.GetHistorySize("metric");

        // Act
        updater.ClearHistory();

        // Assert
        Assert.True(historySizeBefore > 0);
        Assert.Equal(0, updater.GetHistorySize("metric"));
    }

    [Fact]
    public void UpdateRegressionResults_Should_Handle_Constant_Values_Gracefully()
    {
        // Arrange: All constant values
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add constant values (no variation in x)
        double[] values = { 50.0, 50.0, 50.0, 50.0, 50.0 };
        foreach (var val in values)
        {
            updater.UpdateRegressionResults(
                new Dictionary<string, double> { ["metric"] = val },
                timestamp);
        }

        // Act
        var result = updater.UpdateRegressionResults(
            new Dictionary<string, double> { ["metric"] = 50.0 },
            timestamp);

        // Assert
        var regression = result["metric"];
        Assert.Equal(0.0, regression.Slope, 4); // No slope for constant data
        Assert.Equal(50.0, regression.Intercept, 4); // Intercept is the average
    }

    [Fact]
    public void UpdateRegressionResults_Should_Maintain_Independent_Metric_Histories()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add data for two different metrics
        for (int i = 1; i <= 3; i++)
        {
            updater.UpdateRegressionResults(
                new Dictionary<string, double>
                {
                    ["metric1"] = i * 10.0,
                    ["metric2"] = i * 5.0
                },
                timestamp);
        }

        // Act - add more data only to metric1
        updater.UpdateRegressionResults(
            new Dictionary<string, double> { ["metric1"] = 40.0 },
            timestamp);

        // Assert
        var size1 = updater.GetHistorySize("metric1");
        var size2 = updater.GetHistorySize("metric2");
        Assert.Equal(4, size1);
        Assert.Equal(3, size2);
    }

    [Fact]
    public void UpdateRegressionResults_Should_Calculate_Slope_With_Positive_And_Negative_Values()
    {
        // Arrange: Data that crosses zero
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add data: -10, -5, 0, 5, 10 (represents y = x with offset)
        var values = new[] { -10.0, -5.0, 0.0, 5.0, 10.0 };
        foreach (var val in values)
        {
            updater.UpdateRegressionResults(
                new Dictionary<string, double> { ["metric"] = val },
                timestamp);
        }

        // Act
        var result = updater.UpdateRegressionResults(
            new Dictionary<string, double> { ["metric"] = 15.0 },
            timestamp);

        // Assert
        var regression = result["metric"];
        // Should have positive slope
        Assert.True(regression.Slope > 0);
        // For perfect linear data through zero, R² should be very high
        Assert.True(regression.RSquared > 0.99);
    }

    [Fact]
    public void UpdateRegressionResults_Should_Preserve_Order_Of_Data_Points()
    {
        // Arrange: Sequential data
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add sequential values with specific slopes at different positions
        var values = new[] { 5.0, 10.0, 15.0, 20.0, 25.0, 30.0 };
        foreach (var val in values)
        {
            updater.UpdateRegressionResults(
                new Dictionary<string, double> { ["metric"] = val },
                timestamp);
        }

        // Act
        var result = updater.UpdateRegressionResults(
            new Dictionary<string, double> { ["metric"] = 35.0 },
            timestamp);

        // Assert
        var regression = result["metric"];
        // Should detect clear positive trend
        Assert.True(regression.Slope > 0);
        // Should have high R² for perfect linear data
        Assert.True(regression.RSquared > 0.99);
    }

    [Fact]
    public void GetHistorySize_Should_Return_Zero_For_Unknown_Metric()
    {
        // Arrange
        var updater = new RegressionUpdater(_loggerMock.Object);

        // Act
        var size = updater.GetHistorySize("unknown");

        // Assert
        Assert.Equal(0, size);
    }

    [Fact]
    public void UpdateRegressionResults_Should_Handle_Large_Value_Ranges()
    {
        // Arrange: Very large and very small values
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add data with large range
        var values = new[] { 1000.0, 2000.0, 3000.0, 4000.0, 5000.0 };
        foreach (var val in values)
        {
            updater.UpdateRegressionResults(
                new Dictionary<string, double> { ["metric"] = val },
                timestamp);
        }

        // Act
        var result = updater.UpdateRegressionResults(
            new Dictionary<string, double> { ["metric"] = 6000.0 },
            timestamp);

        // Assert
        var regression = result["metric"];
        Assert.True(regression.Slope > 0);
        Assert.NotEqual(double.NaN, regression.Slope);
        Assert.False(double.IsInfinity(regression.Slope));
    }

    [Fact]
    public void UpdateRegressionResults_Should_Handle_Fractional_Values()
    {
        // Arrange: Fractional values
        var updater = new RegressionUpdater(_loggerMock.Object);
        var timestamp = DateTime.UtcNow;

        // Add fractional data
        var values = new[] { 0.1, 0.2, 0.3, 0.4, 0.5 };
        foreach (var val in values)
        {
            updater.UpdateRegressionResults(
                new Dictionary<string, double> { ["metric"] = val },
                timestamp);
        }

        // Act
        var result = updater.UpdateRegressionResults(
            new Dictionary<string, double> { ["metric"] = 0.6 },
            timestamp);

        // Assert
        var regression = result["metric"];
        // Should handle fractional values correctly
        Assert.True(regression.Slope > 0);
        Assert.True(regression.RSquared > 0.99);
    }
}
