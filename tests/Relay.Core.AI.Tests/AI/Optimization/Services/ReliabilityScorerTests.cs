using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class ReliabilityScorerTests
{
    private readonly ILogger _logger;
    private readonly ReliabilityScorer _scorer;

    public ReliabilityScorerTests()
    {
        _logger = NullLogger.Instance;
        _scorer = new ReliabilityScorer(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new ReliabilityScorer(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var scorer = new ReliabilityScorer(logger);

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
        Assert.Equal("ReliabilityScorer", name);
    }

    #endregion

    #region CalculateScoreCore Tests

    [Fact]
    public void CalculateScore_Should_Return_Maximum_Score_With_Perfect_Reliability()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.0, // No errors
            ["ExceptionCount"] = 0 // No exceptions
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: errorScore = 1.0 - min(0.0, 1.0) = 1.0
        // exceptionScore = max(0, 1.0 - 0/100) = 1.0
        // Final score = (1.0 + 1.0) / 2.0 = 1.0
        var expectedScore = 1.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Return_Minimum_Score_With_Worst_Reliability()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 1.0, // Maximum error rate
            ["ExceptionCount"] = 1000 // Very high exception count (will be normalized)
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: errorScore = 1.0 - min(1.0, 1.0) = 0.0
        // exceptionScore = max(0, 1.0 - 1000/100) = max(0, -9) = 0.0
        // Final score = (0.0 + 0.0) / 2.0 = 0.0
        var expectedScore = 0.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Calculate_Correctly_With_High_Error_Rate()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.8, // High error rate
            ["ExceptionCount"] = 0 // No exceptions
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: errorScore = 1.0 - min(0.8, 1.0) = 0.2
        // exceptionScore = max(0, 1.0 - 0/100) = 1.0
        // Final score = (0.2 + 1.0) / 2.0 = 0.6
        var expectedScore = 0.6;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Calculate_Correctly_With_High_Exception_Count()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.0, // No errors
            ["ExceptionCount"] = 50 // Moderate exception count
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: errorScore = 1.0 - min(0.0, 1.0) = 1.0
        // exceptionScore = max(0, 1.0 - 50/100) = max(0, 0.5) = 0.5
        // Final score = (1.0 + 0.5) / 2.0 = 0.75
        var expectedScore = 0.75;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Use_Default_Values_For_Missing_Metrics()
    {
        // Arrange - Only provide some metrics, others should use defaults
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.2
            // ExceptionCount will use default (0)
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: errorScore = 1.0 - min(0.2, 1.0) = 0.8
        // exceptionScore = max(0, 1.0 - 0/100) = 1.0 (default exception count)
        // Final score = (0.8 + 1.0) / 2.0 = 0.9
        var expectedScore = 0.9;

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

    [Fact]
    public void CalculateScore_Should_Clamp_Individual_Scores()
    {
        // Arrange - Test with very high exception count that would create negative scores
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.0,
            ["ExceptionCount"] = 500 // Would make exceptionScore negative without clamping
        };

        // Act
        var score = _scorer.CalculateScore(metrics);

        // Assert: The exceptionScore should be clamped to 0, not negative
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
            ["ErrorRate"] = 0.05 // Below critical threshold (0.1)
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
            ["ErrorRate"] = 0.15 // Above critical threshold (0.1)
        };

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.NotEmpty(criticalAreas);
        Assert.Contains("High error rate", criticalAreas);
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
        Assert.Empty(criticalAreas); // Default error rate (0.1) is at threshold but not above
    }

    #endregion

    #region GetRecommendations Tests

    [Fact]
    public void GetRecommendations_Should_Return_Empty_When_No_Issues()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.03 // Below recommendation threshold (0.05)
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
            ["ErrorRate"] = 0.08 // Above recommendation threshold (0.05)
        };

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.NotEmpty(recommendations);
        Assert.Contains("Implement better error handling and retry logic", recommendations);
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
        Assert.NotEmpty(recommendations); // Default error rate (0.1) is above threshold (0.05)
        Assert.Contains("Implement better error handling and retry logic", recommendations);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void CalculateScore_Should_Handle_Negative_Metric_Values()
    {
        // Arrange - Test with negative values
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = -0.1, // Negative value
            ["ExceptionCount"] = -5 // Negative value
        };

        // Act & Assert - Should not throw
        var score = _scorer.CalculateScore(metrics);
        Assert.True(score >= 0.0);
        Assert.True(score <= 1.0);
    }

    [Fact]
    public void CalculateScore_Should_Handle_NaN_And_Infinity_Gracefully()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = double.NaN,
            ["ExceptionCount"] = double.PositiveInfinity
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
            ["ErrorRate"] = 0.08, // Above recommendation but below critical
            ["ExceptionCount"] = 10
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.True(score >= 0.0 && score <= 1.0);
        
        // Error rate is not high enough for critical areas
        Assert.Empty(criticalAreas);
        
        // But it is high enough for recommendations
        Assert.NotEmpty(recommendations);
        Assert.Contains("Implement better error handling and retry logic", recommendations);
    }

    [Fact]
    public void Scoring_With_Critical_Error_Rate_Should_Trigger_Both_Areas_And_Recommendations()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["ErrorRate"] = 0.15, // Above both thresholds (0.05 for recommendations, 0.1 for critical)
            ["ExceptionCount"] = 5
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.True(score >= 0.0 && score <= 1.0);
        
        // Error rate is high enough for critical areas
        Assert.NotEmpty(criticalAreas);
        Assert.Contains("High error rate", criticalAreas);
        
        // And also trigger recommendations
        Assert.NotEmpty(recommendations);
        Assert.Contains("Implement better error handling and retry logic", recommendations);
    }

    #endregion
}
