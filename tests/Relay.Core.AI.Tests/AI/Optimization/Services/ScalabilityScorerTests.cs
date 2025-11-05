using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class ScalabilityScorerTests
{
    private readonly ILogger _logger;
    private readonly ScalabilityScorer _scorer;

    public ScalabilityScorerTests()
    {
        _logger = NullLogger.Instance;
        _scorer = new ScalabilityScorer(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new ScalabilityScorer(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var scorer = new ScalabilityScorer(logger);

        // Assert
        Assert.NotNull(scorer);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Name_Should_Return_Correct_Value()
    {
        // Act
        var name = _scorer.Name;

        // Assert
        Assert.Equal("ScalabilityScorer", name);
    }

    #endregion

    #region CalculateScoreCore Tests

    [Fact]
    public void CalculateScore_Should_Return_Maximum_Score_With_Perfect_Metrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ThreadCount"] = 1, // Minimal threads
            ["HandleCount"] = 100, // Minimal handles
            ["ThroughputPerSecond"] = 1000 // High throughput
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: threadEfficiency = min(1000/1, 10) / 10 = 10/10 = 1.0
        // handleEfficiency = min(1000/(100/100), 10) / 10 = min(1000/1, 10) / 10 = 10/10 = 1.0
        // Final score = (1.0 + 1.0) / 2.0 = 1.0
        var expectedScore = 1.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Return_Zero_With_Zero_Throughput()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ThreadCount"] = 50,
            ["HandleCount"] = 1000,
            ["ThroughputPerSecond"] = 0 // Zero throughput
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: threadEfficiency = min(0/50, 10) / 10 = 0/10 = 0.0
        // handleEfficiency = min(0/(1000/100), 10) / 10 = min(0/10, 10) / 10 = 0/10 = 0.0
        // Final score = (0.0 + 0.0) / 2.0 = 0.0
        var expectedScore = 0.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Calculate_Correctly_With_Moderate_Values()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ThreadCount"] = 100, // 100 threads
            ["HandleCount"] = 2000, // 2000 handles
            ["ThroughputPerSecond"] = 200 // Moderate throughput
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: threadEfficiency = min(200/100, 10) / 10 = min(2, 10) / 10 = 2/10 = 0.2
        // handleEfficiency = min(200/(2000/100), 10) / 10 = min(200/20, 10) / 10 = min(10, 10) / 10 = 1.0
        // Final score = (0.2 + 1.0) / 2.0 = 0.6
        var expectedScore = (0.2 + 1.0) / 2.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Use_Default_Values_For_Missing_Metrics()
    {
        // Arrange - Only provide throughput, others should use defaults
        var metrics = new Dictionary<string, double>
        {
            ["ThroughputPerSecond"] = 200
            // ThreadCount and HandleCount will use defaults (50 and 1000)
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: threadEfficiency = min(200/50, 10) / 10 = min(4, 10) / 10 = 0.4
        // handleEfficiency = min(200/(1000/100), 10) / 10 = min(200/10, 10) / 10 = min(20, 10) / 10 = 1.0
        // Final score = (0.4 + 1.0) / 2.0 = 0.7
        var expectedScore = (0.4 + 1.0) / 2.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Have_Max_Efficiency_Value()
    {
        // Arrange - Very high throughput to test the Max/Min limits
        var metrics = new Dictionary<string, double>
        {
            ["ThreadCount"] = 1, // Minimal threads
            ["HandleCount"] = 100, // Minimal handles
            ["ThroughputPerSecond"] = 10000 // Very high throughput
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: threadEfficiency = min(10000/1, 10) / 10 = min(10000, 10) / 10 = 10/10 = 1.0 (clamped)
        // handleEfficiency = min(10000/(100/100), 10) / 10 = min(10000/1, 10) / 10 = min(10000, 10) / 10 = 1.0 (clamped)
        // Final score = (1.0 + 1.0) / 2.0 = 1.0
        var expectedScore = 1.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Not_Exceed_1_0_Or_Fall_Below_0_0()
    {
        // Arrange - Test with extreme values
        var metrics = new Dictionary<string, double>();

        // Act
        var score = _scorer.CalculateScore(metrics);

        // Assert
        Assert.True(score >= 0.0);
        Assert.True(score <= 1.0);
    }

    #endregion

    #region GetCriticalAreas Tests

    [Fact]
    public void GetCriticalAreas_Should_Return_Empty_When_No_Critical_Issues()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ThreadCount"] = 150, // Below critical threshold (200)
            ["HandleCount"] = 4000 // Below critical threshold (5000)
        };

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.Empty(criticalAreas);
    }

    [Fact]
    public void GetCriticalAreas_Should_Return_Area_For_High_Thread_Count()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ThreadCount"] = 250, // Above critical threshold (200)
            ["HandleCount"] = 1000 // Below critical threshold (5000)
        };

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.NotEmpty(criticalAreas);
        Assert.Contains("High thread count", criticalAreas);
    }

    [Fact]
    public void GetCriticalAreas_Should_Return_Area_For_High_Handle_Count()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ThreadCount"] = 100, // Below critical threshold (200)
            ["HandleCount"] = 6000 // Above critical threshold (5000)
        };

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.NotEmpty(criticalAreas);
        Assert.Contains("High handle count", criticalAreas);
    }

    [Fact]
    public void GetCriticalAreas_Should_Return_Multiple_Areas_For_Multiple_Issues()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ThreadCount"] = 250, // Above critical threshold (200)
            ["HandleCount"] = 6000 // Above critical threshold (5000)
        };

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.Equal(2, criticalAreas.Count);
        Assert.Contains("High thread count", criticalAreas);
        Assert.Contains("High handle count", criticalAreas);
    }

    [Fact]
    public void GetCriticalAreas_Should_Use_Default_For_Missing_Metrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>(); // Empty - will use defaults

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.Empty(criticalAreas); // Default values (50 for threads, 1000 for handles) are below thresholds
    }

    #endregion

    #region GetRecommendations Tests

    [Fact]
    public void GetRecommendations_Should_Return_Empty_When_No_Issues()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ThreadCount"] = 90 // Below recommendation threshold (100)
        };

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.Empty(recommendations);
    }

    [Fact]
    public void GetRecommendations_Should_Return_Recommendation_For_High_Thread_Count()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ThreadCount"] = 150 // Above recommendation threshold (100)
        };

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.NotEmpty(recommendations);
        Assert.Contains("Consider thread pooling optimizations", recommendations);
    }

    [Fact]
    public void GetRecommendations_Should_Use_Default_For_Missing_Thread_Count()
    {
        // Arrange
        var metrics = new Dictionary<string, double>(); // Empty - will use default thread count (50)

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.Empty(recommendations); // Default thread count (50) is below threshold (100)
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void CalculateScore_Should_Handle_Zero_Division_Safely()
    {
        // Arrange - Test with zero thread count (should be handled by Math.Max)
        var metrics = new Dictionary<string, double>
        {
            ["ThreadCount"] = 0, // Zero threads - should be normalized to 1
            ["HandleCount"] = 100,
            ["ThroughputPerSecond"] = 100
        };

        // Act
        var score = _scorer.CalculateScore(metrics);

        // Assert: Should not throw and should return valid score
        Assert.True(score >= 0.0);
        Assert.True(score <= 1.0);
    }

    [Fact]
    public void CalculateScore_Should_Handle_NaN_And_Infinity_Gracefully()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ThreadCount"] = double.NaN,
            ["HandleCount"] = double.PositiveInfinity,
            ["ThroughputPerSecond"] = double.NegativeInfinity
        };

        // Act & Assert - Should not throw
        var score = _scorer.CalculateScore(metrics);
        Assert.True(score >= 0.0);
        Assert.True(score <= 1.0);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Full_Scoring_Workflow_Should_Work_Together()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ThreadCount"] = 120, // Above recommendation threshold (100)
            ["HandleCount"] = 4000, // Below critical threshold (5000)
            ["ThroughputPerSecond"] = 150
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.True(score >= 0.0 && score <= 1.0);
        
        // Thread count is not high enough for critical
        Assert.Empty(criticalAreas);
        
        // But it is high enough for recommendation
        Assert.NotEmpty(recommendations);
        Assert.Contains("Consider thread pooling optimizations", recommendations);
    }

    [Fact]
    public void Scoring_With_Critical_Values_Should_Trigger_Critical_Areas()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ThreadCount"] = 250, // Above critical threshold (200)
            ["HandleCount"] = 6000, // Above critical threshold (5000)
            ["ThroughputPerSecond"] = 50
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.True(score >= 0.0 && score <= 1.0);
        
        // Values are high enough for critical areas
        Assert.Equal(2, criticalAreas.Count);
        Assert.Contains("High thread count", criticalAreas);
        Assert.Contains("High handle count", criticalAreas);
        
        // Thread count also triggers recommendation
        Assert.NotEmpty(recommendations);
        Assert.Contains("Consider thread pooling optimizations", recommendations);
    }

    #endregion
}
