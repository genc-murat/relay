using Xunit;
using Relay.CLI.TemplateEngine;

namespace Relay.CLI.Tests.Integration;

[Collection("Integration Tests")]
[Trait("Category", "Integration")]
public class TemplateEndToEndTests : IDisposable
{
    private readonly TemplateGenerator _generator;
    private readonly TemplateValidator _validator;
    private readonly TemplatePublisher _publisher;
    private readonly string _templatesPath;
    private readonly string _testOutputPath;

    public TemplateEndToEndTests()
    {
        _templatesPath = Path.Combine(AppContext.BaseDirectory, "TestData");
        _testOutputPath = Path.Combine(Path.GetTempPath(), "RelayIntegrationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputPath);
        
        _generator = new TemplateGenerator(_templatesPath);
        _validator = new TemplateValidator();
        _publisher = new TemplatePublisher(_templatesPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputPath))
        {
            try
            {
                Directory.Delete(_testOutputPath, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public async Task CompleteWorkflow_CreateValidatePackPublish_Succeeds()
    {
        // Step 1: Create a template
        var templatePath = CreateTestTemplate();
        Assert.NotEmpty(templatePath);
        Assert.True(Directory.Exists(templatePath));

        // Step 2: Validate template
        var validationResult = await _validator.ValidateAsync(templatePath);
        Assert.True(validationResult.IsValid);
        Assert.Empty(validationResult.Errors);

        // Step 3: Package template
        var packResult = await _publisher.PackTemplateAsync(templatePath, _testOutputPath);
        Assert.True(packResult.Success);
        Assert.NotEmpty(packResult.PackagePath);

        // Step 4: Publish template (simulated)
        var packagePath = packResult.PackagePath.Replace(".nupkg", ".zip");
        if (File.Exists(packagePath))
        {
            var publishResult = await _publisher.PublishTemplateAsync(packagePath, "https://test-registry.com");
            Assert.True(publishResult.Success);
        }
    }

    [Fact]
    public async Task GenerateProject_FromValidTemplate_CreatesCompleteStructure()
    {
        // Arrange
        var projectName = "TestWebApi";
        var options = new GenerationOptions
        {
            EnableAuth = true,
            EnableSwagger = true,
            EnableDocker = true,
            EnableHealthChecks = true,
            DatabaseProvider = "postgres",
            TargetFramework = "net8.0"
        };

        // Act
        var result = await _generator.GenerateAsync("relay-webapi", projectName, _testOutputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.CreatedDirectories);
        Assert.Contains(result.CreatedDirectories, d => d.Contains("src"));
        Assert.Contains(result.CreatedDirectories, d => d.Contains("tests"));
        Assert.True(result.Duration < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GenerateMultipleProjects_FromSameTemplate_AllSucceed()
    {
        // Arrange
        var options = new GenerationOptions();
        var projects = new[] { "Project1", "Project2", "Project3" };

        // Act
        var results = new List<GenerationResult>();
        foreach (var project in projects)
        {
            var projectPath = Path.Combine(_testOutputPath, project);
            Directory.CreateDirectory(projectPath);
            var result = await _generator.GenerateAsync("relay-webapi", project, projectPath, options);
            results.Add(result);
        }

        // Assert
        Assert.All(results, r => Assert.True(r.Success));
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task ValidateAndGenerate_WithInvalidProjectName_HandlesGracefully()
    {
        // Arrange
        var invalidProjectName = "<Invalid>";
        var options = new GenerationOptions();

        // Act - Validation
        var validationResult = _validator.ValidateProjectName(invalidProjectName);
        Assert.False(validationResult.IsValid);

        // Act - Generation (should handle error)
        var result = await _generator.GenerateAsync(
            "relay-webapi",
            invalidProjectName,
            _testOutputPath,
            options);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task GenerateProject_WithAllFeatures_CreatesExpectedFiles()
    {
        // Arrange
        var projectName = "FullFeaturedApi";
        var options = new GenerationOptions
        {
            EnableAuth = true,
            EnableSwagger = true,
            EnableDocker = true,
            EnableHealthChecks = true,
            EnableCaching = true,
            EnableTelemetry = true,
            DatabaseProvider = "postgres"
        };

        // Act
        var result = await _generator.GenerateAsync("relay-webapi", projectName, _testOutputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.CreatedFiles);
        
        // Should have created multiple project layers
        Assert.Contains(result.CreatedDirectories, d => d.Contains("Api"));
        Assert.Contains(result.CreatedDirectories, d => d.Contains("Application"));
        Assert.Contains(result.CreatedDirectories, d => d.Contains("Domain"));
        Assert.Contains(result.CreatedDirectories, d => d.Contains("Infrastructure"));
    }

    [Fact]
    public async Task GenerateProject_WithMinimalFeatures_CreatesBasicStructure()
    {
        // Arrange
        var projectName = "MinimalApi";
        var options = new GenerationOptions
        {
            EnableAuth = false,
            EnableSwagger = false,
            EnableDocker = false,
            EnableHealthChecks = false,
            EnableCaching = false,
            EnableTelemetry = false
        };

        // Act
        var result = await _generator.GenerateAsync("relay-webapi", projectName, _testOutputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.CreatedDirectories);
    }

    [Fact]
    public async Task ListTemplates_AfterCreatingMultiple_ReturnsAllTemplates()
    {
        // Arrange
        CreateTestTemplate("template1");
        CreateTestTemplate("template2");
        CreateTestTemplate("template3");

        // Act
        var templates = await _publisher.ListAvailableTemplatesAsync();

        // Assert
        Assert.NotEmpty(templates);
        Assert.True(templates.Count >= 3);
    }

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("postgres")]
    [InlineData("mysql")]
    [InlineData("sqlite")]
    public async Task GenerateProject_WithDifferentDatabaseProviders_Succeeds(string provider)
    {
        // Arrange
        var options = new GenerationOptions { DatabaseProvider = provider };
        var projectPath = Path.Combine(_testOutputPath, provider);
        Directory.CreateDirectory(projectPath);
        
        // Act
        var result = await _generator.GenerateAsync("relay-webapi", $"Test{provider}", projectPath, options);
        
        // Assert
        Assert.True(result.Success, $"generation with {provider} should succeed");
    }

    [Fact]
    public async Task GenerateModularProject_WithCustomModules_CreatesModuleStructure()
    {
        // Arrange
        var projectName = "ModularMonolith";
        var options = new GenerationOptions
        {
            Modules = new[] { "Catalog", "Orders", "Customers", "Inventory" }
        };

        // Act
        var result = await _generator.GenerateAsync("relay-modular", projectName, _testOutputPath, options);

        // Assert
        if (!result.Success)
        {
            Console.WriteLine($"Error: {result.Message}");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }

        Assert.True(result.Success);
        Assert.Contains(result.CreatedDirectories, d => d.Contains("Modules"));

        foreach (var module in options.Modules!)
        {
            Assert.Contains(result.CreatedDirectories, d => d.Contains(module));
        }
    }

    [Fact]
    public async Task GenerateProject_MeasuresPerformance()
    {
        // Arrange
        var projectName = "PerformanceTest";
        var options = new GenerationOptions();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _generator.GenerateAsync("relay-webapi", projectName, _testOutputPath, options);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Duration > TimeSpan.Zero);
        Assert.True(result.Duration < TimeSpan.FromSeconds(30),
            "generation should complete within 30 seconds");
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CompletePackagingWorkflow_CreateValidatePackVerify_ProducesValidNuGetPackage()
    {
        // Step 1: Create a complete template with all necessary files
        var templatePath = CreateCompleteTemplate();
        Assert.NotEmpty(templatePath);
        Assert.True(Directory.Exists(templatePath));

        // Step 2: Validate the template structure
        var validationResult = await _validator.ValidateAsync(templatePath);
        Assert.True(validationResult.IsValid, "template should be valid");
        Assert.True(validationResult.Errors.Count == 0, "valid template should have no errors");

        // Step 3: Package the template into NuGet format
        var packResult = await _publisher.PackTemplateAsync(templatePath, _testOutputPath);
        Assert.True(packResult.Success, "packaging should succeed");
        Assert.NotEmpty(packResult.PackagePath);
        Assert.True(File.Exists(packResult.PackagePath), "package file should exist");

        // Step 4: Verify package is a valid NuGet package
        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(packResult.PackagePath);
        var entries = zipArchive.Entries.Select(e => e.FullName).ToList();

        // Verify core NuGet package structure
        Assert.Contains("[Content_Types].xml", entries);
        Assert.Contains(entries, e => e.EndsWith(".nuspec"));
        Assert.Contains("_rels/.rels", entries);
        Assert.Contains(entries, e => e.StartsWith("package/services/metadata/core-properties/"));
        Assert.Contains(entries, e => e.StartsWith("content/"));

        // Step 5: Verify .nuspec content
        var nuspecEntry = zipArchive.Entries.First(e => e.FullName.EndsWith(".nuspec"));
        using var nuspecReader = new StreamReader(nuspecEntry.Open());
        var nuspecContent = await nuspecReader.ReadToEndAsync();

        Assert.Contains("<packageType name=\"Template\" />", nuspecContent);
        Assert.Contains("<id>", nuspecContent);
        Assert.Contains("<version>", nuspecContent);
        Assert.Contains("<authors>", nuspecContent);
        Assert.Contains("<description>", nuspecContent);

        // Step 6: Verify template files were copied
        var templateJsonEntry = entries.FirstOrDefault(e => e.Contains("template.json"));
        Assert.True(templateJsonEntry != null, "template.json should be in package");
    }

    [Fact]
    public async Task PackageMultipleTemplates_InParallel_AllSucceed()
    {
        // Arrange
        var templates = new[]
        {
            CreateCompleteTemplate("api-template"),
            CreateCompleteTemplate("worker-template"),
            CreateCompleteTemplate("lib-template")
        };

        // Act
        var packTasks = templates.Select(t => _publisher.PackTemplateAsync(t, _testOutputPath)).ToArray();
        var results = await Task.WhenAll(packTasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Success);
            Assert.NotEmpty(r.PackagePath);
            Assert.True(File.Exists(r.PackagePath));
        });

        // Verify all packages have unique names
        var packageNames = results.Select(r => Path.GetFileName(r.PackagePath)).ToList();
        Assert.Equal(packageNames.Count, packageNames.Distinct().Count());
    }

    [Fact]
    public async Task PackageTemplate_WithVersionUpdate_CreatesNewPackage()
    {
        // Arrange - Create template with version 1.0.0
        var templatePath = CreateTemplateWithVersion("1.0.0");

        // Act 1 - Package version 1.0.0
        var result1 = await _publisher.PackTemplateAsync(templatePath, _testOutputPath);
        Assert.True(result1.Success);

        // Arrange - Update template to version 2.0.0
        UpdateTemplateVersion(templatePath, "2.0.0");

        // Act 2 - Package version 2.0.0
        var result2 = await _publisher.PackTemplateAsync(templatePath, _testOutputPath);
        Assert.True(result2.Success);

        // Assert - Verify version in second package
        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result2.PackagePath);
        var nuspecEntry = zipArchive.Entries.First(e => e.FullName.EndsWith(".nuspec"));
        using var reader = new StreamReader(nuspecEntry.Open());
        var nuspecContent = await reader.ReadToEndAsync();

        Assert.Contains("<version>2.0.0</version>", nuspecContent);
    }

    [Fact]
    public async Task ValidatePackageIntegrity_AfterPackaging_AllFilesAreAccessible()
    {
        // Arrange
        var templatePath = CreateCompleteTemplate();

        // Act
        var packResult = await _publisher.PackTemplateAsync(templatePath, _testOutputPath);
        Assert.True(packResult.Success);

        // Assert - Open and read all entries to verify integrity
        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(packResult.PackagePath);

        foreach (var entry in zipArchive.Entries)
        {
            if (entry.Length > 0) // Skip directories
            {
                using var stream = entry.Open();
                using var reader = new StreamReader(stream);

                // Should be able to read without exceptions
                var content = await reader.ReadToEndAsync();
                Assert.True(content != null, $"entry {entry.FullName} should be readable");
            }
        }
    }

    [Fact]
    public async Task PackageTemplate_WithLargeContent_CompletesSuccessfully()
    {
        // Arrange - Create template with many files
        var templatePath = CreateTemplateWithManyFiles(100);

        // Act
        var packResult = await _publisher.PackTemplateAsync(templatePath, _testOutputPath);

        // Assert
        Assert.True(packResult.Success);

        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(packResult.PackagePath);
        var contentFiles = zipArchive.Entries.Where(e => e.FullName.StartsWith("content/")).ToList();

        Assert.True(contentFiles.Count > 50, "should contain many files");
    }

    [Fact]
    public async Task EndToEndWorkflow_PackagePublishList_WorksCorrectly()
    {
        // Step 1: Create and package template
        var templatePath = CreateCompleteTemplate("e2e-template");
        var packResult = await _publisher.PackTemplateAsync(templatePath, _testOutputPath);
        Assert.True(packResult.Success);

        // Step 2: Publish (simulated)
        var publishResult = await _publisher.PublishTemplateAsync(packResult.PackagePath, "https://nuget.org");
        Assert.True(publishResult.Success);

        // Step 3: List templates
        var templates = await _publisher.ListAvailableTemplatesAsync();
        Assert.Contains(templates, t => t.Id.Contains("e2e-template"));
    }

    private string CreateTestTemplate(string? suffix = null)
    {
        var templateName = "TestTemplate" + (suffix ?? Guid.NewGuid().ToString("N"));
        var templatePath = Path.Combine(_templatesPath, templateName);
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");
        
        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var templateJson = $@"{{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Test Author"",
            ""classifications"": [""Test"", ""Integration""],
            ""identity"": ""Test.Template.{templateName}"",
            ""name"": ""Test Template {suffix}"",
            ""shortName"": ""test-{suffix ?? "template"}"",
            ""description"": ""Integration test template"",
            ""sourceName"": ""TestProject"",
            ""tags"": {{
                ""language"": ""C#"",
                ""type"": ""project""
            }}
        }}";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        File.WriteAllText(templateJsonPath, templateJson);

        // Create sample content
        File.WriteAllText(Path.Combine(contentPath, "README.md"), "# Test Project");
        File.WriteAllText(Path.Combine(contentPath, "TestProject.csproj"), 
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(contentPath, "Program.cs"), 
            "Console.WriteLine(\"Hello from template!\");");

        return templatePath;
    }

    private string CreateCompleteTemplate(string? suffix = null)
    {
        var templateName = "CompleteTemplate" + (suffix ?? Guid.NewGuid().ToString("N"));
        var templatePath = Path.Combine(_templatesPath, templateName);
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");
        var srcPath = Path.Combine(contentPath, "src");
        var testsPath = Path.Combine(contentPath, "tests");

        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(srcPath);
        Directory.CreateDirectory(testsPath);

        var shortName = suffix ?? "complete-template";
        var templateJson = $@"{{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Relay Team"",
            ""classifications"": [""Web"", ""API"", ""Test""],
            ""identity"": ""Relay.Template.{templateName}"",
            ""name"": ""Complete Relay Template"",
            ""shortName"": ""{shortName}"",
            ""description"": ""A complete template for integration testing"",
            ""version"": ""1.0.0"",
            ""sourceName"": ""CompleteProject"",
            ""tags"": {{
                ""language"": ""C#"",
                ""type"": ""project""
            }}
        }}";

        File.WriteAllText(Path.Combine(configPath, "template.json"), templateJson);

        // Create comprehensive content structure
        File.WriteAllText(Path.Combine(contentPath, "README.md"), "# Complete Project\n\nA complete template for testing.");
        File.WriteAllText(Path.Combine(contentPath, ".gitignore"), "bin/\nobj/\n*.user");
        File.WriteAllText(Path.Combine(contentPath, "CompleteProject.sln"), "<Solution />");

        File.WriteAllText(Path.Combine(srcPath, "CompleteProject.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(srcPath, "Program.cs"),
            "var builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();\napp.Run();");
        File.WriteAllText(Path.Combine(srcPath, "appsettings.json"),
            "{\"Logging\": {\"LogLevel\": {\"Default\": \"Information\"}}}");

        File.WriteAllText(Path.Combine(testsPath, "CompleteProject.Tests.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(testsPath, "UnitTest1.cs"),
            "using Xunit;\npublic class UnitTest1 { [Fact] public void Test1() { } }");

        return templatePath;
    }

    private string CreateTemplateWithVersion(string version)
    {
        var templateName = "VersionedTemplate" + Guid.NewGuid().ToString("N");
        var templatePath = Path.Combine(_templatesPath, templateName);
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");

        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var templateJson = $@"{{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Version Test"",
            ""classifications"": [""Test""],
            ""identity"": ""Test.Versioned.Template"",
            ""name"": ""Versioned Template"",
            ""shortName"": ""versioned-template"",
            ""description"": ""Template with version"",
            ""version"": ""{version}"",
            ""sourceName"": ""VersionedProject""
        }}";

        File.WriteAllText(Path.Combine(configPath, "template.json"), templateJson);
        File.WriteAllText(Path.Combine(contentPath, "README.md"), $"# Version {version}");
        File.WriteAllText(Path.Combine(contentPath, "Project.csproj"), "<Project />");

        return templatePath;
    }

