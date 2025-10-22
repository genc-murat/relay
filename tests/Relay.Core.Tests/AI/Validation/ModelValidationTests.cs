using Relay.Core.AI;
using System;
using System.Threading;
using Xunit;

namespace Relay.Core.Tests.AI.Validation;

public class ModelValidationTests
{
    [Fact]
    public void ModelValidationIssue_DefaultConstructor_InitializesProperties()
    {
        // Act
        var issue = new ModelValidationIssue();

        // Assert
        Assert.Equal(default(ModelIssueType), issue.Type);
        Assert.Equal(default(ValidationSeverity), issue.Severity);
        Assert.Equal(string.Empty, issue.Description);
        Assert.Equal(string.Empty, issue.RecommendedAction);
    }

    [Fact]
    public void ModelValidationIssue_Properties_CanBeSet()
    {
        // Arrange
        var issue = new ModelValidationIssue();

        // Act
        issue.Type = ModelIssueType.LowAccuracy;
        issue.Severity = ValidationSeverity.Error;
        issue.Description = "Model accuracy is below threshold";
        issue.RecommendedAction = "Retrain the model with more data";

        // Assert
        Assert.Equal(ModelIssueType.LowAccuracy, issue.Type);
        Assert.Equal(ValidationSeverity.Error, issue.Severity);
        Assert.Equal("Model accuracy is below threshold", issue.Description);
        Assert.Equal("Retrain the model with more data", issue.RecommendedAction);
    }

    [Fact]
    public void ModelValidationIssue_CanBeUsedInCollections()
    {
        // Arrange
        var issues = new System.Collections.Generic.List<ModelValidationIssue>
        {
            new ModelValidationIssue
            {
                Type = ModelIssueType.LowAccuracy,
                Severity = ValidationSeverity.Error,
                Description = "Low accuracy",
                RecommendedAction = "Retrain"
            },
            new ModelValidationIssue
            {
                Type = ModelIssueType.InsufficientData,
                Severity = ValidationSeverity.Warning,
                Description = "Not enough data",
                RecommendedAction = "Collect more data"
            }
        };

        // Assert
        Assert.Equal(2, issues.Count);
        Assert.Contains(issues, i => i.Type == ModelIssueType.LowAccuracy);
        Assert.Contains(issues, i => i.Type == ModelIssueType.InsufficientData);
    }

    [Fact]
    public void ValidationSeverity_EnumValues_AreDefined()
    {
        // Assert
        Assert.Equal(0, (int)ValidationSeverity.Success);
        Assert.Equal(1, (int)ValidationSeverity.Warning);
        Assert.Equal(2, (int)ValidationSeverity.Error);
    }

    [Fact]
    public void ValidationSeverity_AllValues_CanBeParsed()
    {
        // Act & Assert
        Assert.True(Enum.TryParse("Success", out ValidationSeverity success));
        Assert.Equal(ValidationSeverity.Success, success);

        Assert.True(Enum.TryParse("Warning", out ValidationSeverity warning));
        Assert.Equal(ValidationSeverity.Warning, warning);

        Assert.True(Enum.TryParse("Error", out ValidationSeverity error));
        Assert.Equal(ValidationSeverity.Error, error);
    }

    [Fact]
    public void ModelIssueType_EnumValues_AreDefined()
    {
        // Assert
        Assert.Equal(0, (int)ModelIssueType.LowAccuracy);
        Assert.Equal(1, (int)ModelIssueType.InconsistentPredictions);
        Assert.Equal(2, (int)ModelIssueType.InsufficientData);
        Assert.Equal(3, (int)ModelIssueType.StaleModel);
        Assert.Equal(4, (int)ModelIssueType.SlowPredictions);
    }

    [Fact]
    public void ModelIssueType_AllValues_CanBeParsed()
    {
        // Act & Assert
        Assert.True(Enum.TryParse("LowAccuracy", out ModelIssueType lowAccuracy));
        Assert.Equal(ModelIssueType.LowAccuracy, lowAccuracy);

        Assert.True(Enum.TryParse("InconsistentPredictions", out ModelIssueType inconsistent));
        Assert.Equal(ModelIssueType.InconsistentPredictions, inconsistent);

        Assert.True(Enum.TryParse("InsufficientData", out ModelIssueType insufficient));
        Assert.Equal(ModelIssueType.InsufficientData, insufficient);

        Assert.True(Enum.TryParse("StaleModel", out ModelIssueType stale));
        Assert.Equal(ModelIssueType.StaleModel, stale);

        Assert.True(Enum.TryParse("SlowPredictions", out ModelIssueType slow));
        Assert.Equal(ModelIssueType.SlowPredictions, slow);
    }

