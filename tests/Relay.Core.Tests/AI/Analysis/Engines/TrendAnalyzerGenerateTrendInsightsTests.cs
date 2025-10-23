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
/// Comprehensive tests for TrendAnalyzer.GenerateTrendInsights method.
/// Tests the private method through reflection to ensure complete coverage.
/// </summary>
public class TrendAnalyzerGenerateTrendInsightsTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TrendAnalyzer _analyzer;
    private readonly MethodInfo _generateTrendInsightsMethod;

    public TrendAnalyzerGenerateTrendInsightsTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddTrendAnalysis();

        _serviceProvider = services.BuildServiceProvider();
        _analyzer = _serviceProvider.GetRequiredService<ITrendAnalyzer>() as TrendAnalyzer
            ?? throw new InvalidOperationException("Could not resolve TrendAnalyzer");

        // Get the private GenerateTrendInsights method
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

    #region Increasing Trend Tests

    [Fact]
    public void GenerateTrendInsights_WithIncreasingTrendHighVelocity_ShouldGenerateCriticalInsight()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 0.6 // High velocity (> 0.5)
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.FirstOrDefault(i => i.Category == "Performance Trend");
        Assert.NotNull(insight);
        Assert.Equal(InsightSeverity.Critical, insight.Severity);
        Assert.Contains("cpu", insight.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("trending upward", insight.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0.60", insight.Message);
    }

    [Fact]
    public void GenerateTrendInsights_WithIncreasingTrendLowVelocity_ShouldGenerateWarningInsight()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["memory"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["memory"] = 0.3 // Medium velocity (0.1 < v <= 0.5)
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["memory"] = 60.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.FirstOrDefault(i => i.Category == "Performance Trend");
        Assert.NotNull(insight);
        Assert.Equal(InsightSeverity.Warning, insight.Severity);
        Assert.Contains("trending upward", insight.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateTrendInsights_WithIncreasingCpuTrend_ShouldRecommendCpuScaling()
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
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 70.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.FirstOrDefault(i => i.Message.Contains("cpu"));
        Assert.NotNull(insight);
        Assert.Contains("CPU", insight.RecommendedAction);
        Assert.Contains("scaling", insight.RecommendedAction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateTrendInsights_WithIncreasingMemoryTrend_ShouldRecommendMemoryOptimization()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["memory"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["memory"] = 0.2
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["memory"] = 70.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.FirstOrDefault(i => i.Message.Contains("memory"));
        Assert.NotNull(insight);
        Assert.Contains("memory", insight.RecommendedAction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateTrendInsights_WithIncreasingErrorTrend_ShouldRecommendErrorInvestigation()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["error_rate"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["error_rate"] = 0.2
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["error_rate"] = 5.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.FirstOrDefault(i => i.Message.Contains("error", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(insight);
        Assert.Contains("error", insight.RecommendedAction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateTrendInsights_WithIncreasingUnknownMetricTrend_ShouldRecommendMonitoring()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["request_count"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["request_count"] = 0.2
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["request_count"] = 100.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.FirstOrDefault(i => i.Category == "Performance Trend");
        Assert.NotNull(insight);
        Assert.Contains("monitor", insight.RecommendedAction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateTrendInsights_WithIncreasingTrendZeroVelocity_ShouldNotGenerateInsight()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 0.05 // Below threshold of 0.1
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 50.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        // Should not generate trend insight for low velocity
        var trendInsights = insights.Where(i => i.Category == "Performance Trend").ToList();
        Assert.Empty(trendInsights);
    }

    #endregion

    #region Decreasing Trend Tests

    [Fact]
    public void GenerateTrendInsights_WithDecreasingTrendHighVelocity_ShouldGenerateImprovementInsight()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["error_rate"] = TrendDirection.Decreasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["error_rate"] = -0.6 // Absolute value > 0.5
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["error_rate"] = 2.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.FirstOrDefault(i => i.Category == "Performance Improvement");
        Assert.NotNull(insight);
        Assert.Equal(InsightSeverity.Info, insight.Severity);
        Assert.Contains("improving", insight.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("downward trend", insight.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateTrendInsights_WithDecreasingTrendLowVelocity_ShouldGenerateInfoInsight()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["latency"] = TrendDirection.Decreasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["latency"] = -0.2 // Absolute value between 0.1 and 0.5
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["latency"] = 100.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.FirstOrDefault(i => i.Category == "Performance Improvement");
        Assert.NotNull(insight);
        Assert.Equal(InsightSeverity.Info, insight.Severity);
    }

    [Fact]
    public void GenerateTrendInsights_WithDecreasingTrend_ShouldRecommendContinueMonitoring()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Decreasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = -0.3
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 50.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.FirstOrDefault(i => i.Category == "Performance Improvement");
        Assert.NotNull(insight);
        Assert.Contains("Continue monitoring", insight.RecommendedAction);
        Assert.Contains("stability", insight.RecommendedAction);
    }

    [Fact]
    public void GenerateTrendInsights_WithDecreasingTrendBelowVelocityThreshold_ShouldNotGenerateInsight()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Decreasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = -0.05 // Below threshold
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 50.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        var trendInsights = insights.Where(i => i.Category == "Performance Improvement").ToList();
        Assert.Empty(trendInsights);
    }

    #endregion

    #region Stable Trend Tests

    [Fact]
    public void GenerateTrendInsights_WithStableTrend_ShouldNotGenerateInsight()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Stable
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 0.05
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 50.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        var trendInsights = insights.Where(i => i.Category == "Performance Trend" || i.Category == "Performance Improvement").ToList();
        Assert.Empty(trendInsights);
    }

    #endregion

    #region Multiple Trends Tests

    [Fact]
    public void GenerateTrendInsights_WithMultipleTrends_ShouldGenerateMultipleInsights()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing,
            ["memory"] = TrendDirection.Increasing,
            ["error_rate"] = TrendDirection.Decreasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 0.2,
            ["memory"] = 0.3,
            ["error_rate"] = -0.15
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 60.0,
            ["memory"] = 70.0,
            ["error_rate"] = 3.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.True(insights.Count >= 3);
        Assert.Contains(insights, i => i.Message.Contains("cpu"));
        Assert.Contains(insights, i => i.Message.Contains("memory"));
        Assert.Contains(insights, i => i.Message.Contains("error"));
    }

    #endregion

    #region Anomaly Detection Tests

    [Fact]
    public void GenerateTrendInsights_WithCriticalAnomaly_ShouldGenerateCriticalInsight()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>();
        var trendVelocities = new Dictionary<string, double>();
        var anomalies = new List<MetricAnomaly>
        {
            new MetricAnomaly
            {
                MetricName = "cpu",
                CurrentValue = 100.0,
                ExpectedValue = 50.0,
                Severity = AnomalySeverity.Critical,
                Description = "Unexpected CPU spike",
                Timestamp = DateTime.UtcNow,
                ZScore = 5.0
            }
        };
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 100.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var anomalyInsight = insights.FirstOrDefault(i => i.Category == "Anomaly Detection");
        Assert.NotNull(anomalyInsight);
        Assert.Equal(InsightSeverity.Critical, anomalyInsight.Severity);
        Assert.Contains("cpu", anomalyInsight.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Anomaly detected", anomalyInsight.Message);
        Assert.Contains("Immediate investigation", anomalyInsight.RecommendedAction);
    }

    [Fact]
    public void GenerateTrendInsights_WithHighAnomaly_ShouldGenerateWarningInsight()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>();
        var trendVelocities = new Dictionary<string, double>();
        var anomalies = new List<MetricAnomaly>
        {
            new MetricAnomaly
            {
                MetricName = "memory",
                CurrentValue = 95.0,
                ExpectedValue = 60.0,
                Severity = AnomalySeverity.High,
                Description = "High memory usage",
                Timestamp = DateTime.UtcNow,
                ZScore = 4.0
            }
        };
        var currentMetrics = new Dictionary<string, double>
        {
            ["memory"] = 95.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var anomalyInsight = insights.FirstOrDefault(i => i.Category == "Anomaly Detection");
        Assert.NotNull(anomalyInsight);
        Assert.Equal(InsightSeverity.Warning, anomalyInsight.Severity);
        Assert.Contains("Monitor this anomaly", anomalyInsight.RecommendedAction);
    }

    [Fact]
    public void GenerateTrendInsights_WithMediumAnomaly_ShouldGenerateWarningInsight()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>();
        var trendVelocities = new Dictionary<string, double>();
        var anomalies = new List<MetricAnomaly>
        {
            new MetricAnomaly
            {
                MetricName = "latency",
                CurrentValue = 150.0,
                ExpectedValue = 100.0,
                Severity = AnomalySeverity.Medium,
                Description = "Elevated latency",
                Timestamp = DateTime.UtcNow,
                ZScore = 3.0
            }
        };
        var currentMetrics = new Dictionary<string, double>
        {
            ["latency"] = 150.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var anomalyInsight = insights.FirstOrDefault(i => i.Category == "Anomaly Detection");
        Assert.NotNull(anomalyInsight);
        Assert.Equal(InsightSeverity.Warning, anomalyInsight.Severity);
    }

    [Fact]
    public void GenerateTrendInsights_WithLowAnomaly_ShouldGenerateInfoInsight()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>();
        var trendVelocities = new Dictionary<string, double>();
        var anomalies = new List<MetricAnomaly>
        {
            new MetricAnomaly
            {
                MetricName = "response_time",
                CurrentValue = 110.0,
                ExpectedValue = 100.0,
                Severity = AnomalySeverity.Low,
                Description = "Slightly elevated response time",
                Timestamp = DateTime.UtcNow,
                ZScore = 2.0
            }
        };
        var currentMetrics = new Dictionary<string, double>
        {
            ["response_time"] = 110.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var anomalyInsight = insights.FirstOrDefault(i => i.Category == "Anomaly Detection");
        Assert.NotNull(anomalyInsight);
        Assert.Equal(InsightSeverity.Info, anomalyInsight.Severity);
    }

    [Fact]
    public void GenerateTrendInsights_WithMultipleAnomalies_ShouldGenerateMultipleAnomalyInsights()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>();
        var trendVelocities = new Dictionary<string, double>();
        var anomalies = new List<MetricAnomaly>
        {
            new MetricAnomaly
            {
                MetricName = "cpu",
                CurrentValue = 100.0,
                ExpectedValue = 50.0,
                Severity = AnomalySeverity.Critical,
                Description = "CPU spike",
                Timestamp = DateTime.UtcNow,
                ZScore = 5.0
            },
            new MetricAnomaly
            {
                MetricName = "memory",
                CurrentValue = 95.0,
                ExpectedValue = 60.0,
                Severity = AnomalySeverity.High,
                Description = "Memory spike",
                Timestamp = DateTime.UtcNow,
                ZScore = 4.0
            }
        };
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 100.0,
            ["memory"] = 95.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        var anomalyInsights = insights.Where(i => i.Category == "Anomaly Detection").ToList();
        Assert.Equal(2, anomalyInsights.Count);
        Assert.Contains(anomalyInsights, i => i.Message.Contains("cpu"));
        Assert.Contains(anomalyInsights, i => i.Message.Contains("memory"));
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void GenerateTrendInsights_WithEmptyInputs_ShouldReturnEmptyList()
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
    public void GenerateTrendInsights_WithMissingVelocityData_ShouldUseDefaultZero()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing,
            ["memory"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 0.2
            // "memory" velocity not provided - should default to 0.0
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 60.0,
            ["memory"] = 70.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        // Only CPU should generate insight (velocity > 0.1)
        var cpuInsights = insights.Where(i => i.Message.Contains("cpu")).ToList();
        Assert.Single(cpuInsights);

        var memoryInsights = insights.Where(i => i.Message.Contains("memory")).ToList();
        Assert.Empty(memoryInsights);
    }

    [Fact]
    public void GenerateTrendInsights_WithMissingCurrentMetricValue_ShouldUseDefaultZero()
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
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>();
        // "cpu" value not provided - should default to 0.0

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.First();
        // Should still generate insight even with missing current value
        Assert.Contains("cpu", insight.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateTrendInsights_WithNegativeVelocities_ShouldHandleCorrectly()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["error_rate"] = TrendDirection.Decreasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["error_rate"] = -0.35 // Negative velocity
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["error_rate"] = 2.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.First();
        Assert.Equal(InsightSeverity.Info, insight.Severity);
        Assert.Contains("improving", insight.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateTrendInsights_WithVeryHighVelocity_ShouldGenerateCriticalInsight()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 2.5 // Very high velocity
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
        Assert.Contains("2.500", insight.Message);
    }

    [Fact]
    public void GenerateTrendInsights_WithMetricNameCaseInsensitivity_ShouldMatchCorrectly()
    {
        // Arrange - Test case-insensitive matching
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["CPU"] = TrendDirection.Increasing,
            ["Memory"] = TrendDirection.Increasing,
            ["Error_Rate"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["CPU"] = 0.2,
            ["Memory"] = 0.2,
            ["Error_Rate"] = 0.2
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["CPU"] = 60.0,
            ["Memory"] = 70.0,
            ["Error_Rate"] = 5.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        var cpuRecommendations = insights.Where(i => i.Message.Contains("CPU")).Select(i => i.RecommendedAction).ToList();
        Assert.NotEmpty(cpuRecommendations);
        Assert.All(cpuRecommendations, action => Assert.Contains("CPU", action));
    }

    [Fact]
    public void GenerateTrendInsights_WithMixedTrendsAndAnomalies_ShouldGenerateAllApplicableInsights()
    {
        // Arrange - Complex scenario with multiple trends and anomalies
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing,
            ["memory"] = TrendDirection.Decreasing,
            ["latency"] = TrendDirection.Stable
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 0.3,
            ["memory"] = -0.2,
            ["latency"] = 0.01
        };
        var anomalies = new List<MetricAnomaly>
        {
            new MetricAnomaly
            {
                MetricName = "error_count",
                CurrentValue = 150.0,
                ExpectedValue = 50.0,
                Severity = AnomalySeverity.Critical,
                Description = "Error spike",
                Timestamp = DateTime.UtcNow,
                ZScore = 6.0
            }
        };
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 65.0,
            ["memory"] = 55.0,
            ["latency"] = 100.0,
            ["error_count"] = 150.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.True(insights.Count >= 3); // At least CPU trend, memory improvement, and anomaly
        Assert.Contains(insights, i => i.Category == "Performance Trend"); // CPU
        Assert.Contains(insights, i => i.Category == "Performance Improvement"); // Memory
        Assert.Contains(insights, i => i.Category == "Anomaly Detection"); // Error spike
        Assert.DoesNotContain(insights, i => i.Message.Contains("latency")); // Stable, no insight
    }

    #endregion

    #region Message Formatting Tests

    [Fact]
    public void GenerateTrendInsights_IncreasingTrendMessage_ShouldIncludeVelocityFormatted()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 0.123456
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 60.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        var insight = insights.First();
        Assert.Contains("0.123", insight.Message); // Should be formatted to 3 decimal places
    }

    [Fact]
    public void GenerateTrendInsights_DecreasingTrendMessage_ShouldIncludeVelocityFormatted()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["error"] = TrendDirection.Decreasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["error"] = -0.987654
        };
        var anomalies = new List<MetricAnomaly>();
        var currentMetrics = new Dictionary<string, double>
        {
            ["error"] = 3.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        var insight = insights.First();
        Assert.Contains("-0.988", insight.Message); // Formatted with 3 decimal places
    }

    [Fact]
    public void GenerateTrendInsights_AnomalyMessage_ShouldIncludeMetricNameAndDescription()
    {
        // Arrange
        var trendDirections = new Dictionary<string, TrendDirection>();
        var trendVelocities = new Dictionary<string, double>();
        var anomalies = new List<MetricAnomaly>
        {
            new MetricAnomaly
            {
                MetricName = "response_time",
                CurrentValue = 500.0,
                ExpectedValue = 100.0,
                Severity = AnomalySeverity.High,
                Description = "Unexpected latency increase",
                Timestamp = DateTime.UtcNow,
                ZScore = 4.0
            }
        };
        var currentMetrics = new Dictionary<string, double>
        {
            ["response_time"] = 500.0
        };

        // Act
        var insights = InvokeGenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

        // Assert
        Assert.NotEmpty(insights);
        var insight = insights.First();
        Assert.Contains("response_time", insight.Message);
        Assert.Contains("Unexpected latency increase", insight.Message);
    }

    #endregion

    #region Integration Tests with AnalyzeMetricTrends

    [Fact]
    public void AnalyzeMetricTrends_WithHighCpuAndIncreasingTrend_ShouldHaveRichInsights()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 92.0 // High CPU that triggers basic insight
        };

        // Act - Call through public method that calls GenerateTrendInsights
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotEmpty(result.Insights);
        // Should have at least the high utilization insight
        var cpuInsight = result.Insights.FirstOrDefault(i => i.Message.Contains("cpu") && i.Severity == InsightSeverity.Critical);
        Assert.NotNull(cpuInsight);
    }

    [Fact]
    public void AnalyzeMetricTrends_WithVariousMetrics_ShouldGenerateComprehensiveInsights()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 85.0,
            ["memory"] = 92.0,
            ["error_rate"] = 2.5,
            ["latency"] = 150.0
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotEmpty(result.Insights);
        // Should have insights for high CPU and memory
        var highUtilizationInsights = result.Insights.Where(i => i.Category == "Resource Utilization").ToList();
        Assert.NotEmpty(highUtilizationInsights);
    }

    #endregion
}
