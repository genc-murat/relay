using Xunit;
using FluentAssertions;
using Relay.CLI.TemplateEngine;

namespace Relay.CLI.Tests.TemplateEngine;

public class TemplatePublisherTests : IDisposable
{
    private readonly TemplatePublisher _publisher;
    private readonly string _testDataPath;
    private readonly string _outputPath;

    public TemplatePublisherTests()
    {
        var testId = Guid.NewGuid().ToString("N");
        _testDataPath = Path.Combine(Path.GetTempPath(), "RelayPublisherTests", testId, "TestData");
        _outputPath = Path.Combine(Path.GetTempPath(), "RelayPublisherTests", testId, "Output");
        Directory.CreateDirectory(_testDataPath);
        Directory.CreateDirectory(_outputPath);
        _publisher = new TemplatePublisher(_testDataPath);
    }

    public void Dispose()
    {
        // Clean up test data
        if (Directory.Exists(_testDataPath))
        {
            try
            {
                Directory.Delete(_testDataPath, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }

        if (Directory.Exists(_outputPath))
        {
            try
            {
                Directory.Delete(_outputPath, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public async Task PackTemplateAsync_WithValidTemplate_CreatesPackage()
    {
        // Arrange
        var templatePath = CreateValidTemplate();

        // Act
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.PackagePath.Should().NotBeEmpty();
        result.Message.Should().Contain("successfully");
    }

    [Fact]
    public async Task PackTemplateAsync_WithInvalidTemplate_ReturnsFailure()
    {
        // Arrange
        var invalidTemplatePath = Path.Combine(_testDataPath, "InvalidTemplate_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(invalidTemplatePath);

        try
        {
            // Act
            var result = await _publisher.PackTemplateAsync(invalidTemplatePath, _outputPath);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("validation failed");
        }
        finally
        {
            if (Directory.Exists(invalidTemplatePath))
                Directory.Delete(invalidTemplatePath, true);
        }
    }

    [Fact]
    public async Task PackTemplateAsync_CreatesZipFile()
    {
        // Arrange
        var templatePath = CreateValidTemplate();

        // Act
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert - Should create .nupkg file (which is a renamed zip)
        File.Exists(result.PackagePath).Should().BeTrue();
        result.PackagePath.Should().EndWith(".nupkg");
    }

    [Fact]
    public async Task PackTemplateAsync_WithNonExistentTemplate_ReturnsFailure()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDataPath, "DoesNotExist");

        // Act
        var result = await _publisher.PackTemplateAsync(nonExistentPath, _outputPath);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task PublishTemplateAsync_WithValidPackage_Succeeds()
    {
        // Arrange
        var packagePath = Path.Combine(_outputPath, "test.nupkg");
        await File.WriteAllTextAsync(packagePath, "test package");
        var registryUrl = "https://test-registry.com";

        // Act
        var result = await _publisher.PublishTemplateAsync(packagePath, registryUrl);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("published successfully");
    }

    [Fact]
    public async Task PublishTemplateAsync_WithNonExistentPackage_ReturnsFailure()
    {
        // Arrange
        var nonExistentPackage = Path.Combine(_outputPath, "nonexistent.nupkg");
        var registryUrl = "https://test-registry.com";

        // Act
        var result = await _publisher.PublishTemplateAsync(nonExistentPackage, registryUrl);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ListAvailableTemplatesAsync_WithNoTemplates_ReturnsEmptyList()
    {
        // Arrange
        var emptyPath = Path.Combine(_testDataPath, "Empty_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emptyPath);
        var publisher = new TemplatePublisher(emptyPath);

        try
        {
            // Act
            var templates = await publisher.ListAvailableTemplatesAsync();

            // Assert
            templates.Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(emptyPath))
                Directory.Delete(emptyPath, true);
        }
    }

    [Fact]
    public async Task ListAvailableTemplatesAsync_WithValidTemplates_ReturnsTemplateList()
    {
        // Arrange
        var template1 = CreateValidTemplate("template1");
        var template2 = CreateValidTemplate("template2");

        // Act
        var templates = await _publisher.ListAvailableTemplatesAsync();

        // Assert
        templates.Should().NotBeEmpty();
        templates.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ListAvailableTemplatesAsync_ReturnsTemplateInfo()
    {
        // Arrange
        CreateValidTemplate();

        // Act
        var templates = await _publisher.ListAvailableTemplatesAsync();

        // Assert
        templates.Should().NotBeEmpty();
        var template = templates.First();
        template.Id.Should().NotBeEmpty();
        template.Name.Should().NotBeEmpty();
        template.Description.Should().NotBeEmpty();
        template.Path.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PackTemplateAsync_HandlesExistingPackage()
    {
        // Arrange
        var templatePath = CreateValidTemplate();
        
        // Pack first time
        await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Act - Pack second time (should overwrite)
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task PublishTemplateAsync_IncludesPackagePathInResult()
    {
        // Arrange
        var packagePath = Path.Combine(_outputPath, "test.nupkg");
        await File.WriteAllTextAsync(packagePath, "test");
        var registryUrl = "https://test-registry.com";

        // Act
        var result = await _publisher.PublishTemplateAsync(packagePath, registryUrl);

        // Assert
        result.PackagePath.Should().Be(packagePath);
    }

    private string CreateValidTemplate(string? suffix = null)
    {
        var templateName = "ValidTemplate" + (suffix ?? Guid.NewGuid().ToString("N"));
        var templatePath = Path.Combine(_testDataPath, templateName);
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");
        
        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var shortName = "test-template" + (suffix ?? Guid.NewGuid().ToString("N").Substring(0, 8));
        var templateJson = $@"{{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Test Author"",
            ""classifications"": [""Test""],
            ""identity"": ""Test.Template.{templateName}"",
            ""name"": ""Test Template"",
            ""shortName"": ""{shortName}"",
            ""description"": ""A test template"",
            ""sourceName"": ""TestProject""
        }}";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        File.WriteAllText(templateJsonPath, templateJson);

        // Create sample content
        File.WriteAllText(Path.Combine(contentPath, "README.md"), "# Test");
        File.WriteAllText(Path.Combine(contentPath, "Test.csproj"), "<Project />");

        return templatePath;
    }
}
