using Relay.CLI.Commands.Models.Validation;

namespace Relay.CLI.Tests.Commands;

public class ValidationResultTests
{
    [Fact]
    public void ValidationResult_ShouldHaveTypeProperty()
    {
        // Arrange & Act
        var result = new ValidationResult { Type = "Handler Pattern" };

        // Assert
        Assert.Equal("Handler Pattern", result.Type);
    }

    [Fact]
    public void ValidationResult_ShouldHaveStatusProperty()
    {
        // Arrange & Act
        var result = new ValidationResult { Status = ValidationStatus.Pass };

        // Assert
        Assert.Equal(ValidationStatus.Pass, result.Status);
    }

    [Fact]
    public void ValidationResult_ShouldHaveMessageProperty()
    {
        // Arrange & Act
        var result = new ValidationResult { Message = "Validation successful" };

        // Assert
        Assert.Equal("Validation successful", result.Message);
    }

    [Fact]
    public void ValidationResult_ShouldHaveSeverityProperty()
    {
        // Arrange & Act
        var result = new ValidationResult { Severity = ValidationSeverity.High };

        // Assert
        Assert.Equal(ValidationSeverity.High, result.Severity);
    }

    [Fact]
    public void ValidationResult_ShouldHaveSuggestionProperty()
    {
        // Arrange & Act
        var result = new ValidationResult { Suggestion = "Use ValueTask instead of Task" };

        // Assert
        Assert.Equal("Use ValueTask instead of Task", result.Suggestion);
    }

    [Fact]
    public void ValidationResult_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var result = new ValidationResult();

