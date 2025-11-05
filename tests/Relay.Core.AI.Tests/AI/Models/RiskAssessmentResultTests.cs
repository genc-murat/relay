using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Models;

public class RiskAssessmentResultTests
{
    [Fact]
    public void RiskAssessmentResult_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var result = new RiskAssessmentResult();

        // Assert
        Assert.Equal(RiskLevel.VeryLow, result.RiskLevel);
        Assert.Equal(0.0, result.AdjustedConfidence);
    }

    [Fact]
    public void RiskAssessmentResult_Should_Allow_Setting_RiskLevel()
    {
        // Arrange
        var result = new RiskAssessmentResult();

        // Act
        result.RiskLevel = RiskLevel.High;

        // Assert
        Assert.Equal(RiskLevel.High, result.RiskLevel);
    }

    [Fact]
    public void RiskAssessmentResult_Should_Allow_Setting_AdjustedConfidence()
    {
        // Arrange
        var result = new RiskAssessmentResult();

        // Act
        result.AdjustedConfidence = 0.85;

        // Assert
        Assert.Equal(0.85, result.AdjustedConfidence);
    }

    [Fact]
    public void RiskAssessmentResult_Should_Support_Object_Initialization()
    {
        // Arrange & Act
        var result = new RiskAssessmentResult
        {
            RiskLevel = RiskLevel.Medium,
            AdjustedConfidence = 0.75
        };

        // Assert
        Assert.Equal(RiskLevel.Medium, result.RiskLevel);
        Assert.Equal(0.75, result.AdjustedConfidence);
    }

    [Fact]
    public void RiskAssessmentResult_Should_Support_All_RiskLevels()
    {
        // Arrange
        var riskLevels = new[] { RiskLevel.VeryLow, RiskLevel.Low, RiskLevel.Medium, RiskLevel.High, RiskLevel.VeryHigh };

        // Act & Assert
        foreach (var level in riskLevels)
        {
            var result = new RiskAssessmentResult { RiskLevel = level };
            Assert.Equal(level, result.RiskLevel);
        }
    }

    [Fact]
    public void RiskAssessmentResult_Should_Support_Confidence_Range()
    {
        // Arrange
        var confidences = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 };

        // Act & Assert
        foreach (var confidence in confidences)
        {
            var result = new RiskAssessmentResult { AdjustedConfidence = confidence };
            Assert.Equal(confidence, result.AdjustedConfidence);
        }
    }

    [Fact]
    public void RiskAssessmentResult_Should_Be_Instantiable()
    {
        // Arrange & Act
        var result = new RiskAssessmentResult();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<RiskAssessmentResult>(result);
    }

    [Fact]
    public void RiskAssessmentResult_Should_Support_Collection_Operations()
    {
        // Arrange
        var results = new System.Collections.Generic.List<RiskAssessmentResult>();

        // Act
        results.Add(new RiskAssessmentResult { RiskLevel = RiskLevel.Low, AdjustedConfidence = 0.6 });
        results.Add(new RiskAssessmentResult { RiskLevel = RiskLevel.High, AdjustedConfidence = 0.8 });

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(RiskLevel.Low, results[0].RiskLevel);
        Assert.Equal(0.6, results[0].AdjustedConfidence);
        Assert.Equal(RiskLevel.High, results[1].RiskLevel);
        Assert.Equal(0.8, results[1].AdjustedConfidence);
    }

    [Fact]
    public void RiskAssessmentResult_Should_Support_Serialization()
    {
        // Arrange
        var original = new RiskAssessmentResult
        {
            RiskLevel = RiskLevel.Medium,
            AdjustedConfidence = 0.72
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(original);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<RiskAssessmentResult>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.RiskLevel, deserialized!.RiskLevel);
        Assert.Equal(original.AdjustedConfidence, deserialized.AdjustedConfidence);
    }

    [Fact]
    public void RiskAssessmentResult_Should_Have_Public_Visibility()
    {
        // Arrange
        var type = typeof(RiskAssessmentResult);

        // Assert
        Assert.True(type.IsPublic);
        Assert.False(type.IsNestedAssembly); // Public class, not nested
    }
}