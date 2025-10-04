using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class ValidationResultTests
{
    [Fact]
    public void ValidationResult_ShouldHaveTypeProperty()
    {
        // Arrange & Act
        var result = new ValidationResult { Type = "Handler Pattern" };

        // Assert
        result.Type.Should().Be("Handler Pattern");
    }

    [Fact]
    public void ValidationResult_ShouldHaveStatusProperty()
    {
        // Arrange & Act
        var result = new ValidationResult { Status = ValidationStatus.Pass };

        // Assert
        result.Status.Should().Be(ValidationStatus.Pass);
    }

    [Fact]
    public void ValidationResult_ShouldHaveMessageProperty()
    {
        // Arrange & Act
        var result = new ValidationResult { Message = "Validation successful" };

        // Assert
        result.Message.Should().Be("Validation successful");
    }

    [Fact]
    public void ValidationResult_ShouldHaveSeverityProperty()
    {
        // Arrange & Act
        var result = new ValidationResult { Severity = ValidationSeverity.High };

        // Assert
        result.Severity.Should().Be(ValidationSeverity.High);
    }

    [Fact]
    public void ValidationResult_ShouldHaveSuggestionProperty()
    {
        // Arrange & Act
        var result = new ValidationResult { Suggestion = "Use ValueTask instead of Task" };

        // Assert
        result.Suggestion.Should().Be("Use ValueTask instead of Task");
    }

    [Fact]
    public void ValidationResult_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var result = new ValidationResult();

        // Assert
        result.Type.Should().Be("");
        result.Status.Should().Be(ValidationStatus.Pass);
        result.Message.Should().Be("");
        result.Severity.Should().Be(ValidationSeverity.Info);
        result.Suggestion.Should().BeNull();
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
        result.Type.Should().Be("Handler Pattern");
        result.Status.Should().Be(ValidationStatus.Warning);
        result.Message.Should().Contain("ValueTask");
        result.Severity.Should().Be(ValidationSeverity.Medium);
        result.Suggestion.Should().Contain("performance");
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
        result.Status.Should().Be(status);
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
        result.Severity.Should().Be(severity);
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
        result.Status.Should().Be(ValidationStatus.Pass);
        result.Severity.Should().Be(ValidationSeverity.Info);
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
        result.Status.Should().Be(ValidationStatus.Warning);
        result.Severity.Should().Be(ValidationSeverity.Medium);
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
        result.Status.Should().Be(ValidationStatus.Fail);
        result.Severity.Should().Be(ValidationSeverity.Critical);
        result.Suggestion.Should().NotBeNullOrEmpty();
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
        result.Suggestion.Should().BeNull();
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
        result.Suggestion.Should().BeEmpty();
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
            result.Type.Should().Be(type);
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
        result.Message.Should().Contain("CreateUserHandler");
        result.Message.Should().Contain("CreateUser.cs");
        result.Message.Should().Contain("Task");
        result.Message.Should().Contain("ValueTask");
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
        result.Suggestion.Should().Contain("CancellationToken");
        result.Suggestion.Should().Contain("cancellation");
    }

    [Fact]
    public void ValidationResult_ShouldBeClass()
    {
        // Arrange & Act
        var result = new ValidationResult();

        // Assert
        result.Should().NotBeNull();
        result.GetType().IsClass.Should().BeTrue();
    }

    [Fact]
    public void ValidationResult_CanBeUsedInList()
    {
        // Arrange & Act
        var results = new List<ValidationResult>
        {
            new ValidationResult { Type = "Type1", Status = ValidationStatus.Pass },
            new ValidationResult { Type = "Type2", Status = ValidationStatus.Warning },
            new ValidationResult { Type = "Type3", Status = ValidationStatus.Fail }
        };

        // Assert
        results.Should().HaveCount(3);
        results.Count(r => r.Status == ValidationStatus.Pass).Should().Be(1);
        results.Count(r => r.Status == ValidationStatus.Warning).Should().Be(1);
        results.Count(r => r.Status == ValidationStatus.Fail).Should().Be(1);
    }

    [Fact]
    public void ValidationResult_CanBeFiltered_ByStatus()
    {
        // Arrange
        var results = new List<ValidationResult>
        {
            new ValidationResult { Status = ValidationStatus.Pass },
            new ValidationResult { Status = ValidationStatus.Pass },
            new ValidationResult { Status = ValidationStatus.Warning },
            new ValidationResult { Status = ValidationStatus.Fail }
        };

        // Act
        var failures = results.Where(r => r.Status == ValidationStatus.Fail).ToList();

        // Assert
        failures.Should().HaveCount(1);
    }

    [Fact]
    public void ValidationResult_CanBeFiltered_BySeverity()
    {
        // Arrange
        var results = new List<ValidationResult>
        {
            new ValidationResult { Severity = ValidationSeverity.Info },
            new ValidationResult { Severity = ValidationSeverity.Low },
            new ValidationResult { Severity = ValidationSeverity.Critical },
            new ValidationResult { Severity = ValidationSeverity.Critical }
        };

        // Act
        var criticalIssues = results.Where(r => r.Severity == ValidationSeverity.Critical).ToList();

        // Assert
        criticalIssues.Should().HaveCount(2);
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
        result.Type.Should().Be("Modified");
        result.Status.Should().Be(ValidationStatus.Fail);
        result.Message.Should().Be("Modified message");
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
        result.Type.Should().Be("Handler Pattern");
        result.Status.Should().Be(ValidationStatus.Warning);
        result.Message.Should().Contain("Task");
        result.Severity.Should().Be(ValidationSeverity.Medium);
        result.Suggestion.Should().NotBeNull();
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
        result.Type.Should().Be("Package Reference");
        result.Status.Should().Be(ValidationStatus.Pass);
        result.Message.Should().Contain("Relay package");
        result.Severity.Should().Be(ValidationSeverity.Info);
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
        result.Type.Should().Be("DI Registration");
        result.Status.Should().Be(ValidationStatus.Fail);
        result.Severity.Should().Be(ValidationSeverity.High);
        result.Suggestion.Should().Contain("AddRelay");
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
        result.Type.Should().Be("Configuration");
        result.Status.Should().Be(ValidationStatus.Pass);
        result.Message.Should().Contain("configuration");
        result.Severity.Should().Be(ValidationSeverity.Info);
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
        result.Type.Should().Be("Code Quality");
        result.Status.Should().Be(ValidationStatus.Warning);
        result.Message.Should().Contain("Nullable");
        result.Severity.Should().Be(ValidationSeverity.Medium);
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
        result.Message.Should().Contain("Handler validation failed");
        result.Message.Should().Contain("Missing CancellationToken");
        result.Message.Should().Contain("Using Task instead of ValueTask");
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
        result.Suggestion.Should().Contain("To fix this issue");
        result.Suggestion.Should().Contain("Add CancellationToken");
        result.Suggestion.Should().Contain("Use ValueTask<T>");
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
        result.Type.Should().Be(type);
        result.Status.Should().Be(status);
        result.Message.Should().Be(message);
        result.Severity.Should().Be(severity);
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
        result.Status.Should().Be(ValidationStatus.Warning);
        result.Severity.Should().Be(ValidationSeverity.Low);
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
        result.Status.Should().Be(ValidationStatus.Pass);
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
        result.Status.Should().Be(ValidationStatus.Warning);
        result.Suggestion.Should().NotBeNull();
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
        result.Status.Should().Be(ValidationStatus.Fail);
        result.Severity.Should().Be(ValidationSeverity.Critical);
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
        result.Type.Should().Be("Handler Pattern Violation");
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
        result.Message.Should().Contain("'CreateUser'");
        result.Message.Should().Contain("->");
        result.Message.Should().Contain("[Handle]");
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
        result.Suggestion.Should().Contain("AddRelay()");
    }

    [Fact]
    public void ValidationResult_CanBeGroupedByType()
    {
        // Arrange
        var results = new List<ValidationResult>
        {
            new ValidationResult { Type = "Handlers" },
            new ValidationResult { Type = "Handlers" },
            new ValidationResult { Type = "Requests" },
            new ValidationResult { Type = "Configuration" }
        };

        // Act
        var grouped = results.GroupBy(r => r.Type);

        // Assert
        grouped.Should().HaveCount(3);
        grouped.First(g => g.Key == "Handlers").Should().HaveCount(2);
    }

    [Fact]
    public void ValidationResult_CanBeOrdered_BySeverity()
    {
        // Arrange
        var results = new List<ValidationResult>
        {
            new ValidationResult { Severity = ValidationSeverity.Low },
            new ValidationResult { Severity = ValidationSeverity.Critical },
            new ValidationResult { Severity = ValidationSeverity.Medium }
        };

        // Act
        var ordered = results.OrderByDescending(r => r.Severity).ToList();

        // Assert
        ordered[0].Severity.Should().Be(ValidationSeverity.Critical);
        ordered[1].Severity.Should().Be(ValidationSeverity.Medium);
        ordered[2].Severity.Should().Be(ValidationSeverity.Low);
    }
}
