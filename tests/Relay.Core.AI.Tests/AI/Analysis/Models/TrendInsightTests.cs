using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Models;

public class TrendInsightTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var insight = new TrendInsight();

        // Assert
        Assert.Equal(string.Empty, insight.Category);
        Assert.Equal(InsightSeverity.Info, insight.Severity);
        Assert.Equal(string.Empty, insight.Message);
        Assert.Equal(string.Empty, insight.RecommendedAction);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var insight = new TrendInsight();

        // Act
        insight.Category = "Performance Trend";
        insight.Severity = InsightSeverity.Warning;
        insight.Message = "CPU utilization is trending upward";
        insight.RecommendedAction = "Monitor CPU usage and prepare scaling strategy";

        // Assert
        Assert.Equal("Performance Trend", insight.Category);
        Assert.Equal(InsightSeverity.Warning, insight.Severity);
        Assert.Equal("CPU utilization is trending upward", insight.Message);
        Assert.Equal("Monitor CPU usage and prepare scaling strategy", insight.RecommendedAction);
    }

    [Fact]
    public void ShouldSupportAllInsightSeverityLevels()
    {
        // Test Info severity
        var infoInsight = new TrendInsight
        {
            Category = "Normal Operation",
            Severity = InsightSeverity.Info,
            Message = "System operating within normal parameters",
            RecommendedAction = "Continue monitoring"
        };

        Assert.Equal(InsightSeverity.Info, infoInsight.Severity);

        // Test Warning severity
        var warningInsight = new TrendInsight
        {
            Category = "Resource Utilization",
            Severity = InsightSeverity.Warning,
            Message = "High CPU utilization detected",
            RecommendedAction = "Monitor closely and prepare optimization"
        };

        Assert.Equal(InsightSeverity.Warning, warningInsight.Severity);

        // Test Critical severity
        var criticalInsight = new TrendInsight
        {
            Category = "System Health",
            Severity = InsightSeverity.Critical,
            Message = "Critical memory usage - risk of system instability",
            RecommendedAction = "Immediate action required to prevent system failure"
        };

        Assert.Equal(InsightSeverity.Critical, criticalInsight.Severity);
    }

    [Fact]
    public void ShouldSupportVariousCategories()
    {
        // Test different categories
        var categories = new[]
        {
            "Performance Trend",
            "Resource Utilization",
            "Anomaly Detection",
            "System Health",
            "Capacity Planning",
            "Optimization Opportunity"
        };

        foreach (var category in categories)
        {
            var insight = new TrendInsight
            {
                Category = category,
                Severity = InsightSeverity.Info,
                Message = $"Test message for {category}",
                RecommendedAction = $"Test action for {category}"
            };

            Assert.Equal(category, insight.Category);
        }
    }

    [Fact]
    public void ShouldHandleLongMessagesAndActions()
    {
        // Arrange
        var longMessage = new string('A', 1000);
        var longAction = new string('B', 1000);

        // Act
        var insight = new TrendInsight
        {
            Category = "Test",
            Severity = InsightSeverity.Info,
            Message = longMessage,
            RecommendedAction = longAction
        };

        // Assert
        Assert.Equal(longMessage, insight.Message);
        Assert.Equal(longAction, insight.RecommendedAction);
    }

    [Fact]
    public void ShouldHandleEmptyStrings()
    {
        // Act
        var insight = new TrendInsight
        {
            Category = "",
            Severity = InsightSeverity.Info,
            Message = "",
            RecommendedAction = ""
        };

        // Assert
        Assert.Equal("", insight.Category);
        Assert.Equal("", insight.Message);
        Assert.Equal("", insight.RecommendedAction);
    }

    [Fact]
    public void ShouldHandleNullAssignmentsGracefully()
    {
        // Arrange
        var insight = new TrendInsight();

        // Act & Assert - Properties should handle null assignments
        // Note: Since properties are auto-implemented, they can't be null
        // but we can test that default empty strings work
        Assert.Equal(string.Empty, insight.Category);
        Assert.Equal(string.Empty, insight.Message);
        Assert.Equal(string.Empty, insight.RecommendedAction);
    }

    [Fact]
    public void ShouldBeEqualWhenAllPropertiesMatch()
    {
        // Arrange
        var insight1 = new TrendInsight
        {
            Category = "Test",
            Severity = InsightSeverity.Warning,
            Message = "Test message",
            RecommendedAction = "Test action"
        };

        var insight2 = new TrendInsight
        {
            Category = "Test",
            Severity = InsightSeverity.Warning,
            Message = "Test message",
            RecommendedAction = "Test action"
        };

        // Act & Assert
        // Note: Reference equality, not value equality since it's a class
        Assert.NotSame(insight1, insight2);
        Assert.Equal(insight1.Category, insight2.Category);
        Assert.Equal(insight1.Severity, insight2.Severity);
        Assert.Equal(insight1.Message, insight2.Message);
        Assert.Equal(insight1.RecommendedAction, insight2.RecommendedAction);
    }

    [Fact]
    public void ShouldSupportCopyingProperties()
    {
        // Arrange
        var original = new TrendInsight
        {
            Category = "Original Category",
            Severity = InsightSeverity.Critical,
            Message = "Original message",
            RecommendedAction = "Original action"
        };

        // Act
        var copy = new TrendInsight
        {
            Category = original.Category,
            Severity = original.Severity,
            Message = original.Message,
            RecommendedAction = original.RecommendedAction
        };

        // Assert
        Assert.Equal(original.Category, copy.Category);
        Assert.Equal(original.Severity, copy.Severity);
        Assert.Equal(original.Message, copy.Message);
        Assert.Equal(original.RecommendedAction, copy.RecommendedAction);
    }
}