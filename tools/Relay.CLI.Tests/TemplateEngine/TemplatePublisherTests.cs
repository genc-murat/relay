using Xunit;
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
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotEmpty(result.PackagePath);
        Assert.Contains("successfully", result.Message);
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
            Assert.False(result.Success);
            Assert.Contains("validation failed", result.Message);
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
        Assert.True(File.Exists(result.PackagePath));
        Assert.EndsWith(".nupkg", result.PackagePath);
    }

    [Fact]
    public async Task PackTemplateAsync_WithNonExistentTemplate_ReturnsFailure()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDataPath, "DoesNotExist");

        // Act
        var result = await _publisher.PackTemplateAsync(nonExistentPath, _outputPath);

        // Assert
        Assert.False(result.Success);
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
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("published successfully", result.Message);
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
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
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
            Assert.Empty(templates);
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
        Assert.NotEmpty(templates);
        Assert.True(templates.Count >= 2);
    }

    [Fact]
    public async Task ListAvailableTemplatesAsync_ReturnsTemplateInfo()
    {
        // Arrange
        CreateValidTemplate();

        // Act
        var templates = await _publisher.ListAvailableTemplatesAsync();

        // Assert
        Assert.NotEmpty(templates);
        var template = templates.First();
        Assert.NotEmpty(template.Id);
        Assert.NotEmpty(template.Name);
        Assert.NotEmpty(template.Description);
        Assert.NotEmpty(template.Path);
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
        Assert.True(result.Success);
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
        Assert.Equal(packagePath, result.PackagePath);
    }

    [Fact]
    public async Task PackTemplateAsync_CreatesNuGetCompliantPackage()
    {
        // Arrange
        var templatePath = CreateValidTemplate();

        // Act
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert
        Assert.True(result.Success);
        Assert.True(File.Exists(result.PackagePath));

        // Verify it's a valid zip file
        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result.PackagePath);
        Assert.NotEmpty(zipArchive.Entries);

        // Verify NuGet package structure
        var entryNames = zipArchive.Entries.Select(e => e.FullName).ToList();
        Assert.Contains("[Content_Types].xml", entryNames);
        Assert.Contains(entryNames, e => e.EndsWith(".nuspec"));
        Assert.Contains(entryNames, e => e.StartsWith("_rels/"));
        Assert.Contains(entryNames, e => e.StartsWith("content/"));
    }

    [Fact]
    public async Task PackTemplateAsync_NuspecContainsCorrectMetadata()
    {
        // Arrange
        var templatePath = CreateValidTemplateWithVersion("1.2.3", "Test Author", "Test Description");

        // Act
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert
        Assert.True(result.Success);

        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result.PackagePath);
        var nuspecEntry = zipArchive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec"));
        Assert.NotNull(nuspecEntry);

        using var reader = new StreamReader(nuspecEntry!.Open());
        var nuspecContent = await reader.ReadToEndAsync();

        Assert.Contains("<version>1.2.3</version>", nuspecContent);
        Assert.Contains("<authors>Test Author</authors>", nuspecContent);
        Assert.Contains("<description>Test Description</description>", nuspecContent);
        Assert.Contains("<packageType name=\"Template\" />", nuspecContent);
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
        Assert.NotNull(contentTypesEntry);

        using var reader = new StreamReader(contentTypesEntry!.Open());
        var contentTypesXml = await reader.ReadToEndAsync();

        Assert.Contains("<?xml version=\"1.0\"", contentTypesXml);
        Assert.Contains("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">", contentTypesXml);
        Assert.Contains("Extension=\"rels\"", contentTypesXml);
        Assert.Contains("Extension=\"psmdcp\"", contentTypesXml);
        Assert.Contains("Extension=\"nuspec\"", contentTypesXml);
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
        Assert.NotNull(relsEntry);

        using var reader = new StreamReader(relsEntry!.Open());
        var relsContent = await reader.ReadToEndAsync();

        Assert.Contains("<?xml version=\"1.0\"", relsContent);
        Assert.Contains("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">", relsContent);
        Assert.Contains("Type=\"http://schemas.microsoft.com/packaging/2010/07/manifest\"", relsContent);
        Assert.Contains("Target=\"/package.nuspec\"", relsContent);
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
        Assert.NotNull(corePropsEntry);

        using var reader = new StreamReader(corePropsEntry!.Open());
        var corePropsContent = await reader.ReadToEndAsync();

        Assert.Contains("<dc:creator>Core Author</dc:creator>", corePropsContent);
        Assert.Contains("<dc:description>Core Description</dc:description>", corePropsContent);
        Assert.Contains("<version>2.0.0</version>", corePropsContent);
        Assert.Contains("dcterms:created", corePropsContent);
        Assert.Contains("dcterms:modified", corePropsContent);
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

        Assert.NotEmpty(contentEntries);
        Assert.Contains(contentEntries, e => e.FullName.Contains("template.json"));
        Assert.Contains(contentEntries, e => e.FullName.Contains("README.md"));
        Assert.Contains(contentEntries, e => e.FullName.Contains(".csproj"));
    }

    [Fact]
    public async Task PackTemplateAsync_HandlesTemplateWithoutVersion()
    {
        // Arrange
        var templatePath = CreateValidTemplate(); // No version specified

        // Act
        var result = await _publisher.PackTemplateAsync(templatePath, _outputPath);

        // Assert
        Assert.True(result.Success);

        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result.PackagePath);
        var nuspecEntry = zipArchive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec"));

        using var reader = new StreamReader(nuspecEntry!.Open());
        var nuspecContent = await reader.ReadToEndAsync();

        Assert.Contains("<version>1.0.0</version>", nuspecContent);
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
        Assert.True(result.Success);

        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result.PackagePath);
        var nuspecEntry = zipArchive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec"));

        using var reader = new StreamReader(nuspecEntry!.Open());
        var nuspecContent = await reader.ReadToEndAsync();

        // Should not contain unescaped special characters
        Assert.DoesNotContain("<authors>Author & Co. <test@example.com></authors>", nuspecContent);
        // Should contain escaped versions
        Assert.Contains("&amp;", nuspecContent);
        Assert.Contains("&lt;", nuspecContent);
        Assert.Contains("&gt;", nuspecContent);
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
