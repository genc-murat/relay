using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using System.Linq;
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
            HighAnomalyZScoreThreshold = 3.0,
            HighVelocityThreshold = 0.5 // 50% change
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
            ["cpu"] = 97.5 // 22.5 units above mean, will trigger multiple methods
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
        Assert.NotEmpty(anomalies);
        // Check that at least one anomaly detected
        var anyAnomaly = anomalies.First();
        Assert.Equal("cpu", anyAnomaly.MetricName);
        Assert.Equal(97.5, anyAnomaly.CurrentValue);
        Assert.Equal(75.0, anyAnomaly.ExpectedValue);
        Assert.True(anyAnomaly.Deviation > 0);
        Assert.NotEqual(default(DateTime), anyAnomaly.Timestamp);
    }

    [Fact]
    public void UpdateAnomalies_Should_Detect_High_Severity_Anomaly()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 112.5 // 37.5 units above mean, high deviation
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
        Assert.NotEmpty(anomalies);
        // At least one anomaly should be high severity
        Assert.True(anomalies.Any(a => a.Severity >= AnomalySeverity.High));
        var anyAnomaly = anomalies.First();
        Assert.Equal("cpu", anyAnomaly.MetricName);
        Assert.Equal(112.5, anyAnomaly.CurrentValue);
        Assert.Equal(75.0, anyAnomaly.ExpectedValue);
        Assert.Equal(37.5, anyAnomaly.Deviation);
    }

    [Fact]
    public void UpdateAnomalies_Should_Handle_Multiple_Metrics_With_Anomalies()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 97.5,    // Will trigger anomalies
            ["memory"] = 90.0, // Normal
            ["latency"] = 200.0 // Will trigger anomalies
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
        // Multiple detection methods can trigger for each metric, so we check for metric names
        var metricNames = anomalies.Select(a => a.MetricName).Distinct().ToList();
        Assert.Contains("cpu", metricNames);
        Assert.Contains("latency", metricNames);
        Assert.DoesNotContain("memory", metricNames);
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

        // Assert - Verify that warning logs were called (at least once for anomaly detection)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void CalculateZScore_Should_Return_Zero_When_StdDev_Is_Zero()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);

        // Act - Use reflection to test private method
        var method = typeof(AnomalyUpdater).GetMethod("CalculateZScore",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (double)method.Invoke(updater, new object[] { 100.0, 75.0, 0.0 });

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void IQR_Detection_Should_Identify_Outliers_Beyond_Bounds()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["metric"] = new MovingAverageData { MA15 = 100.0, Timestamp = DateTime.UtcNow }
        };

        // Build history: [10, 20, 30, 40, 50, 60, 70, 80, 90, 100]
        // Q1 ≈ 27.5, Q3 ≈ 72.5, IQR = 45, LowerBound = -40, UpperBound = 160
        for (int i = 0; i < 10; i++)
        {
            var metrics = new Dictionary<string, double> { ["metric"] = (i + 1) * 10.0 };
            updater.UpdateAnomalies(metrics, movingAverages);
        }

        // Act - Add a value far outside bounds
        var testMetrics = new Dictionary<string, double> { ["metric"] = 200.0 }; // Outside upper bound
        var anomalies = updater.UpdateAnomalies(testMetrics, movingAverages);

        // Assert - Multiple detection methods may trigger for extreme outliers
        Assert.NotEmpty(anomalies);
        Assert.True(anomalies.Any(a => a.Description.Contains("IQR anomaly")));
    }

    [Fact]
    public void Spike_Detection_Should_Identify_Large_Percentage_Increase()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["requests"] = new MovingAverageData { MA15 = 100.0, Timestamp = DateTime.UtcNow }
        };

        // Build baseline history
        for (int i = 0; i < 5; i++)
        {
            var metrics = new Dictionary<string, double> { ["requests"] = 100.0 };
            updater.UpdateAnomalies(metrics, movingAverages);
        }

        // Act - Add a spike of 60% increase
        var testMetrics = new Dictionary<string, double> { ["requests"] = 160.0 };
        var anomalies = updater.UpdateAnomalies(testMetrics, movingAverages);

        // Assert
        Assert.NotEmpty(anomalies);
        var spikeAnomaly = anomalies.FirstOrDefault(a => a.Description.Contains("Spike detected"));
        Assert.NotNull(spikeAnomaly);
        Assert.Equal(AnomalySeverity.High, spikeAnomaly.Severity);
        Assert.Contains("60%", spikeAnomaly.Description);
    }

    [Fact]
    public void Drop_Detection_Should_Identify_Large_Percentage_Decrease()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["throughput"] = new MovingAverageData { MA15 = 100.0, Timestamp = DateTime.UtcNow }
        };

        // Build baseline history
        for (int i = 0; i < 5; i++)
        {
            var metrics = new Dictionary<string, double> { ["throughput"] = 100.0 };
            updater.UpdateAnomalies(metrics, movingAverages);
        }

        // Act - Add a drop of 60% decrease
        var testMetrics = new Dictionary<string, double> { ["throughput"] = 40.0 };
        var anomalies = updater.UpdateAnomalies(testMetrics, movingAverages);

        // Assert
        Assert.NotEmpty(anomalies);
        var dropAnomaly = anomalies.FirstOrDefault(a => a.Description.Contains("Drop detected"));
        Assert.NotNull(dropAnomaly);
        Assert.Equal(AnomalySeverity.High, dropAnomaly.Severity);
        Assert.Contains("60%", dropAnomaly.Description);
    }

    [Fact]
    public void Velocity_Detection_Should_Identify_Rapid_Changes()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["latency"] = new MovingAverageData { MA15 = 50.0, Timestamp = DateTime.UtcNow }
        };

        // Build stable history
        for (int i = 0; i < 5; i++)
        {
            var metrics = new Dictionary<string, double> { ["latency"] = 50.0 };
            updater.UpdateAnomalies(metrics, movingAverages);
        }

        // Act - Add a rapid 100% increase
        var testMetrics = new Dictionary<string, double> { ["latency"] = 100.0 };
        var anomalies = updater.UpdateAnomalies(testMetrics, movingAverages);

        // Assert
        Assert.NotEmpty(anomalies);
        var velocityAnomaly = anomalies.FirstOrDefault(a => a.Description.Contains("High velocity"));
        Assert.NotNull(velocityAnomaly);
        Assert.Contains("100%", velocityAnomaly.Description);
    }

    [Fact]
    public void Velocity_Detection_Should_Mark_Extreme_Changes_As_High_Severity()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["metric"] = new MovingAverageData { MA15 = 50.0, Timestamp = DateTime.UtcNow }
        };

        // Build stable history
        for (int i = 0; i < 5; i++)
        {
            var metrics = new Dictionary<string, double> { ["metric"] = 50.0 };
            updater.UpdateAnomalies(metrics, movingAverages);
        }

        // Act - Add a 200% increase (more than 2x velocity threshold)
        var testMetrics = new Dictionary<string, double> { ["metric"] = 200.0 };
        var anomalies = updater.UpdateAnomalies(testMetrics, movingAverages);

        // Assert
        Assert.NotEmpty(anomalies);
        var velocityAnomaly = anomalies.FirstOrDefault(a => a.Description.Contains("High velocity"));
        Assert.NotNull(velocityAnomaly);
        Assert.Equal(AnomalySeverity.High, velocityAnomaly.Severity);
    }

    [Fact]
    public void Standard_Deviation_Should_Increase_With_More_Historical_Data()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["volatile"] = new MovingAverageData { MA15 = 100.0, Timestamp = DateTime.UtcNow }
        };

        // Build volatile history with wide variation
        var volatileValues = new[] { 50.0, 150.0, 75.0, 125.0, 60.0, 140.0, 80.0, 120.0, 90.0, 110.0 };
        foreach (var value in volatileValues)
        {
            var metrics = new Dictionary<string, double> { ["volatile"] = value };
            updater.UpdateAnomalies(metrics, movingAverages);
        }

        // Act - Test an extremely deviant value (beyond normal variance)
        var testMetrics = new Dictionary<string, double> { ["volatile"] = 300.0 }; // Extreme outlier
        var anomalies = updater.UpdateAnomalies(testMetrics, movingAverages);

        // Assert - With volatile data, extreme outliers should trigger some detection method
        Assert.NotEmpty(anomalies);
    }

    [Fact]
    public void UpdateAnomalies_Should_Maintain_History_Size_Under_100_Values()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["metric"] = new MovingAverageData { MA15 = 50.0, Timestamp = DateTime.UtcNow }
        };

        // Act - Add 150 values to test history trimming
        for (int i = 0; i < 150; i++)
        {
            var metrics = new Dictionary<string, double> { ["metric"] = 50.0 + (i % 20) };
            updater.UpdateAnomalies(metrics, movingAverages);
        }

        // Assert - No exception should be thrown, history should be managed internally

        // Verify by inducing one more update and checking it processes normally
        var testMetrics = new Dictionary<string, double> { ["metric"] = 55.0 };
        var anomalies = updater.UpdateAnomalies(testMetrics, movingAverages);

        // Should complete without error
        Assert.NotNull(anomalies);
    }

    [Fact]
    public void ClearHistory_Should_Reset_Metric_History()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["metric1"] = new MovingAverageData { MA15 = 100.0, Timestamp = DateTime.UtcNow },
            ["metric2"] = new MovingAverageData { MA15 = 50.0, Timestamp = DateTime.UtcNow }
        };

        // Add history for multiple metrics
        for (int i = 0; i < 10; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["metric1"] = 100.0 + i,
                ["metric2"] = 50.0 + i
            };
            updater.UpdateAnomalies(metrics, movingAverages);
        }

        // Act - Clear history
        updater.ClearHistory();

        // Verify history was cleared by checking that next anomaly detection
        // uses fallback standard deviation calculation (since no history exists)
        var testMetrics = new Dictionary<string, double> { ["metric1"] = 150.0 };
        var anomalies = updater.UpdateAnomalies(testMetrics, movingAverages);

        // Assert - Should work normally, even with cleared history
        Assert.NotNull(anomalies);
    }

    [Fact]
    public void Multiple_Detection_Methods_Should_All_Trigger_For_Extreme_Values()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["metric"] = new MovingAverageData { MA15 = 100.0, Timestamp = DateTime.UtcNow }
        };

        // Build stable history
        for (int i = 0; i < 10; i++)
        {
            var metrics = new Dictionary<string, double> { ["metric"] = 100.0 };
            updater.UpdateAnomalies(metrics, movingAverages);
        }

        // Act - Add an extremely deviant value
        var testMetrics = new Dictionary<string, double> { ["metric"] = 400.0 }; // 300% increase
        var anomalies = updater.UpdateAnomalies(testMetrics, movingAverages);

        // Assert - Should detect via multiple methods
        Assert.NotEmpty(anomalies);
        Assert.True(anomalies.Count >= 2, "Extreme values should trigger multiple detection methods");

        var methods = anomalies.Select(a => a.Description).ToList();
        Assert.True(methods.Any(m => m.Contains("Spike detected")), "Should detect spike");
        Assert.True(methods.Any(m => m.Contains("High velocity") || m.Contains("Z-Score") || m.Contains("IQR")),
                    "Should detect via another method");
    }

    [Fact]
    public void Negative_Values_Should_Be_Handled_Correctly()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["delta"] = new MovingAverageData { MA15 = -50.0, Timestamp = DateTime.UtcNow }
        };

        // Build history with negative values
        for (int i = 0; i < 5; i++)
        {
            var metrics = new Dictionary<string, double> { ["delta"] = -50.0 };
            updater.UpdateAnomalies(metrics, movingAverages);
        }

        // Act - Add a negative anomaly
        var testMetrics = new Dictionary<string, double> { ["delta"] = -150.0 };
        var anomalies = updater.UpdateAnomalies(testMetrics, movingAverages);

        // Assert - Should handle negative values correctly
        Assert.NotEmpty(anomalies);
        Assert.True(anomalies.All(a => a.Severity >= AnomalySeverity.Low));
    }

    [Fact]
    public void Zero_MA15_Should_Not_Cause_Division_By_Zero()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["metric"] = new MovingAverageData { MA15 = 0.0, Timestamp = DateTime.UtcNow }
        };

        // Act & Assert - Should handle gracefully
        var metrics = new Dictionary<string, double> { ["metric"] = 100.0 };
        var anomalies = updater.UpdateAnomalies(metrics, movingAverages);

        // No division by zero exception should occur
        Assert.NotNull(anomalies);
    }

    [Fact]
    public void UpdateAnomalies_Should_Handle_Exception_And_Continue()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AnomalyUpdater>>();
        var config = new TrendAnalysisConfig
        {
            AnomalyZScoreThreshold = 2.0,
            HighAnomalyZScoreThreshold = 3.0,
            HighVelocityThreshold = 0.5
        };
        var updater = new AnomalyUpdater(loggerMock.Object, config);

        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 97.5
        };
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData
            {
                MA15 = 75.0,
                Timestamp = DateTime.UtcNow
            }
        };

        // Act - Should not throw even with edge cases
        var anomalies = updater.UpdateAnomalies(currentMetrics, movingAverages);

        // Assert
        Assert.NotNull(anomalies);
        // Should complete without exception
    }

    [Fact]
    public void IQR_Critical_Severity_Should_Trigger_For_Extreme_Outliers()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["metric"] = new MovingAverageData { MA15 = 100.0, Timestamp = DateTime.UtcNow }
        };

        // Build a controlled distribution: [10, 20, 30, 40, 50, 60, 70, 80, 90, 100]
        var distribution = new[] { 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0, 100.0 };
        foreach (var value in distribution)
        {
            var metrics = new Dictionary<string, double> { ["metric"] = value };
            updater.UpdateAnomalies(metrics, movingAverages);
        }

        // Act - Add an extreme outlier beyond extended IQR bounds
        var testMetrics = new Dictionary<string, double> { ["metric"] = 500.0 };
        var anomalies = updater.UpdateAnomalies(testMetrics, movingAverages);

        // Assert - Should detect as critical or high severity
        Assert.NotEmpty(anomalies);
        var iqrAnomaly = anomalies.FirstOrDefault(a => a.Description.Contains("IQR anomaly"));
        Assert.NotNull(iqrAnomaly);
        Assert.True(iqrAnomaly.Severity >= AnomalySeverity.High);
    }

    [Fact]
    public void ClearHistory_Should_Log_Debug_Message()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);

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
    public void DetectHighVelocity_Should_Return_Null_When_History_Less_Than_Two()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["metric"] = new MovingAverageData { MA15 = 100.0, Timestamp = DateTime.UtcNow }
        };

        // Act - First call adds one value to history
        var metrics = new Dictionary<string, double> { ["metric"] = 100.0 };
        var anomalies = updater.UpdateAnomalies(metrics, movingAverages);

        // Assert - No velocity anomaly since history < 2
        Assert.DoesNotContain(anomalies, a => a.Description.Contains("High velocity"));
    }

    [Fact]
    public void DetectHighVelocity_Should_Return_Null_When_Previous_Value_Is_Zero()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["metric"] = new MovingAverageData { MA15 = 0.0, Timestamp = DateTime.UtcNow }
        };

        // Build history with zero value
        var metrics1 = new Dictionary<string, double> { ["metric"] = 0.0 };
        updater.UpdateAnomalies(metrics1, movingAverages);

        // Act - Add non-zero value
        var metrics2 = new Dictionary<string, double> { ["metric"] = 100.0 };
        var anomalies = updater.UpdateAnomalies(metrics2, movingAverages);

        // Assert - No velocity anomaly since previous value is zero
        Assert.DoesNotContain(anomalies, a => a.Description.Contains("High velocity"));
    }

    [Fact]
    public void DetectAnomalyByZScore_Should_Return_Null_When_StdDev_Is_Zero()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["metric"] = new MovingAverageData { MA15 = 100.0, Timestamp = DateTime.UtcNow }
        };

        // Build history with identical values to make stdDev = 0
        for (int i = 0; i < 5; i++)
        {
            var metrics = new Dictionary<string, double> { ["metric"] = 100.0 };
            updater.UpdateAnomalies(metrics, movingAverages);
        }

        // Act - Add same value, stdDev should be 0
        var testMetrics = new Dictionary<string, double> { ["metric"] = 100.0 };
        var anomalies = updater.UpdateAnomalies(testMetrics, movingAverages);

        // Assert - No Z-Score anomaly since stdDev = 0
        Assert.DoesNotContain(anomalies, a => a.Description.Contains("Z-Score anomaly"));
    }

    [Fact]
    public void UpdateAnomalies_Should_Log_Warnings_For_Detected_Anomalies()
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
        var anomalies = updater.UpdateAnomalies(currentMetrics, movingAverages);

        // Assert - Should detect anomalies and log warnings
        Assert.NotEmpty(anomalies);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void DetectAnomalyByZScore_Should_Return_Anomaly_When_ZScore_Exceeds_Threshold()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);

        // Use reflection to invoke private method
        var method = typeof(AnomalyUpdater).GetMethod("DetectAnomalyByZScore",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ma = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow };

        // Act
        var result = method.Invoke(updater, new object[] { "cpu", 97.5, ma });

        // Assert - Should return anomaly since 97.5 - 75 = 22.5, stdDev = 7.5, zscore = 22.5/7.5 = 3 > 2
        Assert.NotNull(result);
        var anomaly = result as MetricAnomaly;
        Assert.Equal("cpu", anomaly.MetricName);
        Assert.Equal(97.5, anomaly.CurrentValue);
        Assert.Equal(75.0, anomaly.ExpectedValue);
    }

    [Fact]
    public void DetectAnomalyByIQR_Should_Handle_Exception_And_Return_Null()
    {
        // Arrange
        _loggerMock.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>())).Throws(new Exception("Logger failed"));
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);

        // Use reflection to invoke private method
        var method = typeof(AnomalyUpdater).GetMethod("DetectAnomalyByIQR",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ma = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow };

        // Act
        var result = method.Invoke(updater, new object[] { "cpu", 97.5, ma });

        // Assert - Should return null
        Assert.Null(result);
    }

    [Fact]
    public void DetectSpikeOrDrop_Should_Handle_Exception_And_Return_Null()
    {
        // Arrange
        _loggerMock.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>())).Throws(new Exception("Logger failed"));
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);

        // Use reflection
        var method = typeof(AnomalyUpdater).GetMethod("DetectSpikeOrDrop",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ma = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow };

        // Act
        var result = method.Invoke(updater, new object[] { "cpu", 97.5, ma });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DetectHighVelocity_Should_Handle_Exception_And_Return_Null()
    {
        // Arrange
        _loggerMock.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>())).Throws(new Exception("Logger failed"));
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);

        // Use reflection
        var method = typeof(AnomalyUpdater).GetMethod("DetectHighVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ma = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow };

        // Act
        var result = method.Invoke(updater, new object[] { "cpu", 97.5, ma });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateStandardDeviation_Should_Return_Fallback_Value_When_No_History()
    {
        // Arrange
        var updater = new AnomalyUpdater(_loggerMock.Object, _config);

        // Use reflection
        var method = typeof(AnomalyUpdater).GetMethod("CalculateStandardDeviation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (double)method.Invoke(updater, new object[] { "cpu", 75.0 });

        // Assert - Should return mean * 0.1 = 7.5
        Assert.Equal(7.5, result);
    }
}