using Xunit;
using FluentAssertions;
using Relay.CLI.TemplateEngine;

namespace Relay.CLI.Tests.TemplateEngine;

public class TemplateValidatorTests : IDisposable
{
    private readonly TemplateValidator _validator;
    private readonly string _testDataPath;

    public TemplateValidatorTests()
    {
        _validator = new TemplateValidator();
        _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData");
        Directory.CreateDirectory(_testDataPath);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task ValidateAsync_WithValidTemplate_ReturnsSuccess()
    {
        // Arrange
        var validTemplatePath = CreateValidTemplate();

        // Act
        var result = await _validator.ValidateAsync(validTemplatePath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Message.Should().Contain("validation passed");
    }

    [Fact]
    public async Task ValidateAsync_WithMissingTemplateDirectory_ReturnsFailure()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDataPath, "NonExistent");

        // Act
        var result = await _validator.ValidateAsync(nonExistentPath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task ValidateAsync_WithMissingConfigDirectory_ReturnsFailure()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "NoConfig_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(templatePath);

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains(".template.config"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithMissingTemplateJson_ReturnsFailure()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "NoTemplateJson_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        Directory.CreateDirectory(configPath);

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("template.json"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidTemplateJson_ReturnsFailure()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "InvalidJson_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        Directory.CreateDirectory(configPath);
        
        var templateJsonPath = Path.Combine(configPath, "template.json");
        await File.WriteAllTextAsync(templateJsonPath, "{ invalid json }");

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            result.IsValid.Should().BeFalse();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithMissingRequiredFields_ReturnsErrors()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "MissingFields_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        Directory.CreateDirectory(configPath);
        
        var templateJsonPath = Path.Combine(configPath, "template.json");
        var incompleteJson = @"{
            ""author"": ""Test Author"",
            ""identity"": ""Test.Template""
        }";
        await File.WriteAllTextAsync(templateJsonPath, incompleteJson);

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("name"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public void ValidateProjectName_WithValidName_ReturnsSuccess()
    {
        // Arrange
        var validName = "MyTestProject";

        // Act
        var result = _validator.ValidateProjectName(validName);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateProjectName_WithEmptyName_ReturnsFailure()
    {
        // Arrange
        var emptyName = "";

        // Act
        var result = _validator.ValidateProjectName(emptyName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public void ValidateProjectName_WithInvalidCharacters_ReturnsFailure()
    {
        // Arrange
        var invalidName = "My<Project>";

        // Act
        var result = _validator.ValidateProjectName(invalidName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("invalid characters"));
    }

    [Fact]
    public void ValidateProjectName_StartingWithNumber_ReturnsWarning()
    {
        // Arrange
        var nameStartingWithNumber = "123Project";

        // Act
        var result = _validator.ValidateProjectName(nameStartingWithNumber);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("start with a letter"));
    }

    [Fact]
    public void ValidateProjectName_WithSpaces_ReturnsWarning()
    {
        // Arrange
        var nameWithSpaces = "My Project";

        // Act
        var result = _validator.ValidateProjectName(nameWithSpaces);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("spaces"));
    }

    [Theory]
    [InlineData("System")]
    [InlineData("Microsoft")]
    [InlineData("Console")]
    [InlineData("Object")]
    public void ValidateProjectName_WithReservedKeyword_ReturnsWarning(string keyword)
    {
        // Act
        var result = _validator.ValidateProjectName(keyword);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("reserved keyword"));
    }

    private string CreateValidTemplate()
    {
        var templatePath = Path.Combine(_testDataPath, "ValidTemplate_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");
        
        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var templateJson = @"{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Test Author"",
            ""classifications"": [""Test"", ""Sample""],
            ""identity"": ""Test.Template.Valid"",
            ""name"": ""Test Template"",
            ""shortName"": ""test-template"",
            ""description"": ""A test template"",
            ""sourceName"": ""TestProject""
        }";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        File.WriteAllText(templateJsonPath, templateJson);

        // Create sample content
        File.WriteAllText(Path.Combine(contentPath, "README.md"), "# Test Project");
        File.WriteAllText(Path.Combine(contentPath, "TestProject.csproj"), "<Project />");

        return templatePath;
    }
}
