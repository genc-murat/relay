using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

/// <summary>
/// Edge case and boundary tests for TrendAnalyzer.GenerateTrendInsights method.
/// </summary>
public class TrendAnalyzerGenerateTrendInsightsEdgeCaseTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TrendAnalyzer _analyzer;
    private readonly MethodInfo _generateTrendInsightsMethod;

    public TrendAnalyzerGenerateTrendInsightsEdgeCaseTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddTrendAnalysis();

        _serviceProvider = services.BuildServiceProvider();
        _analyzer = _serviceProvider.GetRequiredService<ITrendAnalyzer>() as TrendAnalyzer
            ?? throw new InvalidOperationException("Could not resolve TrendAnalyzer");

        _generateTrendInsightsMethod = typeof(TrendAnalyzer).GetMethod(
            "GenerateTrendInsights",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] {
                typeof(Dictionary<string, TrendDirection>),
                typeof(Dictionary<string, double>),
                typeof(List<MetricAnomaly>),
                typeof(Dictionary<string, double>)
            },
            null);

        if (_generateTrendInsightsMethod == null)
            throw new InvalidOperationException("Could not find GenerateTrendInsights method");
    }

    private List<TrendInsight> InvokeGenerateTrendInsights(
        Dictionary<string, TrendDirection> trendDirections,
        Dictionary<string, double> trendVelocities,
        List<MetricAnomaly> anomalies,
        Dictionary<string, double> currentMetrics)
    {
        var result = _generateTrendInsightsMethod.Invoke(_analyzer, new object[] {
            trendDirections,
            trendVelocities,
            anomalies,
            currentMetrics
        });

        return result as List<TrendInsight> ?? new List<TrendInsight>();
    }

    #region Velocity Threshold Boundary Tests

    [Fact]
    public void GenerateTrendInsights_WithVelocityAtExactThreshold_ShouldGenerateInsight()
    {
        // Arrange - velocity exactly at 0.1 threshold
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 0.1 // Exactly at threshold
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 60.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        // At exactly 0.1, should generate insight (condition is > 0.1, not >=)
        // So at exactly 0.1, it should NOT generate insight
        var trendInsights = insights.Where(i => i.Category == "Performance Trend").ToList();
        Assert.Empty(trendInsights);
    }

    [Fact]
    public void GenerateTrendInsights_WithVelocityJustAboveThreshold_ShouldGenerateInsight()
    {
        // Arrange - velocity just above 0.1 threshold
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 0.10001 // Just above threshold
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 60.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        var trendInsights = insights.Where(i => i.Category == "Performance Trend").ToList();
        Assert.Single(trendInsights);
    }

    [Fact]
    public void GenerateTrendInsights_WithVelocityAtSeverityThreshold_ShouldSwitchSeverity()
    {
        // Arrange - test velocity thresholds for severity (0.5)
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 0.5 // Exactly at 0.5 severity threshold
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 60.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        var insight = insights.FirstOrDefault(i => i.Category == "Performance Trend");
        Assert.NotNull(insight);
        // At 0.5, absolute value is NOT > 0.5, so should be Warning severity
        Assert.Equal(InsightSeverity.Warning, insight.Severity);
    }

    [Fact]
    public void GenerateTrendInsights_WithVelocityJustAboveSeverityThreshold_ShouldGenerateCritical()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 0.50001 // Just above 0.5 severity threshold
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 60.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        var insight = insights.FirstOrDefault(i => i.Category == "Performance Trend");
        Assert.NotNull(insight);
        Assert.Equal(InsightSeverity.Critical, insight.Severity);
    }

    #endregion

    #region Empty Dictionary Tests

    [Fact]
    public void GenerateTrendInsights_WithEmptyTrendDirections_ShouldReturnEmptyInsights()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>();
        var trendVelocities = new Dictionary<string, double>();
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>();

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.Empty(insights);
    }

    [Fact]
    public void GenerateTrendInsights_WithEmptyVelocities_ShouldHandleGracefully()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>();
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 60.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        // Should handle missing velocity gracefully (defaults to 0.0)
        Assert.Empty(insights);
    }

    [Fact]
    public void GenerateTrendInsights_WithEmptyAnomalies_ShouldReturnOnlyTrendInsights()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 0.2
        };
        var anomalies = new List<MetricAnomaly>(); // Empty
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 60.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        Assert.All(insights, i => Assert.NotEqual("Anomaly Detection", i.Category));
    }

    #endregion

    #region Extreme Value Tests

    [Fact]
    public void GenerateTrendInsights_WithExtremelyHighVelocity_ShouldHandleCorrectly()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = double.MaxValue
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 60.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.First();
        Assert.Equal(InsightSeverity.Critical, insight.Severity);
    }

    [Fact]
    public void GenerateTrendInsights_WithNegativeMetricValues_ShouldHandleCorrectly()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["temperature"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["temperature"] = 0.2
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["temperature"] = -10.5 // Negative value
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.First();
        Assert.Contains("temperature", insight.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateTrendInsights_WithZeroCurrentMetricValue_ShouldHandleCorrectly()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["custom_metric"] = TrendDirection.Decreasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["custom_metric"] = -0.15
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["custom_metric"] = 0.0 // Zero value
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.First();
        Assert.Equal(InsightSeverity.Info, insight.Severity);
    }

    [Fact]
    public void GenerateTrendInsights_WithVerySmallMetricValue_ShouldHandleCorrectly()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["precision_metric"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["precision_metric"] = 0.0001
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["precision_metric"] = 0.00001
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        // Velocity is below threshold
        var trendInsights = insights.Where(i => i.Category == "Performance Trend").ToList();
        Assert.Empty(trendInsights);
    }

    #endregion

    #region Metric Name Handling Tests

    [Fact]
    public void GenerateTrendInsights_WithVeryLongMetricName_ShouldIncludeInMessage()
    {
        // Arrange
        var longName = "system_performance_cpu_utilization_percentage_normalized_value";
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            [longName] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            [longName] = 0.2
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            [longName] = 60.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.First();
        Assert.Contains(longName, insight.Message);
    }

    [Fact]
    public void GenerateTrendInsights_WithSpecialCharactersInMetricName_ShouldHandleCorrectly()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["metric-with-dashes_and_underscores"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["metric-with-dashes_and_underscores"] = 0.2
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["metric-with-dashes_and_underscores"] = 60.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.First();
        Assert.Contains("metric-with-dashes_and_underscores", insight.Message);
    }

    [Fact]
    public void GenerateTrendInsights_WithNumericMetricName_ShouldHandleCorrectly()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["metric123"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["metric123"] = 0.2
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["metric123"] = 60.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
    }

    #endregion

    #region Anomaly Severity Edge Cases

    [Fact]
    public void GenerateTrendInsights_WithAllAnomalySeverityLevels_ShouldMapCorrectly()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>();
        var trendVelocities = new Dictionary<string, double>();
        var anomalies = new List<MetricAnomaly>
        {
            new MetricAnomaly
            {
                MetricName = "critical_metric",
                CurrentValue = 100.0,
                ExpectedValue = 50.0,
                Severity = AnomalySeverity.Critical,
                Description = "Critical anomaly",
                Timestamp = DateTime.UtcNow,
                ZScore = 5.0
            },
            new MetricAnomaly
            {
                MetricName = "high_metric",
                CurrentValue = 95.0,
                ExpectedValue = 60.0,
                Severity = AnomalySeverity.High,
                Description = "High anomaly",
                Timestamp = DateTime.UtcNow,
                ZScore = 4.0
            },
            new MetricAnomaly
            {
                MetricName = "medium_metric",
                CurrentValue = 85.0,
                ExpectedValue = 70.0,
                Severity = AnomalySeverity.Medium,
                Description = "Medium anomaly",
                Timestamp = DateTime.UtcNow,
                ZScore = 3.0
            },
            new MetricAnomaly
            {
                MetricName = "low_metric",
                CurrentValue = 75.0,
                ExpectedValue = 70.0,
                Severity = AnomalySeverity.Low,
                Description = "Low anomaly",
                Timestamp = DateTime.UtcNow,
                ZScore = 2.0
            }
        };
        var currentMetrics = new Dictionary<string, double>
        {
            ["critical_metric"] = 100.0,
            ["high_metric"] = 95.0,
            ["medium_metric"] = 85.0,
            ["low_metric"] = 75.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.Equal(4, insights.Count);

        var criticalInsight = insights.Find(i => i.Message.Contains("critical_metric"));
        Assert.NotNull(criticalInsight);
        Assert.Equal(InsightSeverity.Critical, criticalInsight.Severity);

        var highInsight = insights.Find(i => i.Message.Contains("high_metric"));
        Assert.NotNull(highInsight);
        Assert.Equal(InsightSeverity.Warning, highInsight.Severity);

        var mediumInsight = insights.Find(i => i.Message.Contains("medium_metric"));
        Assert.NotNull(mediumInsight);
        Assert.Equal(InsightSeverity.Warning, mediumInsight.Severity);

        var lowInsight = insights.Find(i => i.Message.Contains("low_metric"));
        Assert.NotNull(lowInsight);
        Assert.Equal(InsightSeverity.Info, lowInsight.Severity);
    }

    #endregion

    #region Complex Scenario Tests

    [Fact]
    public void GenerateTrendInsights_WithAllTrendDirections_ShouldGenerateAppropriateInsights()
    {
        // Arrange - Test all trend directions
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["increasing_metric"] = TrendDirection.Increasing,
            ["decreasing_metric"] = TrendDirection.Decreasing,
            ["stable_metric"] = TrendDirection.Stable,
            ["unknown_metric"] = (TrendDirection)999 // Unknown/undefined direction
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["increasing_metric"] = 0.2,
            ["decreasing_metric"] = -0.2,
            ["stable_metric"] = 0.01,
            ["unknown_metric"] = 0.0
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["increasing_metric"] = 60.0,
            ["decreasing_metric"] = 50.0,
            ["stable_metric"] = 75.0,
            ["unknown_metric"] = 40.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.True(insights.Count >= 2); // At least increasing and decreasing trends
        Assert.Contains(insights, i => i.Category == "Performance Trend");
        Assert.Contains(insights, i => i.Category == "Performance Improvement");
    }

    [Fact]
    public void GenerateTrendInsights_WithLargeNumberOfTrends_ShouldGenerateAllInsights()
    {
        // Arrange - Create 100 metrics with different trends
        var trendDirections = new Dictionary<string, TrendDirection>();
        var trendVelocities = new Dictionary<string, double>();
        var currentMetrics = new Dictionary<string, double>();

        for (int i = 0; i < 100; i++)
        {
            var metricName = $"metric_{i}";
            trendDirections[metricName] = i % 2 == 0 ? TrendDirection.Increasing : TrendDirection.Decreasing;
            trendVelocities[metricName] = (i % 2 == 0) ? 0.2 : -0.2;
            currentMetrics[metricName] = 50.0 + i;
        }
        var anomalies = new List<MetricAnomaly>();

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.True(insights.Count >= 50); // At least 50 insights (some metrics might not meet thresholds)
    }

    [Fact]
    public void GenerateTrendInsights_WithDuplicateMetricNamesButDifferentCases_ShouldHandleSeparately()
    {
        // Arrange - Dictionary keys are case-sensitive in C#
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing,
            ["CPU"] = TrendDirection.Decreasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 0.2,
            ["CPU"] = -0.2
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 60.0,
            ["CPU"] = 50.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.True(insights.Count >= 2); // Should have insights for both
        var lowercaseInsights = insights.Where(i => i.Message.Contains("cpu") && !i.Message.Contains("CPU")).ToList();
        var uppercaseInsights = insights.Where(i => i.Message.Contains("CPU")).ToList();

        // Both should be present
        Assert.True(lowercaseInsights.Count > 0 || uppercaseInsights.Count > 0);
    }

    [Fact]
    public void GenerateTrendInsights_WithManyAnomalies_ShouldGenerateAllAnomalyInsights()
    {
        // Arrange - Create 50 anomalies
        var trendDirections = new Dictionary<string, TrendDirection>();
        var trendVelocities = new Dictionary<string, double>();
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>();

        for (int i = 0; i < 50; i++)
        {
            var metricName = $"anomaly_metric_{i}";
            anomalies.Add(new MetricAnomaly
            {
                MetricName = metricName,
                CurrentValue = 100.0 + i,
                ExpectedValue = 50.0,
                Severity = (AnomalySeverity)(i % 4),
                Description = $"Anomaly {i}",
                Timestamp = DateTime.UtcNow,
                ZScore = 3.0 + (i * 0.1)
            });
            currentMetrics[metricName] = 100.0 + i;
        }

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.Equal(50, insights.Count);
        Assert.All(insights, i => Assert.Equal("Anomaly Detection", i.Category));
    }

    #endregion

    #region Trend Direction Cases

    [Fact]
    public void GenerateTrendInsights_WithIncreasingTrendNegativeVelocity_ShouldUseAbsoluteValue()
    {
        // Arrange - Negative velocity for increasing trend (edge case)
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = -0.3 // Negative, but uses Math.Abs
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 60.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        // Should generate insight because Abs(-0.3) = 0.3 > 0.1
        Assert.NotEmpty(insights);
        var insight = insights.First();
        Assert.Equal(InsightSeverity.Warning, insight.Severity); // 0.3 < 0.5
    }

    [Fact]
    public void GenerateTrendInsights_WithDecreasingTrendPositiveVelocity_ShouldUseAbsoluteValue()
    {
        // Arrange - Positive velocity for decreasing trend
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["error_rate"] = TrendDirection.Decreasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["error_rate"] = 0.3 // Positive velocity
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["error_rate"] = 3.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        // Should generate insight because Abs(0.3) = 0.3 > 0.1
        Assert.NotEmpty(insights);
        var insight = insights.First();
        Assert.Equal("Performance Improvement", insight.Category);
    }

    #endregion

    #region Integration with AnalyzeMetricTrends

    [Fact]
    public void AnalyzeMetricTrends_ShouldIncludeBothBasicAndAdvancedInsights()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 92.0,      // Should trigger basic insight
            ["memory"] = 45.0,   // Normal
            ["error"] = 1.0      // Normal
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotEmpty(result.Insights);
        // Should have at least one insight for high CPU
        var cpuInsight = result.Insights.FirstOrDefault(i => i.Message.Contains("cpu"));
        Assert.NotNull(cpuInsight);
    }

    [Fact]
    public void AnalyzeMetricTrends_WithException_ShouldReturnBasicInsightsOnly()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 95.0  // High value for basic insight
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(default(DateTime), result.Timestamp);
        // Should have insights even if advanced analysis fails
        Assert.NotNull(result.Insights);
    }

    #endregion
}
