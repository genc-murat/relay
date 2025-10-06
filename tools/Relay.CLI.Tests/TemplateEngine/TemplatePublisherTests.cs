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

    [Fact]
    public async Task PackTemplateAsync_CreatesNuGetCompliantPackage()
    {
        // Arrange
        var templatePath = CreateValidTemplate();

        // Act
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert
        result.Success.Should().BeTrue();
        File.Exists(result.PackagePath).Should().BeTrue();

        // Verify it's a valid zip file
        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result.PackagePath);
        zipArchive.Entries.Should().NotBeEmpty();

        // Verify NuGet package structure
        var entryNames = zipArchive.Entries.Select(e => e.FullName).ToList();
        entryNames.Should().Contain(e => e == "[Content_Types].xml", "package should contain Content_Types.xml");
        entryNames.Should().Contain(e => e.EndsWith(".nuspec"), "package should contain .nuspec file");
        entryNames.Should().Contain(e => e.StartsWith("_rels/"), "package should contain _rels directory");
        entryNames.Should().Contain(e => e.StartsWith("content/"), "package should contain content directory");
    }

    [Fact]
    public async Task PackTemplateAsync_NuspecContainsCorrectMetadata()
    {
        // Arrange
        var templatePath = CreateValidTemplateWithVersion("1.2.3", "Test Author", "Test Description");

        // Act
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert
        result.Success.Should().BeTrue();

        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result.PackagePath);
        var nuspecEntry = zipArchive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec"));
        nuspecEntry.Should().NotBeNull("package should contain .nuspec file");

        using var reader = new StreamReader(nuspecEntry!.Open());
        var nuspecContent = await reader.ReadToEndAsync();

        nuspecContent.Should().Contain("<version>1.2.3</version>");
        nuspecContent.Should().Contain("<authors>Test Author</authors>");
        nuspecContent.Should().Contain("<description>Test Description</description>");
        nuspecContent.Should().Contain("<packageType name=\"Template\" />");
    }

    [Fact]
    public async Task PackTemplateAsync_ContentTypesXmlIsValid()
    {
        // Arrange
        var templatePath = CreateValidTemplate();

        // Act
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert
        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result.PackagePath);
        var contentTypesEntry = zipArchive.Entries.FirstOrDefault(e => e.FullName == "[Content_Types].xml");
        contentTypesEntry.Should().NotBeNull();

        using var reader = new StreamReader(contentTypesEntry!.Open());
        var contentTypesXml = await reader.ReadToEndAsync();

        contentTypesXml.Should().Contain("<?xml version=\"1.0\"");
        contentTypesXml.Should().Contain("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
        contentTypesXml.Should().Contain("Extension=\"rels\"");
        contentTypesXml.Should().Contain("Extension=\"psmdcp\"");
        contentTypesXml.Should().Contain("Extension=\"nuspec\"");
    }

    [Fact]
    public async Task PackTemplateAsync_PackageRelationshipsAreValid()
    {
        // Arrange
        var templatePath = CreateValidTemplate();

        // Act
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert
        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result.PackagePath);
        var relsEntry = zipArchive.Entries.FirstOrDefault(e => e.FullName == "_rels/.rels");
        relsEntry.Should().NotBeNull();

        using var reader = new StreamReader(relsEntry!.Open());
        var relsContent = await reader.ReadToEndAsync();

        relsContent.Should().Contain("<?xml version=\"1.0\"");
        relsContent.Should().Contain("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
        relsContent.Should().Contain("Type=\"http://schemas.microsoft.com/packaging/2010/07/manifest\"");
        relsContent.Should().Contain("Target=\"/package.nuspec\"");
    }

    [Fact]
    public async Task PackTemplateAsync_CorePropertiesAreValid()
    {
        // Arrange
        var templatePath = CreateValidTemplateWithVersion("2.0.0", "Core Author", "Core Description");

        // Act
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert
        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result.PackagePath);
        var corePropsEntry = zipArchive.Entries.FirstOrDefault(e =>
            e.FullName.StartsWith("package/services/metadata/core-properties/") &&
            e.FullName.EndsWith(".psmdcp"));
        corePropsEntry.Should().NotBeNull();

        using var reader = new StreamReader(corePropsEntry!.Open());
        var corePropsContent = await reader.ReadToEndAsync();

        corePropsContent.Should().Contain("<dc:creator>Core Author</dc:creator>");
        corePropsContent.Should().Contain("<dc:description>Core Description</dc:description>");
        corePropsContent.Should().Contain("<version>2.0.0</version>");
        corePropsContent.Should().Contain("dcterms:created");
        corePropsContent.Should().Contain("dcterms:modified");
    }

    [Fact]
    public async Task PackTemplateAsync_TemplateFilesAreCopiedToContent()
    {
        // Arrange
        var templatePath = CreateValidTemplate();

        // Act
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert
        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result.PackagePath);
        var contentEntries = zipArchive.Entries.Where(e => e.FullName.StartsWith("content/")).ToList();

        contentEntries.Should().NotBeEmpty("template files should be copied to content directory");
        contentEntries.Should().Contain(e => e.FullName.Contains("template.json"), "template.json should be in content");
        contentEntries.Should().Contain(e => e.FullName.Contains("README.md"), "README.md should be in content");
        contentEntries.Should().Contain(e => e.FullName.Contains(".csproj"), "project file should be in content");
    }

    [Fact]
    public async Task PackTemplateAsync_HandlesTemplateWithoutVersion()
    {
        // Arrange
        var templatePath = CreateValidTemplate(); // No version specified

        // Act
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert
        result.Success.Should().BeTrue();

        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result.PackagePath);
        var nuspecEntry = zipArchive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec"));

        using var reader = new StreamReader(nuspecEntry!.Open());
        var nuspecContent = await reader.ReadToEndAsync();

        nuspecContent.Should().Contain("<version>1.0.0</version>", "default version should be 1.0.0");
    }

    [Fact]
    public async Task PackTemplateAsync_EscapesXmlSpecialCharacters()
    {
        // Arrange
        var templatePath = CreateValidTemplateWithSpecialChars(
            author: "Author & Co. <test@example.com>",
            description: "Description with <tags> & \"quotes\""
        );

        // Act
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert
        result.Success.Should().BeTrue();

        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result.PackagePath);
        var nuspecEntry = zipArchive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec"));

        using var reader = new StreamReader(nuspecEntry!.Open());
        var nuspecContent = await reader.ReadToEndAsync();

        // Should not contain unescaped special characters
        nuspecContent.Should().NotContain("<authors>Author & Co. <test@example.com></authors>");
        // Should contain escaped versions
        nuspecContent.Should().Contain("&amp;", "& should be escaped");
        nuspecContent.Should().Contain("&lt;", "< should be escaped");
        nuspecContent.Should().Contain("&gt;", "> should be escaped");
    }

    [Fact]
    public async Task PackTemplateAsync_CleansUpTemporaryDirectory()
    {
        // Arrange
        var templatePath = CreateValidTemplate();
        var tempDirsBefore = Directory.GetDirectories(Path.GetTempPath(), "relay_template_*").ToHashSet();

        // Act
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert
        result.Success.Should().BeTrue();

        // Give cleanup a moment
        await Task.Delay(100);

        var tempDirsAfter = Directory.GetDirectories(Path.GetTempPath(), "relay_template_*").ToHashSet();

        // Check that no new temporary directories were left behind
        var newTempDirs = tempDirsAfter.Except(tempDirsBefore).ToList();
        newTempDirs.Should().BeEmpty("temporary directories created during the operation should be cleaned up");
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

    private string CreateValidTemplateWithVersion(string version, string author, string description)
    {
        var templateName = "ValidTemplate" + Guid.NewGuid().ToString("N");
        var templatePath = Path.Combine(_testDataPath, templateName);
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");

        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var shortName = "test-template" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var templateJson = $@"{{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""{author}"",
            ""classifications"": [""Test""],
            ""identity"": ""Test.Template.{templateName}"",
            ""name"": ""Test Template"",
            ""shortName"": ""{shortName}"",
            ""description"": ""{description}"",
            ""version"": ""{version}"",
            ""sourceName"": ""TestProject""
        }}";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        File.WriteAllText(templateJsonPath, templateJson);

        // Create sample content
        File.WriteAllText(Path.Combine(contentPath, "README.md"), "# Test");
        File.WriteAllText(Path.Combine(contentPath, "Test.csproj"), "<Project />");

        return templatePath;
    }

    private string CreateValidTemplateWithSpecialChars(string author, string description)
    {
        var templateName = "ValidTemplate" + Guid.NewGuid().ToString("N");
        var templatePath = Path.Combine(_testDataPath, templateName);
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");

        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var shortName = "test-template" + Guid.NewGuid().ToString("N").Substring(0, 8);

        // Use JsonSerializer to properly escape JSON strings
        var metadata = new
        {
            schema = "http://json.schemastore.org/template",
            author = author,
            classifications = new[] { "Test" },
            identity = $"Test.Template.{templateName}",
            name = "Test Template",
            shortName = shortName,
            description = description,
            version = "1.0.0",
            sourceName = "TestProject"
        };

        var templateJson = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        var templateJsonPath = Path.Combine(configPath, "template.json");
        File.WriteAllText(templateJsonPath, templateJson);

        // Create sample content
        File.WriteAllText(Path.Combine(contentPath, "README.md"), "# Test");
        File.WriteAllText(Path.Combine(contentPath, "Test.csproj"), "<Project />");

        return templatePath;
    }
}
