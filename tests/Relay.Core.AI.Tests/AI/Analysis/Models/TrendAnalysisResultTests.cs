using Relay.Core.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Models;

public class TrendAnalysisResultTests
{
    [Fact]
    public void Constructor_ShouldInitializeEmptyCollections()
    {
        // Act
        var result = new TrendAnalysisResult();

        // Assert
        Assert.NotNull(result.MovingAverages);
        Assert.Empty(result.MovingAverages);
        Assert.NotNull(result.TrendDirections);
        Assert.Empty(result.TrendDirections);
        Assert.NotNull(result.TrendVelocities);
        Assert.Empty(result.TrendVelocities);
        Assert.NotNull(result.SeasonalityPatterns);
        Assert.Empty(result.SeasonalityPatterns);
        Assert.NotNull(result.RegressionResults);
        Assert.Empty(result.RegressionResults);
        Assert.NotNull(result.Correlations);
        Assert.Empty(result.Correlations);
        Assert.NotNull(result.Anomalies);
        Assert.Empty(result.Anomalies);
        Assert.NotNull(result.Insights);
        Assert.Empty(result.Insights);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { CurrentValue = 80.0, Timestamp = timestamp }
        };
        var trendDirections = new Dictionary<string, TrendDirection>
        {
            ["cpu"] = TrendDirection.Increasing
        };
        var trendVelocities = new Dictionary<string, double>
        {
            ["cpu"] = 0.5
        };
        var seasonalityPatterns = new Dictionary<string, SeasonalityPattern>
        {
            ["cpu"] = new SeasonalityPattern { DailyPattern = "daily", ExpectedMultiplier = 0.8 }
        };
        var regressionResults = new Dictionary<string, RegressionResult>
        {
            ["cpu"] = new RegressionResult { Slope = 0.1, Intercept = 70.0, RSquared = 0.85 }
        };
        var correlations = new Dictionary<string, List<string>>
        {
            ["cpu"] = new List<string> { "memory", "disk" }
        };
        var anomalies = new List<MetricAnomaly>
        {
            new MetricAnomaly
            {
                MetricName = "cpu",
                CurrentValue = 95.0,
                ExpectedValue = 80.0,
                Severity = AnomalySeverity.High
            }
        };
        var insights = new List<TrendInsight>
        {
            new TrendInsight
            {
                Category = "Resource Utilization",
                Severity = InsightSeverity.Warning,
                Message = "High CPU utilization detected",
                RecommendedAction = "Monitor CPU usage closely"
            }
        };

        // Act
        var result = new TrendAnalysisResult
        {
            Timestamp = timestamp,
            MovingAverages = movingAverages,
            TrendDirections = trendDirections,
            TrendVelocities = trendVelocities,
            SeasonalityPatterns = seasonalityPatterns,
            RegressionResults = regressionResults,
            Correlations = correlations,
            Anomalies = anomalies,
            Insights = insights
        };

        // Assert
        Assert.Equal(timestamp, result.Timestamp);
        Assert.Equal(movingAverages, result.MovingAverages);
        Assert.Equal(trendDirections, result.TrendDirections);
        Assert.Equal(trendVelocities, result.TrendVelocities);
        Assert.Equal(seasonalityPatterns, result.SeasonalityPatterns);
        Assert.Equal(regressionResults, result.RegressionResults);
        Assert.Equal(correlations, result.Correlations);
        Assert.Equal(anomalies, result.Anomalies);
        Assert.Equal(insights, result.Insights);
    }

    [Fact]
    public void Insights_ShouldAllowAddingAndRemovingItems()
    {
        // Arrange
        var result = new TrendAnalysisResult();
        var insight = new TrendInsight
        {
            Category = "Performance",
            Severity = InsightSeverity.Info,
            Message = "System operating normally",
            RecommendedAction = "Continue monitoring"
        };

        // Act
        result.Insights.Add(insight);

        // Assert
        Assert.Single(result.Insights);
        Assert.Equal(insight, result.Insights[0]);

        // Act
        result.Insights.Remove(insight);

        // Assert
        Assert.Empty(result.Insights);
    }

    [Fact]
    public void Insights_ShouldSupportMultipleInsights()
    {
        // Arrange
        var result = new TrendAnalysisResult();
        var insights = new List<TrendInsight>
        {
            new TrendInsight
            {
                Category = "CPU",
                Severity = InsightSeverity.Warning,
                Message = "High CPU usage",
                RecommendedAction = "Optimize CPU-intensive operations"
            },
            new TrendInsight
            {
                Category = "Memory",
                Severity = InsightSeverity.Critical,
                Message = "Critical memory usage",
                RecommendedAction = "Immediate memory optimization required"
            },
            new TrendInsight
            {
                Category = "Network",
                Severity = InsightSeverity.Info,
                Message = "Network performance stable",
                RecommendedAction = "Monitor network metrics"
            }
        };

        // Act
        result.Insights.AddRange(insights);

        // Assert
        Assert.Equal(3, result.Insights.Count);
        Assert.Equal(1, result.Insights.Count(i => i.Severity == InsightSeverity.Warning));
        Assert.Equal(1, result.Insights.Count(i => i.Severity == InsightSeverity.Critical));
        Assert.Equal(1, result.Insights.Count(i => i.Severity == InsightSeverity.Info));
    }

    [Fact]
    public void DefaultTimestamp_ShouldBeDefaultDateTime()
    {
        // Act
        var result = new TrendAnalysisResult();

        // Assert
        Assert.Equal(default(DateTime), result.Timestamp);
    }
}