    [Fact]
    public void ValidationRules_DefaultConstructor_InitializesProperties()
    {
        // Act
        var rules = new ValidationRules();

        // Assert
        Assert.Equal(0.0, rules.MinConfidence);
        Assert.Equal(default(RiskLevel), rules.MaxRisk);
        Assert.Empty(rules.RequiredParameters);
        Assert.Null(rules.CustomValidation);
    }

    [Fact]
    public void ValidationRules_Properties_CanBeSet()
    {
        // Arrange
        var rules = new ValidationRules();
        var requiredParams = new[] { "param1", "param2" };

        // Act
        rules.MinConfidence = 0.8;
        rules.MaxRisk = RiskLevel.Medium;
        rules.RequiredParameters = requiredParams;
        rules.CustomValidation = async (rec, type, token) => new StrategyValidationResult
        {
            Errors = new System.Collections.Generic.List<string>(),
            Warnings = new System.Collections.Generic.List<string>()
        };

        // Assert
        Assert.Equal(0.8, rules.MinConfidence);
        Assert.Equal(RiskLevel.Medium, rules.MaxRisk);
        Assert.Equal(requiredParams, rules.RequiredParameters);
        Assert.NotNull(rules.CustomValidation);
    }

    [Fact]
    public void ValidationRules_CustomValidation_CanBeExecuted()
    {
        // Arrange
        var rules = new ValidationRules();
        var executed = false;

        rules.CustomValidation = async (rec, type, token) =>
        {
            executed = true;
            return new StrategyValidationResult
            {
                Errors = new System.Collections.Generic.List<string>(),
                Warnings = new System.Collections.Generic.List<string> { "Test warning" }
            };
        };

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            Priority = OptimizationPriority.High,
            Reasoning = "Test reasoning"
        };

        // Act
        var result = rules.CustomValidation!(recommendation, typeof(string), CancellationToken.None).Result;

        // Assert
        Assert.True(executed);
        Assert.Empty(result.Errors);
        Assert.Contains("Test warning", result.Warnings);
    }

    [Fact]
    public void ValidationRules_RequiredParameters_CanBeModified()
    {
        // Arrange
        var rules = new ValidationRules();

        // Act
        rules.RequiredParameters = new[] { "param1", "param2", "param3" };

        // Assert
        Assert.Equal(3, rules.RequiredParameters.Length);
        Assert.Contains("param1", rules.RequiredParameters);
        Assert.Contains("param2", rules.RequiredParameters);
        Assert.Contains("param3", rules.RequiredParameters);
    }

    [Fact]
    public void ValidationRules_EmptyRequiredParameters_WorksCorrectly()
    {
        // Arrange
        var rules = new ValidationRules();

        // Act
        rules.RequiredParameters = Array.Empty<string>();

        // Assert
        Assert.Empty(rules.RequiredParameters);
    }

    [Fact]
    public void ModelValidationIssue_Equality_WorksByReference()
    {
        // Arrange
        var issue1 = new ModelValidationIssue
        {
            Type = ModelIssueType.LowAccuracy,
            Severity = ValidationSeverity.Error
        };
        var issue2 = new ModelValidationIssue
        {
            Type = ModelIssueType.LowAccuracy,
            Severity = ValidationSeverity.Error
        };

        // Assert - Different instances with same values are not equal by reference
        Assert.NotSame(issue1, issue2);
        Assert.Equal(issue1.Type, issue2.Type);
        Assert.Equal(issue1.Severity, issue2.Severity);
    }

    [Fact]
    public void ValidationRules_ImmutableProperties_WorkCorrectly()
    {
        // Arrange
        var rules1 = new ValidationRules { MinConfidence = 0.5 };
        var rules2 = new ValidationRules { MinConfidence = 0.7 };

        // Act
        rules1.MinConfidence = 0.9;

        // Assert
        Assert.Equal(0.9, rules1.MinConfidence);
        Assert.Equal(0.7, rules2.MinConfidence);
    }
}