        // Assert
        Assert.Equal("", result.Type);
        Assert.Equal(ValidationStatus.Pass, result.Status);
        Assert.Equal("", result.Message);
        Assert.Equal(ValidationSeverity.Info, result.Severity);
        Assert.Null(result.Suggestion);
    }

    [Fact]
    public void ValidationResult_CanSetAllPropertiesViaInitializer()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = "Handler Pattern",
            Status = ValidationStatus.Warning,
            Message = "Handler uses Task instead of ValueTask",
            Severity = ValidationSeverity.Medium,
            Suggestion = "Consider using ValueTask<T> for better performance"
        };

        // Assert
        Assert.Equal("Handler Pattern", result.Type);
        Assert.Equal(ValidationStatus.Warning, result.Status);
        Assert.Contains("ValueTask", result.Message);
        Assert.Equal(ValidationSeverity.Medium, result.Severity);
        Assert.Contains("performance", result.Suggestion);
    }

    [Theory]
    [InlineData(ValidationStatus.Pass)]
    [InlineData(ValidationStatus.Warning)]
    [InlineData(ValidationStatus.Fail)]
    public void ValidationResult_ShouldSupportAllValidationStatuses(ValidationStatus status)
    {
        // Arrange & Act
        var result = new ValidationResult { Status = status };

        // Assert
        Assert.Equal(status, result.Status);
    }

    [Theory]
    [InlineData(ValidationSeverity.Info)]
    [InlineData(ValidationSeverity.Low)]
    [InlineData(ValidationSeverity.Medium)]
    [InlineData(ValidationSeverity.High)]
    [InlineData(ValidationSeverity.Critical)]
    public void ValidationResult_ShouldSupportAllSeverityLevels(ValidationSeverity severity)
    {
        // Arrange & Act
        var result = new ValidationResult { Severity = severity };

        // Assert
        Assert.Equal(severity, result.Severity);
    }

    [Fact]
    public void ValidationResult_PassStatus_WithInfoSeverity()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = "Package Reference",
            Status = ValidationStatus.Pass,
            Message = "Relay package found",
            Severity = ValidationSeverity.Info
        };

        // Assert
        Assert.Equal(ValidationStatus.Pass, result.Status);
        Assert.Equal(ValidationSeverity.Info, result.Severity);
    }

    [Fact]
    public void ValidationResult_WarningStatus_WithMediumSeverity()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = "Handler Pattern",
            Status = ValidationStatus.Warning,
            Message = "Handler missing CancellationToken",
            Severity = ValidationSeverity.Medium
        };

        // Assert
        Assert.Equal(ValidationStatus.Warning, result.Status);
        Assert.Equal(ValidationSeverity.Medium, result.Severity);
    }

    [Fact]
    public void ValidationResult_FailStatus_WithCriticalSeverity()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = "Package Reference",
            Status = ValidationStatus.Fail,
            Message = "No Relay package found",
            Severity = ValidationSeverity.Critical,
            Suggestion = "Add Relay.Core package: dotnet add package Relay.Core"
        };

        // Assert
        Assert.Equal(ValidationStatus.Fail, result.Status);
        Assert.Equal(ValidationSeverity.Critical, result.Severity);
        Assert.NotNull(result.Suggestion);
        Assert.NotEmpty(result.Suggestion);
    }

    [Fact]
    public void ValidationResult_Suggestion_CanBeNull()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = "Code Quality",
            Status = ValidationStatus.Pass,
            Message = "All checks passed"
        };

        // Assert
        Assert.Null(result.Suggestion);
    }

    [Fact]
    public void ValidationResult_Suggestion_CanBeEmpty()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Suggestion = ""
        };

        // Assert
        Assert.Empty(result.Suggestion);
    }

    [Fact]
    public void ValidationResult_TypeProperty_CanDescribeVariousCategories()
    {
        // Arrange
        var types = new[]
        {
            "Package Reference",
            "Handler Pattern",
            "Request Pattern",
            "Code Quality",
            "Configuration",
            "DI Registration",
            "Handlers",
            "Requests"
        };

        foreach (var type in types)
        {
            // Act
            var result = new ValidationResult { Type = type };

            // Assert
            Assert.Equal(type, result.Type);
        }
    }

    [Fact]
    public void ValidationResult_MessageProperty_CanContainDetailedInformation()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Message = "CreateUserHandler in CreateUser.cs uses Task instead of ValueTask"
        };

        // Assert
        Assert.Contains("CreateUserHandler", result.Message);
        Assert.Contains("CreateUser.cs", result.Message);
        Assert.Contains("Task", result.Message);
        Assert.Contains("ValueTask", result.Message);
    }

    [Fact]
    public void ValidationResult_SuggestionProperty_CanProvideActionableGuidance()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Suggestion = "Add CancellationToken parameter to support cancellation"
        };

        // Assert
        Assert.Contains("CancellationToken", result.Suggestion);
        Assert.Contains("cancellation", result.Suggestion);
    }

    [Fact]
    public void ValidationResult_ShouldBeClass()
    {
        // Arrange & Act
        var result = new ValidationResult();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.GetType().IsClass);
    }

    [Fact]
    public void ValidationResult_CanBeUsedInList()
    {
        // Arrange & Act
        var results = new List<ValidationResult>
        {
            new() { Type = "Type1", Status = ValidationStatus.Pass },
            new() { Type = "Type2", Status = ValidationStatus.Warning },
            new() { Type = "Type3", Status = ValidationStatus.Fail }
        };

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(1, results.Count(r => r.Status == ValidationStatus.Pass));
        Assert.Equal(1, results.Count(r => r.Status == ValidationStatus.Warning));
        Assert.Equal(1, results.Count(r => r.Status == ValidationStatus.Fail));
    }

    [Fact]
    public void ValidationResult_CanBeFiltered_ByStatus()
    {
        // Arrange
        var results = new List<ValidationResult>
        {
            new() { Status = ValidationStatus.Pass },
            new() { Status = ValidationStatus.Pass },
            new() { Status = ValidationStatus.Warning },
            new() { Status = ValidationStatus.Fail }
        };

        // Act
        var failures = results.Where(r => r.Status == ValidationStatus.Fail).ToList();

        // Assert
        Assert.Single(failures);
    }

    [Fact]
    public void ValidationResult_CanBeFiltered_BySeverity()
    {
        // Arrange
        var results = new List<ValidationResult>
        {
            new() { Severity = ValidationSeverity.Info },
            new() { Severity = ValidationSeverity.Low },
            new() { Severity = ValidationSeverity.Critical },
            new() { Severity = ValidationSeverity.Critical }
        };

        // Act
        var criticalIssues = results.Where(r => r.Severity == ValidationSeverity.Critical).ToList();

        // Assert
        Assert.Equal(2, criticalIssues.Count);
    }

    [Fact]
    public void ValidationResult_PropertiesCanBeModified()
    {
        // Arrange
        var result = new ValidationResult
        {
            Type = "Initial",
            Status = ValidationStatus.Pass,
            Message = "Initial message"
        };

        // Act
        result.Type = "Modified";
        result.Status = ValidationStatus.Fail;
        result.Message = "Modified message";

        // Assert
        Assert.Equal("Modified", result.Type);
        Assert.Equal(ValidationStatus.Fail, result.Status);
        Assert.Equal("Modified message", result.Message);
    }

    [Fact]
    public void ValidationResult_WithHandlerValidation_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = "Handler Pattern",
            Status = ValidationStatus.Warning,
            Message = "CreateUserHandler uses Task instead of ValueTask",
            Severity = ValidationSeverity.Medium,
            Suggestion = "Consider using ValueTask<T> for better performance"
        };

        // Assert
        Assert.Equal("Handler Pattern", result.Type);
        Assert.Equal(ValidationStatus.Warning, result.Status);
        Assert.Contains("Task", result.Message);
        Assert.Equal(ValidationSeverity.Medium, result.Severity);
        Assert.NotNull(result.Suggestion);
    }

    [Fact]
    public void ValidationResult_WithProjectFileValidation_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = "Package Reference",
            Status = ValidationStatus.Pass,
            Message = "Relay package found in MyProject.csproj",
            Severity = ValidationSeverity.Info
        };

        // Assert
        Assert.Equal("Package Reference", result.Type);
        Assert.Equal(ValidationStatus.Pass, result.Status);
        Assert.Contains("Relay package", result.Message);
        Assert.Equal(ValidationSeverity.Info, result.Severity);
    }

    [Fact]
    public void ValidationResult_WithDIRegistrationValidation_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = "DI Registration",
            Status = ValidationStatus.Fail,
            Message = "No AddRelay() registration found",
            Severity = ValidationSeverity.High,
            Suggestion = "Add services.AddRelay() in your DI configuration"
        };

        // Assert
        Assert.Equal("DI Registration", result.Type);
        Assert.Equal(ValidationStatus.Fail, result.Status);
        Assert.Equal(ValidationSeverity.High, result.Severity);
        Assert.Contains("AddRelay", result.Suggestion);
    }

    [Fact]
    public void ValidationResult_WithConfigurationValidation_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = "Configuration",
            Status = ValidationStatus.Pass,
            Message = "Relay CLI configuration found and valid",
            Severity = ValidationSeverity.Info
        };

        // Assert
        Assert.Equal("Configuration", result.Type);
        Assert.Equal(ValidationStatus.Pass, result.Status);
        Assert.Contains("configuration", result.Message);
        Assert.Equal(ValidationSeverity.Info, result.Severity);
    }

    [Fact]
    public void ValidationResult_WithCodeQualityValidation_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = "Code Quality",
            Status = ValidationStatus.Warning,
            Message = "Nullable reference types not enabled in MyProject.csproj",
            Severity = ValidationSeverity.Medium
        };

        // Assert
        Assert.Equal("Code Quality", result.Type);
        Assert.Equal(ValidationStatus.Warning, result.Status);
        Assert.Contains("Nullable", result.Message);
        Assert.Equal(ValidationSeverity.Medium, result.Severity);
    }

    [Fact]
    public void ValidationResult_MessageCanBeMultiline()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Message = "Handler validation failed:\n- Missing CancellationToken\n- Using Task instead of ValueTask"
        };

        // Assert
        Assert.Contains("Handler validation failed", result.Message);
        Assert.Contains("Missing CancellationToken", result.Message);
        Assert.Contains("Using Task instead of ValueTask", result.Message);
    }

    [Fact]
    public void ValidationResult_SuggestionCanBeMultiline()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Suggestion = "To fix this issue:\n1. Add CancellationToken parameter\n2. Use ValueTask<T> return type"
        };

        // Assert
        Assert.Contains("To fix this issue", result.Suggestion);
        Assert.Contains("Add CancellationToken", result.Suggestion);
        Assert.Contains("Use ValueTask<T>", result.Suggestion);
    }

    [Theory]
    [InlineData("Package Reference", ValidationStatus.Pass, "Relay package found", ValidationSeverity.Info)]
    [InlineData("Handler Pattern", ValidationStatus.Warning, "Missing CancellationToken", ValidationSeverity.Medium)]
    [InlineData("DI Registration", ValidationStatus.Fail, "No AddRelay() found", ValidationSeverity.Critical)]
    public void ValidationResult_WithVariousScenarios_ShouldStoreCorrectValues(
        string type,
        ValidationStatus status,
        string message,
        ValidationSeverity severity)
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = type,
            Status = status,
            Message = message,
            Severity = severity
        };

        // Assert
        Assert.Equal(type, result.Type);
        Assert.Equal(status, result.Status);
        Assert.Equal(message, result.Message);
        Assert.Equal(severity, result.Severity);
    }

    [Fact]
    public void ValidationResult_StatusAndSeverity_ShouldBeIndependent()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Status = ValidationStatus.Warning,
            Severity = ValidationSeverity.Low
        };

        // Assert
        Assert.Equal(ValidationStatus.Warning, result.Status);
        Assert.Equal(ValidationSeverity.Low, result.Severity);
    }

    [Fact]
    public void ValidationResult_CanRepresentSuccess()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = "Handlers",
            Status = ValidationStatus.Pass,
            Message = "Found 5 handlers: all optimal",
            Severity = ValidationSeverity.Info
        };

        // Assert
        Assert.Equal(ValidationStatus.Pass, result.Status);
    }

    [Fact]
    public void ValidationResult_CanRepresentWarning()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = "Request Pattern",
            Status = ValidationStatus.Warning,
            Message = "Request uses class instead of record",
            Severity = ValidationSeverity.Low,
            Suggestion = "Consider using 'record' for immutable request objects"
        };

        // Assert
        Assert.Equal(ValidationStatus.Warning, result.Status);
        Assert.NotNull(result.Suggestion);
    }

    [Fact]
    public void ValidationResult_CanRepresentFailure()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = "Package Reference",
            Status = ValidationStatus.Fail,
            Message = "No Relay package references found in any project",
            Severity = ValidationSeverity.Critical,
            Suggestion = "Add Relay.Core package: dotnet add package Relay.Core"
        };

        // Assert
        Assert.Equal(ValidationStatus.Fail, result.Status);
        Assert.Equal(ValidationSeverity.Critical, result.Severity);
    }

    [Fact]
    public void ValidationResult_TypeCanContainSpaces()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Type = "Handler Pattern Violation"
        };

        // Assert
        Assert.Equal("Handler Pattern Violation", result.Type);
    }

    [Fact]
    public void ValidationResult_MessageCanContainSpecialCharacters()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Message = "Handler 'CreateUser' -> missing [Handle] attribute"
        };

        // Assert
        Assert.Contains("'CreateUser'", result.Message);
        Assert.Contains("->", result.Message);
        Assert.Contains("[Handle]", result.Message);
    }

    [Fact]
    public void ValidationResult_SuggestionCanContainCodeSnippets()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Suggestion = "Add services.AddRelay() in your DI configuration"
        };

        // Assert
        Assert.Contains("AddRelay()", result.Suggestion);
    }

    [Fact]
    public void ValidationResult_CanBeGroupedByType()
    {
        // Arrange
        var results = new List<ValidationResult>
        {
            new() { Type = "Handlers" },
            new() { Type = "Handlers" },
            new() { Type = "Requests" },
            new() { Type = "Configuration" }
        };

        // Act
        var grouped = results.GroupBy(r => r.Type);

        // Assert
        Assert.Equal(3, grouped.Count());
        Assert.Equal(2, grouped.First(g => g.Key == "Handlers").Count());
    }

    [Fact]
    public void ValidationResult_CanBeOrdered_BySeverity()
    {
        // Arrange
        var results = new List<ValidationResult>
        {
            new() { Severity = ValidationSeverity.Low },
            new() { Severity = ValidationSeverity.Critical },
            new() { Severity = ValidationSeverity.Medium }
        };

        // Act
        var ordered = results.OrderByDescending(r => r.Severity).ToList();

        // Assert
        Assert.Equal(ValidationSeverity.Critical, ordered[0].Severity);
        Assert.Equal(ValidationSeverity.Medium, ordered[1].Severity);
        Assert.Equal(ValidationSeverity.Low, ordered[2].Severity);
    }
}
