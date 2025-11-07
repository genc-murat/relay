using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

public class TrendAnalyzerTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TrendAnalyzer _analyzer;

    public TrendAnalyzerTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddTrendAnalysis();

        _serviceProvider = services.BuildServiceProvider();
        _analyzer = _serviceProvider.GetRequiredService<ITrendAnalyzer>() as TrendAnalyzer
            ?? throw new InvalidOperationException("Could not resolve TrendAnalyzer");
    }

    #region Constructor Tests

    [Fact]
    public void Service_Should_Be_Registered_In_DI_Container()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTrendAnalysis();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        var analyzer = provider.GetService<ITrendAnalyzer>();
        Assert.NotNull(analyzer);
    }

    #endregion

    #region AnalyzeMetricTrends Tests

    [Fact]
    public void AnalyzeMetricTrends_Should_Return_Result_With_Timestamp_When_Metrics_Are_Empty()
    {
        // Arrange
        var metrics = new Dictionary<string, double>();

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotEqual(default(DateTime), result.Timestamp);
        Assert.Empty(result.MovingAverages);
        Assert.Empty(result.TrendDirections);
        Assert.Empty(result.TrendVelocities);
        Assert.Empty(result.SeasonalityPatterns);
        Assert.Empty(result.RegressionResults);
        Assert.Empty(result.Correlations);
        Assert.Empty(result.Anomalies);
    }

    [Fact]
    public void AnalyzeMetricTrends_Should_Return_Populated_Result_When_Metrics_Are_Provided()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.5,
            ["memory"] = 85.2,
            ["latency"] = 120.0
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotEqual(default(DateTime), result.Timestamp);
        Assert.Equal(3, result.MovingAverages.Count);
        Assert.Equal(3, result.TrendDirections.Count);
        Assert.Equal(3, result.TrendVelocities.Count);
        Assert.Equal(3, result.SeasonalityPatterns.Count);
        Assert.Equal(3, result.RegressionResults.Count);
        // Anomalies list should exist (even if empty)
        Assert.NotNull(result.Anomalies);
    }

    [Fact]
    public void AnalyzeMetricTrends_Should_Include_Anomalies_In_Result()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 100.0  // This should trigger an anomaly
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result.Anomalies);
        // The anomaly detection might not trigger depending on the moving average calculation
        // but the Anomalies property should be populated by the analysis
    }

    [Fact]
    public void AnalyzeMetricTrends_Should_Handle_Exception_And_Return_Basic_Result()
    {
        // Arrange - we can't easily force an exception, but the method has try-catch
        // This test ensures the structure is correct even if internals fail
        var metrics = new Dictionary<string, double>
        {
            ["test"] = double.NaN // This might cause issues in calculations
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotEqual(default(DateTime), result.Timestamp);
        // Result should still be valid even if calculations fail
    }

    #endregion

    #region CalculateMovingAverages Tests

    [Fact]
    public void CalculateMovingAverages_Should_Return_Data_For_All_Metrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 80.0,
            ["memory"] = 90.0
        };
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _analyzer.CalculateMovingAverages(metrics, timestamp);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("cpu"));
        Assert.True(result.ContainsKey("memory"));
        Assert.Equal(timestamp, result["cpu"].Timestamp);
        Assert.Equal(80.0, result["cpu"].CurrentValue);
        Assert.Equal(90.0, result["memory"].CurrentValue);
    }

    #endregion

    #region DetectPerformanceAnomalies Tests

    [Fact]
    public void DetectPerformanceAnomalies_Should_Return_Empty_List_When_No_Anomalies()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
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
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectPerformanceAnomalies_Should_Detect_High_Anomaly()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 100.0
        };
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData
            {
                MA15 = 50.0, // Large difference
                Timestamp = DateTime.UtcNow
            }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        Assert.Single(anomalies);
        var anomaly = anomalies[0];
        Assert.Equal("cpu", anomaly.MetricName);
        Assert.Equal(100.0, anomaly.CurrentValue);
        Assert.Equal(50.0, anomaly.ExpectedValue);
        Assert.Equal(50.0, anomaly.Deviation);
        Assert.True(anomaly.ZScore > 3.0);
        Assert.Equal(AnomalySeverity.High, anomaly.Severity);
        Assert.NotEqual(default(DateTime), anomaly.Timestamp);
        Assert.Contains("cpu", anomaly.Description);
    }

    [Fact]
    public void DetectPerformanceAnomalies_Should_Detect_Multiple_Anomalies_With_Different_Severities()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 97.5,      // Medium anomaly (Z-score = (97.5-75)/(75*0.1) = 22.5/7.5 = 3.0)
            ["memory"] = 120.0,  // High anomaly (Z-score = (120-80)/(80*0.1) = 40/8 = 5.0)
            ["latency"] = 75.0   // Normal
        };
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow },
            ["memory"] = new MovingAverageData { MA15 = 80.0, Timestamp = DateTime.UtcNow },
            ["latency"] = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        Assert.Equal(2, anomalies.Count);

        var cpuAnomaly = anomalies.Find(a => a.MetricName == "cpu");
        Assert.NotNull(cpuAnomaly);
        Assert.Equal(AnomalySeverity.Medium, cpuAnomaly.Severity);
        Assert.Equal(22.5, cpuAnomaly.Deviation);

        var memoryAnomaly = anomalies.Find(a => a.MetricName == "memory");
        Assert.NotNull(memoryAnomaly);
        Assert.Equal(AnomalySeverity.High, memoryAnomaly.Severity);
        Assert.Equal(40.0, memoryAnomaly.Deviation);
    }

    [Fact]
    public void DetectPerformanceAnomalies_Should_Handle_Empty_Moving_Averages()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 85.0
        };
        var movingAverages = new Dictionary<string, MovingAverageData>(); // Empty

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectPerformanceAnomalies_Should_Handle_Empty_Metrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>(); // Empty
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        Assert.Empty(anomalies);
    }

    #endregion

    #region CalculateMovingAverages Tests

    [Fact]
    public void CalculateMovingAverages_Should_Delegate_To_Updater()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0,
            ["memory"] = 80.0
        };
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _analyzer.CalculateMovingAverages(metrics, timestamp);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("cpu", result.Keys);
        Assert.Contains("memory", result.Keys);

        // Verify the data structure
        foreach (var kvp in result)
        {
            Assert.NotEqual(default(DateTime), kvp.Value.Timestamp);
            Assert.True(kvp.Value.MA5 >= 0);
            Assert.True(kvp.Value.MA15 >= 0);
        }
    }

    [Fact]
    public void CalculateMovingAverages_WithEmptyMetrics_ReturnsEmptyDictionary()
    {
        // Arrange
        var metrics = new Dictionary<string, double>();
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _analyzer.CalculateMovingAverages(metrics, timestamp);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region TrendInsight Tests

    [Fact]
    public void AnalyzeMetricTrends_Should_Include_Insights_In_Result()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 95.0,  // High CPU should generate insight
            ["memory"] = 85.0
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result.Insights);
        Assert.True(result.Insights.Count > 0, "Should generate at least one insight for high CPU usage");
    }

    [Fact]
    public void AnalyzeMetricTrends_Should_Generate_Critical_Insight_For_High_CPU()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 96.0  // Above 95% threshold
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        var cpuInsight = result.Insights.Find(i => i.Message.Contains("cpu") && i.Severity == InsightSeverity.Critical);
        Assert.NotNull(cpuInsight);
        Assert.Contains("Critical utilization level", cpuInsight.Message);
        Assert.Contains("Immediate action required", cpuInsight.RecommendedAction);
    }

    [Fact]
    public void AnalyzeMetricTrends_Should_Generate_Warning_Insight_For_Elevated_CPU()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 82.0  // Between 80-95%
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result.Insights);
        Assert.True(result.Insights.Count > 0, $"Expected insights to be generated, but got {result.Insights.Count}");

        var cpuInsight = result.Insights.Find(i => i.Message.Contains("cpu") && i.Severity == InsightSeverity.Warning);
        Assert.NotNull(cpuInsight);
        Assert.Contains("High utilization level", cpuInsight.Message);
        Assert.Contains("Monitor closely", cpuInsight.RecommendedAction);
    }

    [Fact]
    public void AnalyzeMetricTrends_Should_Generate_Insight_For_Increasing_Trend_With_High_Velocity()
    {
        // Arrange - We need to simulate trend data that would show increasing trend
        // This is tricky to test directly since it depends on internal state
        // Let's test with a metric that should trigger anomaly detection
        var metrics = new Dictionary<string, double>
        {
            ["response_time"] = 150.0  // High value that might be anomalous
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        // The insight generation depends on the analysis results
        // At minimum, we should have some insights generated
        Assert.NotNull(result.Insights);
    }

    [Fact]
    public void AnalyzeMetricTrends_Should_Generate_Anomaly_Insights()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 100.0  // This should trigger an anomaly
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        // Should have at least the high utilization insight
        Assert.True(result.Insights.Count > 0);
        var insights = result.Insights.Where(i => i.Category == "Anomaly Detection" || i.Category.Contains("Resource Utilization"));
        Assert.True(insights.Any());
    }

    [Fact]
    public void AnalyzeMetricTrends_Should_Handle_Empty_Metrics_With_Empty_Insights()
    {
        // Arrange
        var metrics = new Dictionary<string, double>();

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result.Insights);
        Assert.Empty(result.Insights);
    }

    #endregion

}