    private void UpdateTemplateVersion(string templatePath, string newVersion)
    {
        var templateJsonPath = Path.Combine(templatePath, ".template.config", "template.json");
        var json = File.ReadAllText(templateJsonPath);

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        var updatedJson = new Dictionary<string, object?>();
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.Equals("version", StringComparison.OrdinalIgnoreCase))
            {
                updatedJson[property.Name] = newVersion;
            }
            else
            {
                updatedJson[property.Name] = System.Text.Json.JsonSerializer.Deserialize<object>(property.Value.GetRawText());
            }
        }

        var newJson = System.Text.Json.JsonSerializer.Serialize(updatedJson, options);
        File.WriteAllText(templateJsonPath, newJson);
    }

    private string CreateTemplateWithManyFiles(int fileCount)
    {
        var templateName = "LargeTemplate" + Guid.NewGuid().ToString("N");
        var templatePath = Path.Combine(_templatesPath, templateName);
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");

        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var templateJson = @"{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Large Test"",
            ""classifications"": [""Test""],
            ""identity"": ""Test.Large.Template"",
            ""name"": ""Large Template"",
            ""shortName"": ""large-template"",
            ""description"": ""Template with many files"",
            ""version"": ""1.0.0"",
            ""sourceName"": ""LargeProject""
        }";

        File.WriteAllText(Path.Combine(configPath, "template.json"), templateJson);

        // Create many files
        for (int i = 0; i < fileCount; i++)
        {
            var subDir = Path.Combine(contentPath, $"Directory{i / 10}");
            Directory.CreateDirectory(subDir);

            var fileName = $"File{i}.cs";
            var filePath = Path.Combine(subDir, fileName);
            File.WriteAllText(filePath, $"// File {i}\nnamespace LargeProject;\npublic class Class{i} {{ }}");
        }

        return templatePath;
    }
}
