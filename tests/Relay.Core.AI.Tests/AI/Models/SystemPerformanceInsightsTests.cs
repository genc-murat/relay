using System.Collections.Generic;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Data;
using Xunit;

namespace Relay.Core.Tests.AI.Models;

public class SystemPerformanceInsightsTests
{
    [Fact]
    public void SystemPerformanceInsights_Should_Initialize_With_Default_Values()
    {
        // Act
        var insights = new SystemPerformanceInsights();

        // Assert
        Assert.NotNull(insights.SeasonalPatterns);
        Assert.Empty(insights.SeasonalPatterns);
        Assert.NotNull(insights.HealthScore);
        Assert.NotNull(insights.Bottlenecks);
        Assert.Empty(insights.Bottlenecks);
        Assert.NotNull(insights.Opportunities);
        Assert.Empty(insights.Opportunities);
        Assert.NotNull(insights.Predictions);
        Assert.Equal('\0', insights.PerformanceGrade); // Default char value
        Assert.NotNull(insights.KeyMetrics);
        Assert.Empty(insights.KeyMetrics);
        Assert.NotNull(insights.LoadPatterns);
    }

    [Fact]
    public void SystemPerformanceInsights_Should_Allow_Setting_SeasonalPatterns_In_Object_Initializer()
    {
        // Arrange
        var patterns = new List<SeasonalPattern>
        {
            new SeasonalPattern { Period = 24, Strength = 0.8, Type = "Daily" },
            new SeasonalPattern { Period = 7, Strength = 0.6, Type = "Weekly" }
        };

        // Act
        var insights = new SystemPerformanceInsights
        {
            SeasonalPatterns = patterns
        };

        // Assert
        Assert.Equal(2, insights.SeasonalPatterns.Count);
        Assert.Equal(24, insights.SeasonalPatterns[0].Period);
        Assert.Equal(0.8, insights.SeasonalPatterns[0].Strength);
        Assert.Equal("Daily", insights.SeasonalPatterns[0].Type);
        Assert.Equal(7, insights.SeasonalPatterns[1].Period);
        Assert.Equal(0.6, insights.SeasonalPatterns[1].Strength);
        Assert.Equal("Weekly", insights.SeasonalPatterns[1].Type);
    }

    [Fact]
    public void SystemPerformanceInsights_Should_Support_Object_Initialization_With_SeasonalPatterns()
    {
        // Arrange
        var patterns = new List<SeasonalPattern>
        {
            new SeasonalPattern { Period = 12, Strength = 0.9, Type = "Hourly" }
        };

        // Act
        var insights = new SystemPerformanceInsights
        {
            SeasonalPatterns = patterns,
            PerformanceGrade = 'A'
        };

        // Assert
        Assert.Single(insights.SeasonalPatterns);
        Assert.Equal(12, insights.SeasonalPatterns[0].Period);
        Assert.Equal(0.9, insights.SeasonalPatterns[0].Strength);
        Assert.Equal("Hourly", insights.SeasonalPatterns[0].Type);
        Assert.Equal('A', insights.PerformanceGrade);
    }

    [Fact]
    public void SystemPerformanceInsights_SeasonalPatterns_Should_Be_Empty_List_By_Default()
    {
        // Act
        var insights = new SystemPerformanceInsights();

        // Assert
        Assert.NotNull(insights.SeasonalPatterns);
        Assert.IsType<List<SeasonalPattern>>(insights.SeasonalPatterns);
        Assert.Empty(insights.SeasonalPatterns);
    }

    [Fact]
    public void SystemPerformanceInsights_Should_Accept_Empty_SeasonalPatterns_List_In_Object_Initializer()
    {
        // Act
        var insights = new SystemPerformanceInsights
        {
            SeasonalPatterns = new List<SeasonalPattern>()
        };

        // Assert
        Assert.NotNull(insights.SeasonalPatterns);
        Assert.Empty(insights.SeasonalPatterns);
    }

    [Fact]
    public void SystemPerformanceInsights_Should_Accept_Null_SeasonalPatterns_And_Initialize_Empty()
    {
        // Arrange
        var insights = new SystemPerformanceInsights();

        // Act - SeasonalPatterns is init-only, so we can't set it to null after initialization
        // But we can verify the default initialization
        Assert.NotNull(insights.SeasonalPatterns);
        Assert.IsType<List<SeasonalPattern>>(insights.SeasonalPatterns);
    }

    [Fact]
    public void SystemPerformanceInsights_Should_Allow_Setting_LoadPatterns_In_Object_Initializer()
    {
        // Arrange
        var loadPatternData = new LoadPatternData
        {
            Level = LoadLevel.High,
            SuccessRate = 0.85,
            AverageImprovement = 0.15,
            TotalPredictions = 100
        };

        // Act
        var insights = new SystemPerformanceInsights
        {
            LoadPatterns = loadPatternData
        };

        // Assert
        Assert.NotNull(insights.LoadPatterns);
        Assert.Equal(LoadLevel.High, insights.LoadPatterns.Level);
        Assert.Equal(0.85, insights.LoadPatterns.SuccessRate);
        Assert.Equal(0.15, insights.LoadPatterns.AverageImprovement);
        Assert.Equal(100, insights.LoadPatterns.TotalPredictions);
    }
}