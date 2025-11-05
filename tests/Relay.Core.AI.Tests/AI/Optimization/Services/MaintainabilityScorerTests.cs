using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class MaintainabilityScorerTests
{
    private readonly ILogger _logger;
    private readonly MaintainabilityScorer _scorer;

    public MaintainabilityScorerTests()
    {
        _logger = NullLogger.Instance;
        _scorer = new MaintainabilityScorer(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new MaintainabilityScorer(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var scorer = new MaintainabilityScorer(logger);

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
        Assert.Equal("MaintainabilityScorer", name);
    }

    #endregion

    #region CalculateScoreCore Tests

    [Fact]
    public void CalculateScore_Should_Return_Maximum_Score_With_Perfect_Metrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.0, // No errors
            ["CpuUtilization"] = 0.0, // No CPU usage
            ["MemoryUtilization"] = 0.0 // No memory usage
        };

        // Act
        var score = _scorer.CalculateScore(metrics);

        // Assert
        Assert.Equal(1.0, score, 6); // Maximum possible score
    }

    [Fact]
    public void CalculateScore_Should_Return_Minimum_Score_With_Worst_Metrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 1.0, // Maximum error rate
            ["CpuUtilization"] = 1.0, // Maximum CPU usage
            ["MemoryUtilization"] = 1.0 // Maximum memory usage
        };

        // Act
        var score = _scorer.CalculateScore(metrics);

        // Assert
        Assert.Equal(0.0, score, 6); // Minimum possible score
    }

    [Fact]
    public void CalculateScore_Should_Calculate_Correctly_With_High_Error_Rate()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.5, // High error rate
            ["CpuUtilization"] = 0.0, // No CPU usage
            ["MemoryUtilization"] = 0.0 // No memory usage
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: errorScore = 1.0 - min(0.5 * 2, 1.0) = 1.0 - 1.0 = 0.0
        // resourceScore = 1.0 - max(0.0, 0.0) = 1.0
        // Final score = (0.0 + 1.0) / 2.0 = 0.5
        var expectedScore = 0.5;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Calculate_Correctly_With_High_Resource_Usage()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.0, // No errors
            ["CpuUtilization"] = 0.8, // High CPU usage
            ["MemoryUtilization"] = 0.9 // High memory usage (will be max)
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: errorScore = 1.0 - min(0.0 * 2, 1.0) = 1.0
        // resourceScore = 1.0 - max(0.8, 0.9) = 1.0 - 0.9 = 0.1
        // Final score = (1.0 + 0.1) / 2.0 = 0.55
        var expectedScore = 0.55;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Use_Default_Values_For_Missing_Metrics()
    {
        // Arrange - Only provide some metrics, others should use defaults
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.1 // Only provide ErrorRate
            // CpuUtilization and MemoryUtilization will use defaults (0.5 each)
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: errorScore = 1.0 - min(0.1 * 2, 1.0) = 1.0 - 0.2 = 0.8
        // resourceScore = 1.0 - max(0.5, 0.5) = 1.0 - 0.5 = 0.5
        // Final score = (0.8 + 0.5) / 2.0 = 0.65
        var expectedScore = 0.65;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Not_Exceed_1_0_Or_Fall_Below_0_0()
    {
        // Arrange - Test with extremely high error rate
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 10.0, // Very high error rate (should be clamped)
            ["CpuUtilization"] = 1.0,
            ["MemoryUtilization"] = 1.0
        };

        // Act
        var score = _scorer.CalculateScore(metrics);

        // Assert
        Assert.True(score >= 0.0);
        Assert.True(score <= 1.0);

        // Even with high error rate, the errorScore calculation clamps at 1.0
        // errorScore = 1.0 - min(10.0 * 2, 1.0) = 1.0 - 1.0 = 0.0
        // resourceScore = 1.0 - max(1.0, 1.0) = 0.0
        // Final score = (0.0 + 0.0) / 2.0 = 0.0
        Assert.Equal(0.0, score, 6);
    }

    #endregion

    #region GetCriticalAreas Tests

    [Fact]
    public void GetCriticalAreas_Should_Return_Empty_When_No_Critical_Issues()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.1 // Below critical threshold (0.2)
        };

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.Empty(criticalAreas);
    }

    [Fact]
    public void GetCriticalAreas_Should_Return_Area_For_High_Error_Rate()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.25 // Above critical threshold (0.2)
        };

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.NotEmpty(criticalAreas);
        Assert.Contains("High error rate affecting maintainability", criticalAreas);
    }

    [Fact]
    public void GetCriticalAreas_Should_Use_Default_For_Missing_Error_Rate()
    {
        // Arrange
        var metrics = new Dictionary<string, double>(); // Empty - will use default error rate (0.1)

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.Empty(criticalAreas); // Default error rate of 0.1 is below threshold of 0.2
    }

    #endregion

    #region GetRecommendations Tests

    [Fact]
    public void GetRecommendations_Should_Return_Empty_When_No_Issues()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.05 // Below recommendation threshold (0.1)
        };

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.Empty(recommendations);
    }

    [Fact]
    public void GetRecommendations_Should_Return_Recommendation_For_Moderate_Error_Rate()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.15 // Above recommendation threshold (0.1)
        };

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.NotEmpty(recommendations);
        Assert.Contains("Improve code quality and testing to reduce errors", recommendations);
    }

    [Fact]
    public void GetRecommendations_Should_Use_Default_For_Missing_Error_Rate()
    {
        // Arrange
        var metrics = new Dictionary<string, double>(); // Empty - will use default error rate (0.1)

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.NotEmpty(recommendations); // Default error rate of 0.1 is at threshold
        Assert.Contains("Improve code quality and testing to reduce errors", recommendations);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void CalculateScore_Should_Handle_Negative_Metrics_Gracefully()
    {
        // Arrange - Test with negative values (shouldn't happen in practice but test robustness)
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = -0.1, // Negative value
            ["CpuUtilization"] = -0.2,
            ["MemoryUtilization"] = -0.3
        };

        // Act & Assert - Should not throw
        var score = _scorer.CalculateScore(metrics);
        Assert.True(score >= 0.0);
        Assert.True(score <= 1.0);
    }

    [Fact]
    public void CalculateScore_Should_Handle_NaN_And_Infinity_Gracefully()
    {
        // Arrange - Test with special floating point values
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = double.NaN,
            ["CpuUtilization"] = double.PositiveInfinity,
            ["MemoryUtilization"] = double.NegativeInfinity
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
            ["ErrorRate"] = 0.15, // Above recommendation threshold, below critical
            ["CpuUtilization"] = 0.6,
            ["MemoryUtilization"] = 0.4
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.True(score >= 0.0 && score <= 1.0);
        
        // Error rate of 0.15 is not high enough for critical area (needs > 0.2)
        Assert.Empty(criticalAreas);
        
        // But error rate of 0.15 triggers recommendations (needs > 0.1)
        Assert.NotEmpty(recommendations);
        Assert.Contains("Improve code quality and testing to reduce errors", recommendations);
    }

    [Fact]
    public void Scoring_With_Critical_Error_Rate_Should_Trigger_Both_Areas_And_Recommendations()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.25, // Above both thresholds (0.1 for recommendations, 0.2 for critical)
            ["CpuUtilization"] = 0.3,
            ["MemoryUtilization"] = 0.2
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.True(score >= 0.0 && score <= 1.0);
        
        // Error rate of 0.25 is high enough for critical area
        Assert.NotEmpty(criticalAreas);
        Assert.Contains("High error rate affecting maintainability", criticalAreas);
        
        // And also triggers recommendations
        Assert.NotEmpty(recommendations);
        Assert.Contains("Improve code quality and testing to reduce errors", recommendations);
    }

    #endregion
}
