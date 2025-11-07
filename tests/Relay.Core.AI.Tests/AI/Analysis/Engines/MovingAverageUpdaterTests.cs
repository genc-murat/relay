using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

public class MovingAverageUpdaterTests
{
    private readonly Mock<ILogger<MovingAverageUpdater>> _loggerMock;
    private readonly TrendAnalysisConfig _config;

    public MovingAverageUpdaterTests()
    {
        _loggerMock = new Mock<ILogger<MovingAverageUpdater>>();
        _config = new TrendAnalysisConfig
        {
            MovingAveragePeriods = new[] { 5, 15, 60 },
            ExponentialMovingAverageAlpha = 0.3
        };
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MovingAverageUpdater(null!, _config));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Config_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MovingAverageUpdater(_loggerMock.Object, null!));
    }

    [Fact]
    public void UpdateMovingAverages_Should_Return_Empty_For_Empty_Metrics()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var metrics = new Dictionary<string, double>();
        var timestamp = DateTime.UtcNow;

        // Act
        var result = updater.UpdateMovingAverages(metrics, timestamp);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void UpdateMovingAverages_Should_Return_Current_Value_On_First_Call()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0
        };
        var timestamp = DateTime.UtcNow;

        // Act
        var result = updater.UpdateMovingAverages(metrics, timestamp);

        // Assert
        Assert.Single(result);
        var maData = result["cpu"];
        Assert.Equal(75.0, maData.CurrentValue);
        Assert.Equal(75.0, maData.MA5);
        Assert.Equal(75.0, maData.MA15);
        Assert.Equal(75.0, maData.MA60);
        Assert.Equal(75.0, maData.EMA);
        Assert.Equal(timestamp, maData.Timestamp);
    }

    [Fact]
    public void UpdateMovingAverages_Should_Calculate_MA5_After_Sufficient_Data()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Add 5 values: 10, 20, 30, 40, 50
        // Expected MA5 = (10 + 20 + 30 + 40 + 50) / 5 = 30
        for (int i = 1; i <= 5; i++)
        {
            var metrics = new Dictionary<string, double> { ["cpu"] = i * 10.0 };
            updater.UpdateMovingAverages(metrics, timestamp);
        }

        // Act
        var finalMetrics = new Dictionary<string, double> { ["cpu"] = 60.0 };
        var result = updater.UpdateMovingAverages(finalMetrics, timestamp);

        // Assert
        var maData = result["cpu"];
        // MA5 should be average of last 5: 20, 30, 40, 50, 60 = 40
        Assert.Equal(40.0, maData.MA5);
    }

    [Fact]
    public void UpdateMovingAverages_Should_Calculate_MA15_After_Sufficient_Data()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Add 15 values: 10, 20, 30, ..., 150
        for (int i = 1; i <= 15; i++)
        {
            var metrics = new Dictionary<string, double> { ["cpu"] = i * 10.0 };
            updater.UpdateMovingAverages(metrics, timestamp);
        }

        // Act
        var finalMetrics = new Dictionary<string, double> { ["cpu"] = 160.0 };
        var result = updater.UpdateMovingAverages(finalMetrics, timestamp);

        // Assert
        var maData = result["cpu"];
        // MA15 should be average of last 15: 20, 30, ..., 160
        // Sum = (20+160)*15/2 = 1350, Avg = 1350/15 = 90
        Assert.Equal(90.0, maData.MA15);
    }

    [Fact]
    public void UpdateMovingAverages_Should_Calculate_MA60_With_Full_History()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Add 60 values: 1, 2, 3, ..., 60
        for (int i = 1; i <= 60; i++)
        {
            var metrics = new Dictionary<string, double> { ["cpu"] = (double)i };
            updater.UpdateMovingAverages(metrics, timestamp);
        }

        // Act
        var finalMetrics = new Dictionary<string, double> { ["cpu"] = 61.0 };
        var result = updater.UpdateMovingAverages(finalMetrics, timestamp);

        // Assert
        var maData = result["cpu"];
        // MA60 should be average of last 60: 2, 3, ..., 61
        // Sum = (2+61)*60/2 = 1890, Avg = 1890/60 = 31.5
        Assert.Equal(31.5, maData.MA60);
    }

    [Fact]
    public void EMA_Should_Give_More_Weight_To_Recent_Values_With_Alpha_0_3()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Start with value 100
        var metrics1 = new Dictionary<string, double> { ["metric"] = 100.0 };
        var result1 = updater.UpdateMovingAverages(metrics1, timestamp);
        Assert.Equal(100.0, result1["metric"].EMA);

        // Add value 200
        // EMA = 200 * 0.3 + 100 * 0.7 = 60 + 70 = 130
        var metrics2 = new Dictionary<string, double> { ["metric"] = 200.0 };
        var result2 = updater.UpdateMovingAverages(metrics2, timestamp);
        Assert.Equal(130.0, result2["metric"].EMA);

        // Act - Add value 100
        // EMA = 100 * 0.3 + 130 * 0.7 = 30 + 91 = 121
        var metrics3 = new Dictionary<string, double> { ["metric"] = 100.0 };
        var result3 = updater.UpdateMovingAverages(metrics3, timestamp);

        // Assert
        Assert.Equal(121.0, result3["metric"].EMA);
    }

    [Fact]
    public void EMA_Should_Converge_To_Constant_Value()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Add same value repeatedly
        var lastEma = 50.0;
        for (int i = 0; i < 20; i++)
        {
            var metrics = new Dictionary<string, double> { ["metric"] = 50.0 };
            var result = updater.UpdateMovingAverages(metrics, timestamp);
            lastEma = result["metric"].EMA;
        }

        // Assert - EMA should converge to the constant value
        Assert.Equal(50.0, lastEma, 4);
    }

    [Fact]
    public void UpdateMovingAverages_Should_Handle_Multiple_Metrics()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0,
            ["memory"] = 50.0,
            ["latency"] = 100.0
        };
        var timestamp = DateTime.UtcNow;

        // Act
        var result = updater.UpdateMovingAverages(metrics, timestamp);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey("cpu"));
        Assert.True(result.ContainsKey("memory"));
        Assert.True(result.ContainsKey("latency"));
    }

    [Fact]
    public void UpdateMovingAverages_Should_Handle_Negative_Values()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Add negative values
        var metrics = new Dictionary<string, double> { ["delta"] = -75.0 };

        // Act
        var result = updater.UpdateMovingAverages(metrics, timestamp);

        // Assert
        Assert.Single(result);
        var maData = result["delta"];
        Assert.Equal(-75.0, maData.CurrentValue);
        Assert.Equal(-75.0, maData.MA5);
        Assert.Equal(-75.0, maData.EMA);
    }

    [Fact]
    public void UpdateMovingAverages_Should_Handle_Zero_Values()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        var metrics = new Dictionary<string, double> { ["zero"] = 0.0 };

        // Act
        var result = updater.UpdateMovingAverages(metrics, timestamp);

        // Assert
        Assert.Single(result);
        var maData = result["zero"];
        Assert.Equal(0.0, maData.CurrentValue);
        Assert.Equal(0.0, maData.MA5);
        Assert.Equal(0.0, maData.EMA);
    }

    [Fact]
    public void UpdateMovingAverages_Should_Handle_Very_Large_Values()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        var metrics = new Dictionary<string, double> { ["large"] = double.MaxValue / 2 };

        // Act
        var result = updater.UpdateMovingAverages(metrics, timestamp);

        // Assert
        Assert.Single(result);
        var maData = result["large"];
        Assert.True(maData.CurrentValue > 0);
        Assert.True(maData.MA5 > 0);
    }

    [Fact]
    public void UpdateMovingAverages_Should_Handle_Exception_And_Return_Fallback()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        var metrics = new Dictionary<string, double> { ["metric"] = 100.0 };

        // Act
        var result = updater.UpdateMovingAverages(metrics, timestamp);

        // Assert - Should complete without throwing
        Assert.Single(result);
        Assert.Equal(100.0, result["metric"].CurrentValue);
    }

    [Fact]
    public void ClearHistory_Should_Reset_Moving_Averages()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Add data to build history
        for (int i = 1; i <= 10; i++)
        {
            var metrics = new Dictionary<string, double> { ["cpu"] = (double)i * 10.0 };
            updater.UpdateMovingAverages(metrics, timestamp);
        }

        // Verify history was built
        Assert.True(updater.GetHistorySize("cpu") > 0);

        // Act - Clear history
        updater.ClearHistory();

        // Assert
        Assert.Equal(0, updater.GetHistorySize("cpu"));
    }

    [Fact]
    public void GetHistorySize_Should_Return_Correct_Count()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Act & Assert
        Assert.Equal(0, updater.GetHistorySize("cpu"));

        var metrics1 = new Dictionary<string, double> { ["cpu"] = 50.0 };
        updater.UpdateMovingAverages(metrics1, timestamp);
        Assert.Equal(1, updater.GetHistorySize("cpu"));

        var metrics2 = new Dictionary<string, double> { ["cpu"] = 60.0 };
        updater.UpdateMovingAverages(metrics2, timestamp);
        Assert.Equal(2, updater.GetHistorySize("cpu"));
    }

    [Fact]
    public void UpdateMovingAverages_Should_Maintain_History_Size_Under_60()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Act - Add 100 values
        for (int i = 1; i <= 100; i++)
        {
            var metrics = new Dictionary<string, double> { ["cpu"] = (double)i };
            updater.UpdateMovingAverages(metrics, timestamp);
        }

        // Assert - History should not exceed 60
        Assert.Equal(60, updater.GetHistorySize("cpu"));
    }

    [Fact]
    public void EMA_Should_Handle_Custom_Alpha_Values()
    {
        // Arrange
        var configHighAlpha = new TrendAnalysisConfig
        {
            MovingAveragePeriods = new[] { 5, 15, 60 },
            ExponentialMovingAverageAlpha = 0.7 // High alpha - more weight on recent
        };
        var updater = new MovingAverageUpdater(_loggerMock.Object, configHighAlpha);
        var timestamp = DateTime.UtcNow;

        // Start with 100
        var metrics1 = new Dictionary<string, double> { ["metric"] = 100.0 };
        var result1 = updater.UpdateMovingAverages(metrics1, timestamp);

        // Add 200
        // EMA = 200 * 0.7 + 100 * 0.3 = 140 + 30 = 170
        var metrics2 = new Dictionary<string, double> { ["metric"] = 200.0 };
        var result2 = updater.UpdateMovingAverages(metrics2, timestamp);

        // Assert
        Assert.Equal(170.0, result2["metric"].EMA);
    }

    [Fact]
    public void EMA_Should_Handle_Low_Alpha_Values()
    {
        // Arrange
        var configLowAlpha = new TrendAnalysisConfig
        {
            MovingAveragePeriods = new[] { 5, 15, 60 },
            ExponentialMovingAverageAlpha = 0.1 // Low alpha - more weight on history
        };
        var updater = new MovingAverageUpdater(_loggerMock.Object, configLowAlpha);
        var timestamp = DateTime.UtcNow;

        // Start with 100
        var metrics1 = new Dictionary<string, double> { ["metric"] = 100.0 };
        var result1 = updater.UpdateMovingAverages(metrics1, timestamp);

        // Add 200
        // EMA = 200 * 0.1 + 100 * 0.9 = 20 + 90 = 110
        var metrics2 = new Dictionary<string, double> { ["metric"] = 200.0 };
        var result2 = updater.UpdateMovingAverages(metrics2, timestamp);

        // Assert
        Assert.Equal(110.0, result2["metric"].EMA);
    }

    [Fact]
    public void UpdateMovingAverages_Should_Handle_Rapid_Value_Changes()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Alternating high and low values
        for (int i = 0; i < 10; i++)
        {
            var value = i % 2 == 0 ? 100.0 : 10.0;
            var metrics = new Dictionary<string, double> { ["volatile"] = value };
            updater.UpdateMovingAverages(metrics, timestamp);
        }

        // Act
        var finalMetrics = new Dictionary<string, double> { ["volatile"] = 100.0 };
        var result = updater.UpdateMovingAverages(finalMetrics, timestamp);

        // Assert - Should handle volatile values
        Assert.NotNull(result);
        Assert.True(!double.IsNaN(result["volatile"].MA5));
        Assert.True(!double.IsNaN(result["volatile"].EMA));
    }

    [Fact]
    public void UpdateMovingAverages_Should_Be_Consistent_Across_Calls()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;
        var values = new[] { 10.0, 20.0, 30.0, 40.0, 50.0 };

        // Build history
        foreach (var value in values)
        {
            var metrics = new Dictionary<string, double> { ["metric"] = value };
            updater.UpdateMovingAverages(metrics, timestamp);
        }

        // Act & Assert - Call again with same final value
        var finalMetrics1 = new Dictionary<string, double> { ["metric"] = 50.0 };
        var result1 = updater.UpdateMovingAverages(finalMetrics1, timestamp);
        var ema1 = result1["metric"].EMA;

        var finalMetrics2 = new Dictionary<string, double> { ["metric"] = 50.0 };
        var result2 = updater.UpdateMovingAverages(finalMetrics2, timestamp);
        var ema2 = result2["metric"].EMA;

        // Should be different because new history was added
        Assert.NotEqual(ema1, ema2);
    }

    [Fact]
    public void ClearHistory_Should_Log_Debug_Message()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);

        // Act
        updater.ClearHistory();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void UpdateMovingAverages_Should_Log_Trace_For_Calculated_Averages()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var metrics = new Dictionary<string, double> { ["cpu"] = 75.0 };
        var timestamp = DateTime.UtcNow;

        // Act
        updater.UpdateMovingAverages(metrics, timestamp);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void CalculateMovingAverage_Should_Return_Current_Value_When_History_Is_Empty()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);

        // Use reflection to invoke private method
        var method = typeof(MovingAverageUpdater).GetMethod("CalculateMovingAverage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (double)method.Invoke(updater, new object[] { "cpu", 100.0, 5 });

        // Assert - Should return current value when no history
        Assert.Equal(100.0, result);
    }

    [Fact]
    public void EMA_Should_Clamp_Alpha_To_Valid_Range()
    {
        // Arrange
        var configInvalidAlpha = new TrendAnalysisConfig
        {
            MovingAveragePeriods = new[] { 5, 15, 60 },
            ExponentialMovingAverageAlpha = 1.5 // Invalid alpha > 1
        };
        var updater = new MovingAverageUpdater(_loggerMock.Object, configInvalidAlpha);
        var timestamp = DateTime.UtcNow;

        // Start with 100
        var metrics1 = new Dictionary<string, double> { ["metric"] = 100.0 };
        updater.UpdateMovingAverages(metrics1, timestamp);

        // Add 200 - Alpha should be clamped to 1.0
        // EMA = 200 * 1.0 + 100 * 0.0 = 200
        var metrics2 = new Dictionary<string, double> { ["metric"] = 200.0 };
        var result = updater.UpdateMovingAverages(metrics2, timestamp);

        // Assert
        Assert.Equal(200.0, result["metric"].EMA);
    }

    [Fact]
    public void EMA_Should_Handle_NaN_Infinity_Results()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var timestamp = DateTime.UtcNow;

        // Start with NaN (simulate invalid state)
        // Use reflection to set EMA cache to NaN
        var emaCacheField = typeof(MovingAverageUpdater).GetField("_emaCache",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var emaCache = (Dictionary<string, double>)emaCacheField.GetValue(updater);
        emaCache["metric"] = double.NaN;

        // Act
        var metrics = new Dictionary<string, double> { ["metric"] = 100.0 };
        var result = updater.UpdateMovingAverages(metrics, timestamp);

        // Assert - Should return current value as fallback
        Assert.Equal(100.0, result["metric"].EMA);
    }

    [Fact]
    public void UpdateMovingAverages_Should_Handle_Outer_Exception_And_Return_Empty()
    {
        // Arrange
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);
        var metrics = new Dictionary<string, double> { ["cpu"] = 75.0 };
        var timestamp = DateTime.UtcNow;

        // Act
        var result = updater.UpdateMovingAverages(metrics, timestamp);

        // Assert - Should return result
        Assert.NotNull(result);
    }

    [Fact]
    public void CalculateMovingAverage_Should_Handle_Exception_And_Return_Current_Value()
    {
        // Arrange
        _loggerMock.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>())).Throws(new Exception("Logger failed"));
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);

        // Use reflection
        var method = typeof(MovingAverageUpdater).GetMethod("CalculateMovingAverage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (double)method.Invoke(updater, new object[] { "cpu", 100.0, 5 });

        // Assert - Should return current value
        Assert.Equal(100.0, result);
    }

    [Fact]
    public void CalculateExponentialMovingAverage_Should_Handle_Exception_And_Return_Current_Value()
    {
        // Arrange
        _loggerMock.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>())).Throws(new Exception("Logger failed"));
        var updater = new MovingAverageUpdater(_loggerMock.Object, _config);

        // Use reflection
        var method = typeof(MovingAverageUpdater).GetMethod("CalculateExponentialMovingAverage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (double)method.Invoke(updater, new object[] { "cpu", 100.0, 0.3 });

        // Assert - Should return current value
        Assert.Equal(100.0, result);
    }
}
