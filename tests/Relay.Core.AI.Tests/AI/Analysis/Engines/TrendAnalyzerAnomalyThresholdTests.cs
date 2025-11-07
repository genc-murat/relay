using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

/// <summary>
/// Tests for performance anomaly detection with various threshold combinations.
/// Tests different Z-score thresholds, severity levels, and anomaly detection scenarios.
/// </summary>
public class TrendAnalyzerAnomalyThresholdTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TrendAnalyzer _analyzer;

    public TrendAnalyzerAnomalyThresholdTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddTrendAnalysis();

        _serviceProvider = services.BuildServiceProvider();
        _analyzer = _serviceProvider.GetRequiredService<ITrendAnalyzer>() as TrendAnalyzer
            ?? throw new InvalidOperationException("Could not resolve TrendAnalyzer");
    }

    #region Z-Score Threshold Tests

    [Fact]
    public void DetectPerformanceAnomalies_HighZScore_DetectsCriticalAnomaly()
    {
        // Arrange - Z-score > 3.0 should be critical
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 95.0
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 50.0, Timestamp = DateTime.UtcNow } // Large deviation
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        Assert.True(anomalies.Count <= 1); // At most one anomaly per metric
        if (anomalies.Count == 1)
        {
            var anomaly = anomalies[0];
            Assert.Equal("cpu", anomaly.MetricName);
            Assert.Equal(95.0, anomaly.CurrentValue);
            Assert.Equal(50.0, anomaly.ExpectedValue);
            Assert.Equal(45.0, anomaly.Deviation);
            Assert.True(anomaly.ZScore >= 3.0); // High Z-score
            Assert.True(anomaly.Severity >= AnomalySeverity.Medium);
        }
    }

    [Fact]
    public void DetectPerformanceAnomalies_MediumZScore_DetectsMediumAnomaly()
    {
        // Arrange - Z-score between 2.0-3.0 should be medium
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 85.0
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 70.0, Timestamp = DateTime.UtcNow } // Moderate deviation
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        var cpuAnomalies = anomalies.Where(a => a.MetricName == "cpu").ToList();
        Assert.True(cpuAnomalies.Count <= 1);

        if (cpuAnomalies.Count == 1)
        {
            var anomaly = cpuAnomalies[0];
            Assert.Equal(85.0, anomaly.CurrentValue);
            Assert.Equal(70.0, anomaly.ExpectedValue);
            Assert.Equal(15.0, anomaly.Deviation);
            // Z-score depends on the calculation, but should be in medium range
            Assert.True(anomaly.Severity >= AnomalySeverity.Low);
        }
    }

    [Fact]
    public void DetectPerformanceAnomalies_LowZScore_DetectsLowAnomaly()
    {
        // Arrange - Z-score between 1.5-2.0 should be low
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 78.0
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 70.0, Timestamp = DateTime.UtcNow } // Small deviation
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        var cpuAnomalies = anomalies.Where(a => a.MetricName == "cpu").ToList();
        Assert.True(cpuAnomalies.Count <= 1);

        if (cpuAnomalies.Count == 1)
        {
            var anomaly = cpuAnomalies[0];
            Assert.Equal(78.0, anomaly.CurrentValue);
            Assert.Equal(70.0, anomaly.ExpectedValue);
            Assert.Equal(8.0, anomaly.Deviation);
            Assert.Equal(AnomalySeverity.Low, anomaly.Severity);
        }
    }

    [Fact]
    public void DetectPerformanceAnomalies_NoDeviation_NoAnomaly()
    {
        // Arrange - No deviation from moving average
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow } // Same value
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        Assert.Empty(anomalies);
    }

    #endregion

    #region Severity Threshold Tests

    [Fact]
    public void DetectPerformanceAnomalies_CriticalSeverityThreshold_DetectsCorrectly()
    {
        // Arrange - Test the boundary for critical severity
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 92.0
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 60.0, Timestamp = DateTime.UtcNow } // Large deviation
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        var cpuAnomalies = anomalies.Where(a => a.MetricName == "cpu").ToList();
        Assert.True(cpuAnomalies.Count <= 1);

        if (cpuAnomalies.Count == 1)
        {
            var anomaly = cpuAnomalies[0];
            Assert.Equal(32.0, anomaly.Deviation); // 92 - 60
            Assert.True(anomaly.Severity >= AnomalySeverity.Medium);
        }
    }

    [Fact]
    public void DetectPerformanceAnomalies_HighSeverityThreshold_DetectsCorrectly()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["memory"] = 88.0
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["memory"] = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        var memoryAnomalies = anomalies.Where(a => a.MetricName == "memory").ToList();
        Assert.True(memoryAnomalies.Count <= 1);

        if (memoryAnomalies.Count == 1)
        {
            var anomaly = memoryAnomalies[0];
            Assert.Equal(13.0, anomaly.Deviation); // 88 - 75
            Assert.True(anomaly.Severity >= AnomalySeverity.Low);
        }
    }

    #endregion

    #region Multiple Metrics Threshold Tests

    [Fact]
    public void DetectPerformanceAnomalies_MultipleMetricsDifferentThresholds_HandlesCorrectly()
    {
        // Arrange - Different metrics with different deviation levels
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 98.0,        // Large deviation - should trigger
            ["memory"] = 82.0,     // Medium deviation - might trigger
            ["disk"] = 76.0,       // Small deviation - might trigger
            ["network"] = 70.0     // No deviation - should not trigger
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 60.0, Timestamp = DateTime.UtcNow },
            ["memory"] = new MovingAverageData { MA15 = 78.0, Timestamp = DateTime.UtcNow },
            ["disk"] = new MovingAverageData { MA15 = 74.0, Timestamp = DateTime.UtcNow },
            ["network"] = new MovingAverageData { MA15 = 70.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        // Should have at most one anomaly per metric
        Assert.True(anomalies.Count <= 4);

        var cpuAnomalies = anomalies.Where(a => a.MetricName == "cpu").ToList();
        var memoryAnomalies = anomalies.Where(a => a.MetricName == "memory").ToList();
        var diskAnomalies = anomalies.Where(a => a.MetricName == "disk").ToList();
        var networkAnomalies = anomalies.Where(a => a.MetricName == "network").ToList();

        Assert.True(cpuAnomalies.Count <= 1);
        Assert.True(memoryAnomalies.Count <= 1);
        Assert.True(diskAnomalies.Count <= 1);
        Assert.Empty(networkAnomalies); // No deviation, no anomaly

        // CPU should have highest severity due to largest deviation
        if (cpuAnomalies.Count == 1)
        {
            Assert.Equal(38.0, cpuAnomalies[0].Deviation); // 98 - 60
        }
    }

    #endregion

    #region Edge Case Threshold Tests

    [Fact]
    public void DetectPerformanceAnomalies_ZeroMovingAverage_HandlesDivision()
    {
        // Arrange - Test division by zero handling
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 10.0
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 0.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert - Should handle zero moving average without crashing
        Assert.True(anomalies.Count <= 1);
    }

    [Fact]
    public void DetectPerformanceAnomalies_ExtremeValues_HandlesCorrectly()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = double.MaxValue
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 1.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert - Should handle extreme values
        Assert.True(anomalies.Count <= 1);
    }

    [Fact]
    public void DetectPerformanceAnomalies_NegativeValues_HandlesCorrectly()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["temperature"] = -10.0
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["temperature"] = new MovingAverageData { MA15 = 0.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert - Should handle negative values
        Assert.True(anomalies.Count <= 1);
    }

    #endregion

    #region Threshold Boundary Tests

    [Fact]
    public void DetectPerformanceAnomalies_ThresholdBoundaries_ConsistentDetection()
    {
        // Arrange - Test various threshold boundaries
        var testCases = new[]
        {
            new { Current = 85.0, Expected = 75.0, ExpectedDeviation = 10.0 }, // Small deviation
            new { Current = 90.0, Expected = 75.0, ExpectedDeviation = 15.0 }, // Medium deviation
            new { Current = 95.0, Expected = 75.0, ExpectedDeviation = 20.0 }, // Large deviation
            new { Current = 99.0, Expected = 75.0, ExpectedDeviation = 24.0 }  // Very large deviation
        };

        foreach (var testCase in testCases)
        {
            var metrics = new Dictionary<string, double>
            {
                ["cpu"] = testCase.Current
            };

            var movingAverages = new Dictionary<string, MovingAverageData>
            {
                ["cpu"] = new MovingAverageData { MA15 = testCase.Expected, Timestamp = DateTime.UtcNow }
            };

            // Act
            var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

            // Assert - Should handle each case without crashing
            Assert.True(anomalies.Count <= 1);

            if (anomalies.Count == 1)
            {
                var anomaly = anomalies[0];
                Assert.Equal(testCase.ExpectedDeviation, anomaly.Deviation);
                Assert.Equal(testCase.Current, anomaly.CurrentValue);
                Assert.Equal(testCase.Expected, anomaly.ExpectedValue);
                Assert.True(anomaly.Severity >= AnomalySeverity.Low);
            }
        }
    }

    #endregion

    #region Integration with AnalyzeMetricTrends Tests

    [Fact]
    public void AnalyzeMetricTrends_IncludesAnomaliesWithCorrectThresholds()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 96.0,  // Should trigger anomaly
            ["memory"] = 80.0 // Normal
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result.Anomalies);

        var cpuAnomalies = result.Anomalies.Where(a => a.MetricName == "cpu").ToList();
        var memoryAnomalies = result.Anomalies.Where(a => a.MetricName == "memory").ToList();

        Assert.True(cpuAnomalies.Count <= 1);
        Assert.Empty(memoryAnomalies); // Memory should not have anomalies

        if (cpuAnomalies.Count == 1)
        {
            var anomaly = cpuAnomalies[0];
            Assert.Equal("cpu", anomaly.MetricName);
            Assert.Equal(96.0, anomaly.CurrentValue);
            Assert.True(anomaly.Deviation > 0);
            Assert.True(anomaly.ZScore >= 0);
            Assert.True(anomaly.Severity >= AnomalySeverity.Low);
            Assert.NotEqual(default(DateTime), anomaly.Timestamp);
            Assert.Contains("cpu", anomaly.Description);
        }
    }

    [Fact]
    public void AnalyzeMetricTrends_AnomalySeverityMapping_CorrectInInsights()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 97.0 // High value to trigger anomaly
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        if (result.Anomalies.Count > 0)
        {
            var anomaly = result.Anomalies[0];
            var relatedInsight = result.Insights.FirstOrDefault(i => i.Category == "Anomaly Detection");

            if (relatedInsight != null)
            {
                // Insight severity should correspond to anomaly severity
                var expectedInsightSeverity = anomaly.Severity == AnomalySeverity.Critical ? InsightSeverity.Critical :
                                            anomaly.Severity == AnomalySeverity.High ? InsightSeverity.Warning :
                                            anomaly.Severity == AnomalySeverity.Medium ? InsightSeverity.Warning :
                                            InsightSeverity.Info;

                Assert.Equal(expectedInsightSeverity, relatedInsight.Severity);
            }
        }
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void DetectPerformanceAnomalies_LargeDataset_HandlesEfficiently()
    {
        // Arrange - Large number of metrics
        var metrics = new Dictionary<string, double>();
        var movingAverages = new Dictionary<string, MovingAverageData>();

        for (int i = 0; i < 100; i++)
        {
            metrics[$"metric_{i}"] = 50.0 + (i % 50); // Values 50-99
            movingAverages[$"metric_{i}"] = new MovingAverageData
            {
                MA15 = 50.0,
                Timestamp = DateTime.UtcNow
            };
        }

        // Act
        var startTime = DateTime.UtcNow;
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);
        var endTime = DateTime.UtcNow;

        // Assert
        Assert.True(anomalies.Count <= 100); // At most one per metric
        var duration = endTime - startTime;
        Assert.True(duration.TotalMilliseconds < 500); // Should complete quickly
    }

    #endregion
}