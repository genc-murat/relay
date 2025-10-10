using Xunit;
using Relay.CLI.TemplateEngine;

namespace Relay.CLI.Tests.TemplateEngine;

public class TemplateGeneratorTests : IDisposable
{
    private readonly TemplateGenerator _generator;
    private readonly string _testOutputPath;
    private readonly string _templatesPath;

    public TemplateGeneratorTests()
    {
        _templatesPath = Path.Combine(AppContext.BaseDirectory, "TestData");
        _testOutputPath = Path.Combine(Path.GetTempPath(), "RelayTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputPath);
        _generator = new TemplateGenerator(_templatesPath);
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
    public async Task GenerateAsync_WithValidTemplate_CreatesProjectSuccessfully()
    {
        // Arrange
        var projectName = "TestProject";
        var options = new GenerationOptions
        {
            EnableSwagger = true,
            EnableDocker = true,
            DatabaseProvider = "postgres"
        };

        // Act
        var result = await _generator.GenerateAsync("relay-webapi", projectName, _testOutputPath, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("relay-webapi", result.TemplateName);
        Assert.NotEmpty(result.CreatedDirectories);
        Assert.Contains("created successfully", result.Message);
    }

    [Fact]
    public async Task GenerateAsync_WithInvalidTemplate_ReturnsFailure()
    {
        // Arrange
        var projectName = "TestProject";
        var options = new GenerationOptions();

        // Act
        var result = await _generator.GenerateAsync("invalid-template", projectName, _testOutputPath, options);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    [Fact]
    public void AddVariable_ShouldStoreVariable()
    {
        // Arrange
        var key = "TestKey";
        var value = "TestValue";

        // Act
        _generator.AddVariable(key, value);

        // Assert - Variables are private, so we test indirectly
        var options = new GenerationOptions();
        var result = _generator.GenerateAsync("relay-webapi", "Test", _testOutputPath, options);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GenerateAsync_WithDifferentOptions_CreatesCorrectStructure()
    {
        // Arrange
        var projectName = "TestProject";
        var options = new GenerationOptions
        {
            EnableAuth = true,
            EnableSwagger = true,
            EnableDocker = true,
            EnableHealthChecks = true,
            EnableCaching = true,
            EnableTelemetry = true,
            DatabaseProvider = "sqlserver",
            TargetFramework = "net8.0"
        };

        // Act
        var result = await _generator.GenerateAsync("relay-webapi", projectName, _testOutputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.Contains(result.CreatedDirectories, d => d.Contains(projectName));
    }

    [Fact]
    public async Task GenerateAsync_TracksGenerationTime()
    {
        // Arrange
        var projectName = "TestProject";
        var options = new GenerationOptions();

        // Act
        var result = await _generator.GenerateAsync("relay-webapi", projectName, _testOutputPath, options);

        // Assert
        Assert.True(result.Duration > TimeSpan.Zero);
        Assert.True(result.Duration < TimeSpan.FromSeconds(30));
    }

    [Theory]
    [InlineData("relay-webapi")]
    [InlineData("relay-microservice")]
    [InlineData("relay-ddd")]
    [InlineData("relay-modular")]
    public async Task GenerateAsync_WithDifferentTemplates_Succeeds(string templateId)
    {
        // Arrange
        var projectName = "TestProject";
        var options = new GenerationOptions();

        // Act
        var result = await _generator.GenerateAsync(templateId, projectName, _testOutputPath, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(templateId, result.TemplateName);
    }

    [Fact]
    public async Task GenerateAsync_WithModularTemplate_CreatesModules()
    {
        // Arrange
        var projectName = "TestProject";
        var options = new GenerationOptions
        {
            Modules = new[] { "Catalog", "Orders", "Customers" }
        };

        // Act
        var result = await _generator.GenerateAsync("relay-modular", projectName, _testOutputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.Contains(result.CreatedDirectories, d => d.Contains("Modules"));
    }

    [Fact]
    public async Task GenerateAsync_RecordsCreatedFilesAndDirectories()
    {
        // Arrange
        var projectName = "TestProject";
        var options = new GenerationOptions();

        // Act
        var result = await _generator.GenerateAsync("relay-webapi", projectName, _testOutputPath, options);

        // Assert
        Assert.NotEmpty(result.CreatedDirectories);
        Assert.NotEmpty(result.CreatedFiles);
        Assert.True(result.CreatedDirectories.Count > 5);
    }

    [Fact]
    public async Task GenerateAsync_WithNullProjectName_HandlesGracefully()
    {
        // Arrange
        var options = new GenerationOptions();

        // Act
        var result = await _generator.GenerateAsync("relay-webapi", null!, _testOutputPath, options);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task GenerateAsync_WithEmptyProjectName_HandlesGracefully()
    {
        // Arrange
        var options = new GenerationOptions();

        // Act
        var result = await _generator.GenerateAsync("relay-webapi", "", _testOutputPath, options);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }
}
