using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class SecurityScorerTests
{
    private readonly ILogger _logger;
    private readonly SecurityScorer _scorer;

    public SecurityScorerTests()
    {
        _logger = NullLogger.Instance;
        _scorer = new SecurityScorer(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new SecurityScorer(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var scorer = new SecurityScorer(logger);

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
        Assert.Equal("SecurityScorer", name);
    }

    #endregion

    #region CalculateScoreCore Tests

    [Fact]
    public void CalculateScore_Should_Return_Maximum_Score_With_Perfect_Security()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["FailedAuthAttempts"] = 0, // No failed attempts
            ["KnownVulnerabilities"] = 0, // No known vulnerabilities
            ["DataEncryptionEnabled"] = 1 // Encryption enabled
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: authScore = 1.0 - (0/100) = 1.0
        // vulnScore = 1.0 - (0/10) = 1.0
        // dataEncryption = 1
        // Final score = (1.0 + 1.0 + 1) / 3.0 = 1.0
        var expectedScore = 1.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Return_Minimum_Score_With_Worst_Security()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["FailedAuthAttempts"] = 1000, // Very high failed attempts
            ["KnownVulnerabilities"] = 100, // Many known vulnerabilities
            ["DataEncryptionEnabled"] = 0 // Encryption disabled
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: authScore = max(0, 1.0 - (1000/100)) = max(0, -9) = 0
        // vulnScore = max(0, 1.0 - (100/10)) = max(0, -9) = 0
        // dataEncryption = 0
        // Final score = (0 + 0 + 0) / 3.0 = 0.0
        var expectedScore = 0.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Calculate_Correctly_With_Moderate_Failed_Auth_Attempts()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["FailedAuthAttempts"] = 50, // Moderate failed attempts
            ["KnownVulnerabilities"] = 0, // No known vulnerabilities
            ["DataEncryptionEnabled"] = 1 // Encryption enabled
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: authScore = 1.0 - (50/100) = 0.5
        // vulnScore = 1.0 - (0/10) = 1.0
        // dataEncryption = 1
        // Final score = (0.5 + 1.0 + 1) / 3.0 = 0.833333
        var expectedScore = (0.5 + 1.0 + 1.0) / 3.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Calculate_Correctly_With_Multiple_Known_Vulnerabilities()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["FailedAuthAttempts"] = 0, // No failed attempts
            ["KnownVulnerabilities"] = 5, // Moderate number of vulnerabilities
            ["DataEncryptionEnabled"] = 1 // Encryption enabled
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: authScore = 1.0 - (0/100) = 1.0
        // vulnScore = 1.0 - (5/10) = 0.5
        // dataEncryption = 1
        // Final score = (1.0 + 0.5 + 1) / 3.0 = 0.833333
        var expectedScore = (1.0 + 0.5 + 1.0) / 3.0;

        // Assert
        Assert.Equal(expectedScore, score, 6);
    }

    [Fact]
    public void CalculateScore_Should_Use_Default_Values_For_Missing_Metrics()
    {
        // Arrange - Only provide some metrics, others should use defaults
        var metrics = new Dictionary<string, double>
        {
            ["FailedAuthAttempts"] = 20
            // KnownVulnerabilities and DataEncryptionEnabled will use defaults (0 and 1)
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        // Expected: authScore = 1.0 - (20/100) = 0.8
        // vulnScore = 1.0 - (0/10) = 1.0 (default vulnerability count)
        // dataEncryption = 1 (default is enabled)
        // Final score = (0.8 + 1.0 + 1) / 3.0 = 0.933333
        var expectedScore = (0.8 + 1.0 + 1.0) / 3.0;

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
        // Arrange - Test with very high values that would create negative scores
        var metrics = new Dictionary<string, double>
        {
            ["FailedAuthAttempts"] = 500, // Would make authScore negative without clamping
            ["KnownVulnerabilities"] = 100, // Would make vulnScore negative without clamping
            ["DataEncryptionEnabled"] = 0
        };

        // Act
        var score = _scorer.CalculateScore(metrics);

        // Assert: The individual scores should be clamped to 0, not negative
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
            ["FailedAuthAttempts"] = 40 // Below critical threshold (50)
        };

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.Empty(criticalAreas);
    }

    [Fact]
    public void GetCriticalAreas_Should_Return_Area_For_High_Failed_Auth_Attempts()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["FailedAuthAttempts"] = 75 // Above critical threshold (50)
        };

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.NotEmpty(criticalAreas);
        Assert.Contains("High failed authentication attempts", criticalAreas);
    }

    [Fact]
    public void GetCriticalAreas_Should_Use_Default_For_Missing_Failed_Auth_Attempts()
    {
        // Arrange
        var metrics = new Dictionary<string, double>(); // Empty - will use default (0)

        // Act
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();

        // Assert
        Assert.NotNull(criticalAreas);
        Assert.Empty(criticalAreas); // Default value (0) is below threshold (50)
    }

    #endregion

    #region GetRecommendations Tests

    [Fact]
    public void GetRecommendations_Should_Return_Empty_When_No_Issues()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["FailedAuthAttempts"] = 5 // Below recommendation threshold (10)
        };

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.Empty(recommendations);
    }

    [Fact]
    public void GetRecommendations_Should_Return_Recommendation_For_Moderate_Failed_Auth_Attempts()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["FailedAuthAttempts"] = 25 // Above recommendation threshold (10)
        };

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.NotEmpty(recommendations);
        Assert.Contains("Review authentication security measures", recommendations);
    }

    [Fact]
    public void GetRecommendations_Should_Use_Default_For_Missing_Failed_Auth_Attempts()
    {
        // Arrange
        var metrics = new Dictionary<string, double>(); // Empty - will use default (0)

        // Act
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.NotNull(recommendations);
        Assert.Empty(recommendations); // Default value (0) is below threshold (10)
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void CalculateScore_Should_Handle_Negative_Metric_Values()
    {
        // Arrange - Test with negative values
        var metrics = new Dictionary<string, double>
        {
            ["FailedAuthAttempts"] = -10, // Negative value
            ["KnownVulnerabilities"] = -5, // Negative value
            ["DataEncryptionEnabled"] = 0
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
            ["FailedAuthAttempts"] = double.NaN,
            ["KnownVulnerabilities"] = double.PositiveInfinity,
            ["DataEncryptionEnabled"] = double.NegativeInfinity
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
            ["FailedAuthAttempts"] = 15, // Above recommendation but below critical
            ["KnownVulnerabilities"] = 3,
            ["DataEncryptionEnabled"] = 1
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.True(score >= 0.0 && score <= 1.0);
        
        // Failed auth is not high enough for critical
        Assert.Empty(criticalAreas);
        
        // But it is high enough for recommendations
        Assert.NotEmpty(recommendations);
        Assert.Contains("Review authentication security measures", recommendations);
    }

    [Fact]
    public void Scoring_With_Critical_Failed_Auth_Should_Trigger_Both_Areas_And_Recommendations()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["FailedAuthAttempts"] = 75, // Above both thresholds (10 for recommendations, 50 for critical)
            ["KnownVulnerabilities"] = 2,
            ["DataEncryptionEnabled"] = 1
        };

        // Act
        var score = _scorer.CalculateScore(metrics);
        var criticalAreas = _scorer.GetCriticalAreas(metrics).ToList();
        var recommendations = _scorer.GetRecommendations(metrics).ToList();

        // Assert
        Assert.True(score >= 0.0 && score <= 1.0);
        
        // Failed auth is high enough for critical area
        Assert.NotEmpty(criticalAreas);
        Assert.Contains("High failed authentication attempts", criticalAreas);
        
        // And also trigger recommendations
        Assert.NotEmpty(recommendations);
        Assert.Contains("Review authentication security measures", recommendations);
    }

    #endregion
}
