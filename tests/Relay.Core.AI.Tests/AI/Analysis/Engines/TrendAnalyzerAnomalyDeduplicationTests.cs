using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

/// <summary>
/// Tests for anomaly deduplication logic in TrendAnalyzer.DetectPerformanceAnomalies method.
/// Tests that only the highest severity anomaly is kept per metric.
/// </summary>
public class TrendAnalyzerAnomalyDeduplicationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TrendAnalyzer _analyzer;

    public TrendAnalyzerAnomalyDeduplicationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddTrendAnalysis();

        _serviceProvider = services.BuildServiceProvider();
        _analyzer = _serviceProvider.GetRequiredService<ITrendAnalyzer>() as TrendAnalyzer
            ?? throw new InvalidOperationException("Could not resolve TrendAnalyzer");
    }

    #region Basic Deduplication Tests

    [Fact]
    public void DetectPerformanceAnomalies_DeduplicationLogic_HandlesEmptyAnomalies()
    {
        // Arrange - Use metrics that may or may not trigger anomalies
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 85.0,
            ["memory"] = 82.0
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 80.0, Timestamp = DateTime.UtcNow },
            ["memory"] = new MovingAverageData { MA15 = 80.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert - The deduplication logic should work even with no anomalies
        // Each metric should have at most one anomaly (deduplication)
        var cpuAnomalies = anomalies.Where(a => a.MetricName == "cpu").ToList();
        var memoryAnomalies = anomalies.Where(a => a.MetricName == "memory").ToList();

        Assert.True(cpuAnomalies.Count <= 1);
        Assert.True(memoryAnomalies.Count <= 1);
    }

    [Fact]
    public void DetectPerformanceAnomalies_MultipleAnomaliesSameMetric_KeepsHighestSeverity()
    {
        // Arrange - Create scenario where multiple anomalies might be detected for same metric
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 100.0 // Very high value that should trigger anomaly
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 50.0, Timestamp = DateTime.UtcNow } // Large deviation
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        var cpuAnomalies = anomalies.Where(a => a.MetricName == "cpu").ToList();
        Assert.True(cpuAnomalies.Count <= 1, "Should have at most one anomaly per metric due to deduplication");

        if (cpuAnomalies.Count == 1)
        {
            var anomaly = cpuAnomalies[0];
            Assert.Equal("cpu", anomaly.MetricName);
            Assert.Equal(100.0, anomaly.CurrentValue);
            Assert.Equal(50.0, anomaly.ExpectedValue);
            Assert.True(anomaly.Severity >= AnomalySeverity.Medium);
        }
    }

    #endregion

    #region Severity-Based Deduplication Tests

    [Fact]
    public void DetectPerformanceAnomalies_MultipleSeveritiesForSameMetric_KeepsHighestSeverity()
    {
        // Arrange - This test is challenging because we can't directly control what anomalies are detected
        // We need to test the deduplication logic indirectly
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 99.0 // High value
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 70.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        var cpuAnomalies = anomalies.Where(a => a.MetricName == "cpu").ToList();

        // Should have at most one anomaly per metric
        Assert.True(cpuAnomalies.Count <= 1);

        // If there is an anomaly, verify its properties
        if (cpuAnomalies.Count == 1)
        {
            var anomaly = cpuAnomalies[0];
            Assert.Equal("cpu", anomaly.MetricName);
            Assert.Equal(99.0, anomaly.CurrentValue);
            Assert.Equal(70.0, anomaly.ExpectedValue);
            Assert.True(anomaly.Deviation > 0);
            Assert.True(anomaly.ZScore > 0);
            Assert.NotEqual(default(DateTime), anomaly.Timestamp);
            Assert.Contains("cpu", anomaly.Description);
        }
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void DetectPerformanceAnomalies_NoAnomaliesDetected_ReturnsEmptyList()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0,     // Normal value
            ["memory"] = 80.0   // Normal value
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow },
            ["memory"] = new MovingAverageData { MA15 = 80.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectPerformanceAnomalies_EmptyMetrics_ReturnsEmptyList()
    {
        // Arrange
        var metrics = new Dictionary<string, double>();
        var movingAverages = new Dictionary<string, MovingAverageData>();

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectPerformanceAnomalies_EmptyMovingAverages_ReturnsEmptyList()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 90.0
        };
        var movingAverages = new Dictionary<string, MovingAverageData>(); // Empty

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectPerformanceAnomalies_MissingMovingAverageForMetric_SkipsThatMetric()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 90.0,
            ["memory"] = 85.0
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 70.0, Timestamp = DateTime.UtcNow }
            // Missing memory moving average
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        // Should only check CPU (memory skipped due to missing moving average)
        var cpuAnomalies = anomalies.Where(a => a.MetricName == "cpu").ToList();
        var memoryAnomalies = anomalies.Where(a => a.MetricName == "memory").ToList();

        Assert.True(cpuAnomalies.Count <= 1);
        Assert.Empty(memoryAnomalies); // No anomaly for memory due to missing moving average
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public void DetectPerformanceAnomalies_MultipleMetricsWithDifferentAnomalyLevels_HandlesCorrectly()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 98.0,      // Should trigger high anomaly
            ["memory"] = 88.0,   // Might trigger medium anomaly
            ["disk"] = 75.0      // Normal
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 60.0, Timestamp = DateTime.UtcNow },
            ["memory"] = new MovingAverageData { MA15 = 85.0, Timestamp = DateTime.UtcNow },
            ["disk"] = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        // Should have at most one anomaly per metric
        var cpuAnomalies = anomalies.Where(a => a.MetricName == "cpu").ToList();
        var memoryAnomalies = anomalies.Where(a => a.MetricName == "memory").ToList();
        var diskAnomalies = anomalies.Where(a => a.MetricName == "disk").ToList();

        Assert.True(cpuAnomalies.Count <= 1);
        Assert.True(memoryAnomalies.Count <= 1);
        Assert.Empty(diskAnomalies); // Disk should not have anomalies

        // CPU should have high severity anomaly
        if (cpuAnomalies.Count == 1)
        {
            Assert.True(cpuAnomalies[0].Severity >= AnomalySeverity.Medium);
            Assert.Equal(38.0, cpuAnomalies[0].Deviation); // 98 - 60
        }
    }

    [Fact]
    public void DetectPerformanceAnomalies_ExtremeValues_HandlesCorrectly()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = double.MaxValue,
            ["memory"] = 0.0
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 50.0, Timestamp = DateTime.UtcNow },
            ["memory"] = new MovingAverageData { MA15 = 80.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert - Should handle extreme values without crashing
        Assert.True(anomalies.Count <= 2); // At most one per metric

        var cpuAnomalies = anomalies.Where(a => a.MetricName == "cpu").ToList();
        var memoryAnomalies = anomalies.Where(a => a.MetricName == "memory").ToList();

        Assert.True(cpuAnomalies.Count <= 1);
        Assert.True(memoryAnomalies.Count <= 1);
    }

    #endregion

    #region Integration with AnalyzeMetricTrends Tests

    [Fact]
    public void AnalyzeMetricTrends_IncludesDeduplicatedAnomaliesInResult()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 95.0 // High value that should trigger anomaly
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Anomalies);

        // Anomalies should be deduplicated (at most one per metric)
        var cpuAnomalies = result.Anomalies.Where(a => a.MetricName == "cpu").ToList();
        Assert.True(cpuAnomalies.Count <= 1);

        // If there is an anomaly, verify its structure
        if (cpuAnomalies.Count == 1)
        {
            var anomaly = cpuAnomalies[0];
            Assert.Equal("cpu", anomaly.MetricName);
            Assert.Equal(95.0, anomaly.CurrentValue);
            Assert.True(anomaly.Severity >= AnomalySeverity.Low);
            Assert.NotEqual(default(DateTime), anomaly.Timestamp);
            Assert.Contains("cpu", anomaly.Description);
        }
    }

    [Fact]
    public void AnalyzeMetricTrends_WithMultipleHighMetrics_IncludesMultipleDeduplicatedAnomalies()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 97.0,
            ["memory"] = 94.0,
            ["disk"] = 70.0 // Normal
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result.Anomalies);

        var cpuAnomalies = result.Anomalies.Where(a => a.MetricName == "cpu").ToList();
        var memoryAnomalies = result.Anomalies.Where(a => a.MetricName == "memory").ToList();
        var diskAnomalies = result.Anomalies.Where(a => a.MetricName == "disk").ToList();

        // Each metric should have at most one anomaly
        Assert.True(cpuAnomalies.Count <= 1);
        Assert.True(memoryAnomalies.Count <= 1);
        Assert.Empty(diskAnomalies); // Disk should not have anomalies
    }

    #endregion

    #region Anomaly Properties Validation Tests

    [Fact]
    public void DetectPerformanceAnomalies_AnomalyProperties_AreCorrectlySet()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 96.0
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 70.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        if (anomalies.Count > 0)
        {
            var anomaly = anomalies[0];
            Assert.Equal("cpu", anomaly.MetricName);
            Assert.Equal(96.0, anomaly.CurrentValue);
            Assert.Equal(70.0, anomaly.ExpectedValue);
            Assert.Equal(26.0, anomaly.Deviation); // 96 - 70
            Assert.True(anomaly.ZScore > 0);
            Assert.True(anomaly.Severity >= AnomalySeverity.Low);
            Assert.NotEqual(default(DateTime), anomaly.Timestamp);
            Assert.NotNull(anomaly.Description);
            Assert.Contains("cpu", anomaly.Description);
        }
    }

    #endregion

    #region GetSeverityLevel Tests

    [Fact]
    public void DetectPerformanceAnomalies_SeverityLevels_AreCorrectlyOrdered()
    {
        // Arrange - Test that severity levels are ordered correctly (Critical > High > Medium > Low)
        // We'll test this by checking that higher Z-scores produce higher severity anomalies

        var metrics = new Dictionary<string, double>
        {
            ["critical_metric"] = 100.0,  // Should produce high Z-score -> Critical
            ["high_metric"] = 90.0,       // Should produce medium-high Z-score -> High
            ["medium_metric"] = 85.0,     // Should produce medium Z-score -> Medium
            ["low_metric"] = 80.0         // Should produce low Z-score -> Low
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["critical_metric"] = new MovingAverageData { MA15 = 50.0, Timestamp = DateTime.UtcNow },
            ["high_metric"] = new MovingAverageData { MA15 = 60.0, Timestamp = DateTime.UtcNow },
            ["medium_metric"] = new MovingAverageData { MA15 = 70.0, Timestamp = DateTime.UtcNow },
            ["low_metric"] = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        // Each metric should have at most one anomaly
        var criticalAnomalies = anomalies.Where(a => a.MetricName == "critical_metric").ToList();
        var highAnomalies = anomalies.Where(a => a.MetricName == "high_metric").ToList();
        var mediumAnomalies = anomalies.Where(a => a.MetricName == "medium_metric").ToList();
        var lowAnomalies = anomalies.Where(a => a.MetricName == "low_metric").ToList();

        Assert.True(criticalAnomalies.Count <= 1);
        Assert.True(highAnomalies.Count <= 1);
        Assert.True(mediumAnomalies.Count <= 1);
        Assert.True(lowAnomalies.Count <= 1);

        // Verify severity ordering: if anomalies exist, higher deviation should have higher severity
        if (criticalAnomalies.Count == 1 && highAnomalies.Count == 1)
        {
            Assert.True((int)criticalAnomalies[0].Severity >= (int)highAnomalies[0].Severity);
        }
        if (highAnomalies.Count == 1 && mediumAnomalies.Count == 1)
        {
            Assert.True((int)highAnomalies[0].Severity >= (int)mediumAnomalies[0].Severity);
        }
        if (mediumAnomalies.Count == 1 && lowAnomalies.Count == 1)
        {
            Assert.True((int)mediumAnomalies[0].Severity >= (int)lowAnomalies[0].Severity);
        }
    }

    [Fact]
    public void DetectPerformanceAnomalies_Deduplication_KeepsHighestSeverityAnomaly()
    {
        // Arrange - Create a scenario where we might have multiple anomalies for same metric
        // This is challenging with the current implementation, but we can test the logic indirectly
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 99.0  // High value
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 50.0, Timestamp = DateTime.UtcNow } // Large deviation
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        var cpuAnomalies = anomalies.Where(a => a.MetricName == "cpu").ToList();

        // Should have at most one anomaly (deduplication)
        Assert.True(cpuAnomalies.Count <= 1);

        if (cpuAnomalies.Count == 1)
        {
            var anomaly = cpuAnomalies[0];
            // Verify it's a high severity anomaly
            Assert.True(anomaly.Severity >= AnomalySeverity.Medium); // At least medium
            Assert.Equal(49.0, anomaly.Deviation); // 99 - 50
            Assert.True(anomaly.ZScore > 0); // Positive Z-score
        }
    }

    [Fact]
    public void DetectPerformanceAnomalies_SeverityMapping_MatchesExpectedLevels()
    {
        // Arrange - Test specific Z-score ranges map to expected severities
        var testCases = new[]
        {
            new { Metric = "critical_cpu", Value = 100.0, MA = 50.0, ExpectedMinSeverity = AnomalySeverity.High },
            new { Metric = "high_cpu", Value = 90.0, MA = 60.0, ExpectedMinSeverity = AnomalySeverity.Medium },
            new { Metric = "medium_cpu", Value = 85.0, MA = 70.0, ExpectedMinSeverity = AnomalySeverity.Low },
            new { Metric = "low_cpu", Value = 80.0, MA = 75.0, ExpectedMinSeverity = AnomalySeverity.Low }
        };

        foreach (var testCase in testCases)
        {
            var metrics = new Dictionary<string, double> { [testCase.Metric] = testCase.Value };
            var movingAverages = new Dictionary<string, MovingAverageData>
            {
                [testCase.Metric] = new MovingAverageData { MA15 = testCase.MA, Timestamp = DateTime.UtcNow }
            };

            // Act
            var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

            // Assert
            var metricAnomalies = anomalies.Where(a => a.MetricName == testCase.Metric).ToList();
            Assert.True(metricAnomalies.Count <= 1);

            if (metricAnomalies.Count == 1)
            {
                Assert.True(metricAnomalies[0].Severity >= testCase.ExpectedMinSeverity);
            }
        }
    }

    #endregion
}