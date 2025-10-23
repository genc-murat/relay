using Xunit;
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
        Assert.True(options.EnableSwagger);
        Assert.True(options.EnableDocker);
        Assert.True(options.EnableHealthChecks);
        Assert.False(options.EnableAuth);
        Assert.False(options.EnableCaching);
        Assert.False(options.EnableTelemetry);
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
            Modules = ["Module1", "Module2"]
        };

        // Assert
        Assert.Equal("Test Author", options.Author);
        Assert.Equal("net8.0", options.TargetFramework);
        Assert.Equal("postgres", options.DatabaseProvider);
        Assert.True(options.EnableAuth);
        Assert.False(options.EnableSwagger);
        Assert.False(options.EnableDocker);
        Assert.False(options.EnableHealthChecks);
        Assert.True(options.EnableCaching);
        Assert.True(options.EnableTelemetry);
        Assert.Equal(2, options.Modules.Length);
        Assert.Contains("Module1", options.Modules);
        Assert.Contains("Module2", options.Modules);
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
        Assert.Null(options.Author);
        Assert.Null(options.TargetFramework);
        Assert.Null(options.DatabaseProvider);
        Assert.Null(options.Modules);
    }

    [Fact]
    public void GenerationResult_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var result = new GenerationResult();

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Message);
        Assert.Empty(result.TemplateName);
        Assert.NotNull(result.CreatedDirectories);
        Assert.Empty(result.CreatedDirectories);
        Assert.NotNull(result.CreatedFiles);
        Assert.Empty(result.CreatedFiles);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
        Assert.Equal(TimeSpan.Zero, result.Duration);
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
        Assert.Equal(2, result.CreatedDirectories.Count);
        Assert.Contains("src", result.CreatedDirectories);
        Assert.Contains("tests", result.CreatedDirectories);
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
        Assert.Equal(2, result.CreatedFiles.Count);
        Assert.Contains("Program.cs", result.CreatedFiles);
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
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Error 1", result.Errors);
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
        Assert.True(result.Success);
        Assert.Equal("Test message", result.Message);
        Assert.Equal("relay-webapi", result.TemplateName);
        Assert.Equal(TimeSpan.FromSeconds(5), result.Duration);
        Assert.Contains("src", result.CreatedDirectories);
        Assert.Contains("Program.cs", result.CreatedFiles);
    }

    [Fact]
    public void ValidationResult_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var result = new ValidationResult();

        // Assert
        Assert.False(result.IsValid);
        Assert.Empty(result.Message);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Warnings);
        Assert.Empty(result.Warnings);
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
        Assert.Single(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains("Critical error", result.Errors);
        Assert.Contains("Minor warning", result.Warnings);
    }

    [Fact]
    public void ValidationResult_WithErrors_ShouldBeInvalid()
    {
        // Arrange
        var result = new ValidationResult { IsValid = true };

        // Act
        result.Errors.Add("Error");

        // Assert
        Assert.NotEmpty(result.Errors);
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
        result.DisplayResults();
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
        result.DisplayResults();
    }

    [Fact]
    public void PublishResult_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var result = new PublishResult();

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Message);
        Assert.Empty(result.PackagePath);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
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
        Assert.True(result.Success);
        Assert.Equal("Published successfully", result.Message);
        Assert.Equal("/path/to/package.nupkg", result.PackagePath);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void TemplateInfo_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var info = new TemplateInfo();

        // Assert
        Assert.Empty(info.Id);
        Assert.Empty(info.Name);
        Assert.Empty(info.Description);
        Assert.Empty(info.Author);
        Assert.Empty(info.Path);
        Assert.Equal("1.0.0", info.Version);
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
        Assert.Equal("relay-webapi", info.Id);
        Assert.Equal("Relay Web API", info.Name);
        Assert.Equal("A web API template", info.Description);
        Assert.Equal("Relay Team", info.Author);
        Assert.Equal("/templates/relay-webapi", info.Path);
        Assert.Equal("2.0.0", info.Version);
    }
}
