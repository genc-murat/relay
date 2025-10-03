using Xunit;
using FluentAssertions;
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
        templatePath.Should().NotBeEmpty();
        Directory.Exists(templatePath).Should().BeTrue();

        // Step 2: Validate template
        var validationResult = await _validator.ValidateAsync(templatePath);
        validationResult.IsValid.Should().BeTrue();
        validationResult.Errors.Should().BeEmpty();

        // Step 3: Package template
        var packResult = await _publisher.PackTemplateAsync(templatePath, _testOutputPath);
        packResult.Success.Should().BeTrue();
        packResult.PackagePath.Should().NotBeEmpty();

        // Step 4: Publish template (simulated)
        var packagePath = packResult.PackagePath.Replace(".nupkg", ".zip");
        if (File.Exists(packagePath))
        {
            var publishResult = await _publisher.PublishTemplateAsync(packagePath, "https://test-registry.com");
            publishResult.Success.Should().BeTrue();
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
        result.Success.Should().BeTrue();
        result.CreatedDirectories.Should().NotBeEmpty();
        result.CreatedDirectories.Should().Contain(d => d.Contains("src"));
        result.CreatedDirectories.Should().Contain(d => d.Contains("tests"));
        result.Duration.Should().BeLessThan(TimeSpan.FromMinutes(1));
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
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task ValidateAndGenerate_WithInvalidProjectName_HandlesGracefully()
    {
        // Arrange
        var invalidProjectName = "<Invalid>";
        var options = new GenerationOptions();

        // Act - Validation
        var validationResult = _validator.ValidateProjectName(invalidProjectName);
        validationResult.IsValid.Should().BeFalse();

        // Act - Generation (should handle error)
        var result = await _generator.GenerateAsync(
            "relay-webapi",
            invalidProjectName,
            _testOutputPath,
            options);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
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
        result.Success.Should().BeTrue();
        result.CreatedFiles.Should().NotBeEmpty();
        
        // Should have created multiple project layers
        result.CreatedDirectories.Should().Contain(d => d.Contains("Api"));
        result.CreatedDirectories.Should().Contain(d => d.Contains("Application"));
        result.CreatedDirectories.Should().Contain(d => d.Contains("Domain"));
        result.CreatedDirectories.Should().Contain(d => d.Contains("Infrastructure"));
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
        result.Success.Should().BeTrue();
        result.CreatedDirectories.Should().NotBeEmpty();
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
        templates.Should().NotBeEmpty();
        templates.Count.Should().BeGreaterThanOrEqualTo(3);
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
        result.Success.Should().BeTrue($"generation with {provider} should succeed");
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
        result.Success.Should().BeTrue();
        result.CreatedDirectories.Should().Contain(d => d.Contains("Modules"));
        
        foreach (var module in options.Modules!)
        {
            result.CreatedDirectories.Should().Contain(d => d.Contains(module));
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
        result.Success.Should().BeTrue();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.Duration.Should().BeLessThan(TimeSpan.FromSeconds(30), 
            "generation should complete within 30 seconds");
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMinutes(1));
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
}
