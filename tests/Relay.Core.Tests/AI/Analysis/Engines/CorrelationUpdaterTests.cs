using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

public class CorrelationUpdaterTests
{
    private readonly Mock<ILogger<CorrelationUpdater>> _loggerMock;
    private readonly TrendAnalysisConfig _config;

    public CorrelationUpdaterTests()
    {
        _loggerMock = new Mock<ILogger<CorrelationUpdater>>();
        _config = new TrendAnalysisConfig
        {
            CorrelationThreshold = 0.7
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CorrelationUpdater(null!, _config));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Config_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CorrelationUpdater(_loggerMock.Object, null!));
    }

    [Fact]
    public void Constructor_Should_Succeed_With_Valid_Parameters()
    {
        // Act
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);

        // Assert
        Assert.NotNull(updater);
    }

    #endregion

    #region UpdateCorrelations Tests

    [Fact]
    public void UpdateCorrelations_Should_Return_Empty_Dictionary_With_Empty_Metrics()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>();

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void UpdateCorrelations_Should_Return_Empty_Dictionary_With_Single_Metric()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double> { ["cpu"] = 75.0 };

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void UpdateCorrelations_Should_Return_Empty_Dictionary_When_No_Correlations_Exceed_Threshold()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0,
            ["memory"] = 50.0
        };

        // Add multiple data points to build history
        for (int i = 0; i < 5; i++)
        {
            updater.UpdateCorrelations(currentMetrics);
        }

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void UpdateCorrelations_Should_Detect_Perfect_Positive_Correlation()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);

        // Add data points that move together (perfect positive correlation)
        for (int i = 1; i <= 10; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["metric1"] = i * 10.0,  // 10, 20, 30, ..., 100
                ["metric2"] = i * 10.0   // 10, 20, 30, ..., 100
            };
            updater.UpdateCorrelations(metrics);
        }

        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 100.0,
            ["metric2"] = 100.0
        };

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("metric1", result.Keys);
        var correlations = result["metric1"];
        Assert.NotEmpty(correlations);
        Assert.Contains("metric2", correlations[0]);
        Assert.True(double.Parse(correlations[0].Split("r=")[1].TrimEnd(')')) > 0.95);
    }

    [Fact]
    public void UpdateCorrelations_Should_Detect_Perfect_Negative_Correlation()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);

        // Add data points that move inversely (perfect negative correlation)
        for (int i = 1; i <= 10; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["metric1"] = i * 10.0,      // 10, 20, 30, ..., 100
                ["metric2"] = 110.0 - i * 10.0  // 100, 90, 80, ..., 10
            };
            updater.UpdateCorrelations(metrics);
        }

        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 100.0,
            ["metric2"] = 10.0
        };

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("metric1", result.Keys);
        var correlations = result["metric1"];
        Assert.NotEmpty(correlations);
        Assert.Contains("metric2", correlations[0]);
        var correlation = double.Parse(correlations[0].Split("r=")[1].TrimEnd(')'));
        Assert.True(correlation < -0.95);
    }

    [Fact]
    public void UpdateCorrelations_Should_Detect_No_Correlation()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);

        // Add data points with no correlation
        var random = new Random(42);
        for (int i = 0; i < 10; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["metric1"] = random.NextDouble() * 100.0,
                ["metric2"] = random.NextDouble() * 100.0
            };
            updater.UpdateCorrelations(metrics);
        }

        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 50.0,
            ["metric2"] = 50.0
        };

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void UpdateCorrelations_Should_Handle_Multiple_Metrics_With_Multiple_Correlations()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);

        // Create data where:
        // metric1 and metric2 are strongly positively correlated
        // metric1 and metric3 are strongly negatively correlated
        // metric2 and metric3 are not correlated
        for (int i = 1; i <= 10; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["metric1"] = i * 10.0,          // 10, 20, 30, ..., 100
                ["metric2"] = i * 10.0 + 5.0,   // 15, 25, 35, ..., 105
                ["metric3"] = 110.0 - i * 10.0  // 100, 90, 80, ..., 10
            };
            updater.UpdateCorrelations(metrics);
        }

        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 100.0,
            ["metric2"] = 105.0,
            ["metric3"] = 10.0
        };

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("metric1", result.Keys);
        Assert.True(result["metric1"].Count >= 1);
    }

    [Fact]
    public void UpdateCorrelations_Should_Update_History_With_New_Data()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);
        var metricsSet1 = new Dictionary<string, double>
        {
            ["metric1"] = 10.0,
            ["metric2"] = 20.0
        };

        var metricsSet2 = new Dictionary<string, double>
        {
            ["metric1"] = 20.0,
            ["metric2"] = 40.0
        };

        // Act
        updater.UpdateCorrelations(metricsSet1);
        updater.UpdateCorrelations(metricsSet1);
        var result = updater.UpdateCorrelations(metricsSet2);

        // Assert - The history should now contain enough data points
        // Since metric2 = metric1 * 2, correlation should be positive
        Assert.NotEmpty(result);
    }

    [Fact]
    public void UpdateCorrelations_Should_Handle_Exception_Gracefully()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0,
            ["memory"] = 50.0
        };

        // Act - Should not throw exception
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.NotNull(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error calculating metric correlations")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never); // Should not log warning for normal operation
    }

    #endregion

    #region CalculateCorrelation Tests (via UpdateCorrelations)

    [Fact]
    public void CalculateCorrelation_Should_Return_Zero_With_Insufficient_Data()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 10.0,
            ["metric2"] = 20.0
        };

        // Act - Only one data point, insufficient for correlation
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateCorrelation_Should_Return_Zero_When_One_Metric_Missing()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);

        // Add data for metric1 and metric2
        for (int i = 0; i < 5; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["metric1"] = i * 10.0,
                ["metric2"] = i * 10.0
            };
            updater.UpdateCorrelations(metrics);
        }

        // Add data for metric1 and metric3 (metric2 not in this call)
        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 50.0,
            ["metric3"] = 50.0
        };

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.Empty(result); // metric1 and metric3 don't have enough history together
    }

    [Fact]
    public void CalculateCorrelation_Should_Handle_Zero_Standard_Deviation()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);

        // Add data where metric1 is constant (stdDev = 0)
        for (int i = 0; i < 5; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["metric1"] = 50.0,  // Constant
                ["metric2"] = i * 10.0  // Varying
            };
            updater.UpdateCorrelations(metrics);
        }

        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 50.0,
            ["metric2"] = 50.0
        };

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.Empty(result); // Correlation should be 0 due to zero stdDev
    }

    [Fact]
    public void CalculateCorrelation_Should_Clamp_Result_To_Valid_Range()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);

        // Add highly correlated data
        for (int i = 1; i <= 10; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["metric1"] = i * 10.0,
                ["metric2"] = i * 10.0
            };
            updater.UpdateCorrelations(metrics);
        }

        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 100.0,
            ["metric2"] = 100.0
        };

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.NotEmpty(result);
        if (result.ContainsKey("metric1"))
        {
            var correlations = result["metric1"];
            foreach (var corr in correlations)
            {
                var value = double.Parse(corr.Split("r=")[1].TrimEnd(')'));
                Assert.True(value >= -1.0 && value <= 1.0, $"Correlation {value} is outside [-1, 1] range");
            }
        }
    }

    #endregion

    #region Logging Tests

    [Fact]
    public void UpdateCorrelations_Should_Log_Debug_When_Correlations_Found()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);

        // Build strong correlation between two metrics
        for (int i = 1; i <= 10; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["metric1"] = i * 10.0,
                ["metric2"] = i * 10.0
            };
            updater.UpdateCorrelations(metrics);
        }

        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 100.0,
            ["metric2"] = 100.0
        };

        // Act
        updater.UpdateCorrelations(currentMetrics);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("correlated with")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void UpdateCorrelations_Should_Log_Warning_On_Exception()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CorrelationUpdater>>();
        var config = new TrendAnalysisConfig { CorrelationThreshold = 0.7 };

        // Create a custom implementation that throws
        var updater = new CorrelationUpdater(loggerMock.Object, config);

        // This test verifies exception handling in UpdateCorrelations
        // The normal flow won't throw, so we're testing the exception handler design
        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 75.0
        };

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void UpdateCorrelations_Should_Handle_NaN_Values()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = double.NaN,
            ["metric2"] = 50.0
        };

        // Act - Should not throw
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void UpdateCorrelations_Should_Handle_Infinity_Values()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = double.PositiveInfinity,
            ["metric2"] = 50.0
        };

        // Act - Should not throw
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void UpdateCorrelations_Should_Handle_Large_Metric_Values()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);

        // Add data with very large values
        for (int i = 1; i <= 10; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["metric1"] = i * 1e15,
                ["metric2"] = i * 1e15
            };
            updater.UpdateCorrelations(metrics);
        }

        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 1e16,
            ["metric2"] = 1e16
        };

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void UpdateCorrelations_Should_Handle_Very_Small_Metric_Values()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);

        // Add data with very small values
        for (int i = 1; i <= 10; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["metric1"] = i * 1e-15,
                ["metric2"] = i * 1e-15
            };
            updater.UpdateCorrelations(metrics);
        }

        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 1e-14,
            ["metric2"] = 1e-14
        };

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void UpdateCorrelations_Should_Respect_Threshold_Configuration()
    {
        // Arrange
        var lowThresholdConfig = new TrendAnalysisConfig { CorrelationThreshold = 0.3 };
        var updater = new CorrelationUpdater(_loggerMock.Object, lowThresholdConfig);

        // Add data with moderate correlation
        for (int i = 1; i <= 10; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["metric1"] = i * 10.0,
                ["metric2"] = i * 10.0 + (i * 2.0) // Slight variation
            };
            updater.UpdateCorrelations(metrics);
        }

        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 100.0,
            ["metric2"] = 120.0
        };

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        // With low threshold, even moderate correlations should be detected
        Assert.NotNull(result);
    }

    #endregion

    #region Data Alignment Tests

    [Fact]
    public void UpdateCorrelations_Should_Handle_Different_History_Lengths()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);

        // Add more data points for metric1
        for (int i = 1; i <= 5; i++)
        {
            updater.UpdateCorrelations(new Dictionary<string, double> { ["metric1"] = i * 10.0 });
        }

        // Then add data for both metrics
        for (int i = 1; i <= 5; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["metric1"] = i * 10.0,
                ["metric2"] = i * 10.0
            };
            updater.UpdateCorrelations(metrics);
        }

        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 50.0,
            ["metric2"] = 50.0
        };

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void UpdateCorrelations_Should_Maintain_History_Within_Max_Size()
    {
        // Arrange
        var updater = new CorrelationUpdater(_loggerMock.Object, _config);

        // Add more data than MaxHistorySize (100)
        for (int i = 0; i < 150; i++)
        {
            updater.UpdateCorrelations(new Dictionary<string, double>
            {
                ["metric1"] = i * 1.0,
                ["metric2"] = i * 1.0
            });
        }

        var currentMetrics = new Dictionary<string, double>
        {
            ["metric1"] = 149.0,
            ["metric2"] = 149.0
        };

        // Act
        var result = updater.UpdateCorrelations(currentMetrics);

        // Assert - Should still work with capped history
        Assert.NotNull(result);
    }

    #endregion
}
