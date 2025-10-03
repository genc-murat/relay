using Xunit;
using FluentAssertions;
using Relay.CLI.TemplateEngine;

namespace Relay.CLI.Tests.TemplateEngine;

public class DataStructureTests
{
    [Fact]
    public void GenerationOptions_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new GenerationOptions();

        // Assert
        options.EnableSwagger.Should().BeTrue();
        options.EnableDocker.Should().BeTrue();
        options.EnableHealthChecks.Should().BeTrue();
        options.EnableAuth.Should().BeFalse();
        options.EnableCaching.Should().BeFalse();
        options.EnableTelemetry.Should().BeFalse();
    }

    [Fact]
    public void GenerationOptions_CanSetAllProperties()
    {
        // Arrange
        var options = new GenerationOptions
        {
            Author = "Test Author",
            TargetFramework = "net8.0",
            DatabaseProvider = "postgres",
            EnableAuth = true,
            EnableSwagger = false,
            EnableDocker = false,
            EnableHealthChecks = false,
            EnableCaching = true,
            EnableTelemetry = true,
            Modules = new[] { "Module1", "Module2" }
        };

        // Assert
        options.Author.Should().Be("Test Author");
        options.TargetFramework.Should().Be("net8.0");
        options.DatabaseProvider.Should().Be("postgres");
        options.EnableAuth.Should().BeTrue();
        options.EnableSwagger.Should().BeFalse();
        options.EnableDocker.Should().BeFalse();
        options.EnableHealthChecks.Should().BeFalse();
        options.EnableCaching.Should().BeTrue();
        options.EnableTelemetry.Should().BeTrue();
        options.Modules.Should().HaveCount(2);
        options.Modules.Should().Contain("Module1");
        options.Modules.Should().Contain("Module2");
    }

    [Fact]
    public void GenerationOptions_NullableProperties_AcceptNull()
    {
        // Arrange & Act
        var options = new GenerationOptions
        {
            Author = null,
            TargetFramework = null,
            DatabaseProvider = null,
            Modules = null
        };

        // Assert
        options.Author.Should().BeNull();
        options.TargetFramework.Should().BeNull();
        options.DatabaseProvider.Should().BeNull();
        options.Modules.Should().BeNull();
    }

    [Fact]
    public void GenerationResult_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var result = new GenerationResult();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().BeEmpty();
        result.TemplateName.Should().BeEmpty();
        result.CreatedDirectories.Should().NotBeNull().And.BeEmpty();
        result.CreatedFiles.Should().NotBeNull().And.BeEmpty();
        result.Errors.Should().NotBeNull().And.BeEmpty();
        result.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void GenerationResult_CanAddDirectories()
    {
        // Arrange
        var result = new GenerationResult();

        // Act
        result.CreatedDirectories.Add("src");
        result.CreatedDirectories.Add("tests");

        // Assert
        result.CreatedDirectories.Should().HaveCount(2);
        result.CreatedDirectories.Should().Contain("src");
        result.CreatedDirectories.Should().Contain("tests");
    }

    [Fact]
    public void GenerationResult_CanAddFiles()
    {
        // Arrange
        var result = new GenerationResult();

        // Act
        result.CreatedFiles.Add("Program.cs");
        result.CreatedFiles.Add("Startup.cs");

        // Assert
        result.CreatedFiles.Should().HaveCount(2);
        result.CreatedFiles.Should().Contain("Program.cs");
    }

    [Fact]
    public void GenerationResult_CanAddErrors()
    {
        // Arrange
        var result = new GenerationResult();

        // Act
        result.Errors.Add("Error 1");
        result.Errors.Add("Error 2");

        // Assert
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Error 1");
    }

    [Fact]
    public void GenerationResult_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var result = new GenerationResult
        {
            Success = true,
            Message = "Test message",
            TemplateName = "relay-webapi",
            Duration = TimeSpan.FromSeconds(5)
        };
        result.CreatedDirectories.Add("src");
        result.CreatedFiles.Add("Program.cs");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Test message");
        result.TemplateName.Should().Be("relay-webapi");
        result.Duration.Should().Be(TimeSpan.FromSeconds(5));
        result.CreatedDirectories.Should().Contain("src");
        result.CreatedFiles.Should().Contain("Program.cs");
    }

    [Fact]
    public void ValidationResult_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var result = new ValidationResult();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().BeEmpty();
        result.Errors.Should().NotBeNull().And.BeEmpty();
        result.Warnings.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ValidationResult_CanAddErrorsAndWarnings()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.Errors.Add("Critical error");
        result.Warnings.Add("Minor warning");

        // Assert
        result.Errors.Should().HaveCount(1);
        result.Warnings.Should().HaveCount(1);
        result.Errors.Should().Contain("Critical error");
        result.Warnings.Should().Contain("Minor warning");
    }

    [Fact]
    public void ValidationResult_WithErrors_ShouldBeInvalid()
    {
        // Arrange
        var result = new ValidationResult { IsValid = true };

        // Act
        result.Errors.Add("Error");

        // Assert
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidationResult_DisplayResults_DoesNotThrow()
    {
        // Arrange
        var result = new ValidationResult
        {
            IsValid = true,
            Message = "Success"
        };

        // Act & Assert
        var act = () => result.DisplayResults();
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidationResult_DisplayResults_WithErrorsAndWarnings_DoesNotThrow()
    {
        // Arrange
        var result = new ValidationResult
        {
            IsValid = false,
            Message = "Failed"
        };
        result.Errors.Add("Error 1");
        result.Warnings.Add("Warning 1");

        // Act & Assert
        var act = () => result.DisplayResults();
        act.Should().NotThrow();
    }

    [Fact]
    public void PublishResult_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var result = new PublishResult();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().BeEmpty();
        result.PackagePath.Should().BeEmpty();
        result.Errors.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void PublishResult_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var result = new PublishResult
        {
            Success = true,
            Message = "Published successfully",
            PackagePath = "/path/to/package.nupkg"
        };
        result.Errors.Add("Warning message");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Published successfully");
        result.PackagePath.Should().Be("/path/to/package.nupkg");
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public void TemplateInfo_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var info = new TemplateInfo();

        // Assert
        info.Id.Should().BeEmpty();
        info.Name.Should().BeEmpty();
        info.Description.Should().BeEmpty();
        info.Author.Should().BeEmpty();
        info.Path.Should().BeEmpty();
        info.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void TemplateInfo_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var info = new TemplateInfo
        {
            Id = "relay-webapi",
            Name = "Relay Web API",
            Description = "A web API template",
            Author = "Relay Team",
            Path = "/templates/relay-webapi",
            Version = "2.0.0"
        };

        // Assert
        info.Id.Should().Be("relay-webapi");
        info.Name.Should().Be("Relay Web API");
        info.Description.Should().Be("A web API template");
        info.Author.Should().Be("Relay Team");
        info.Path.Should().Be("/templates/relay-webapi");
        info.Version.Should().Be("2.0.0");
    }
}
