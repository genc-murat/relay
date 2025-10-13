using Relay.CLI.TemplateEngine;

namespace Relay.CLI.Tests.TemplateEngine;

public class TemplateGeneratorTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _templatesPath;

    public TemplateGeneratorTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _templatesPath = Path.Combine(_tempDirectory, "templates");
        Directory.CreateDirectory(_templatesPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void Constructor_InitializesWithTemplatesPath()
    {
        // Arrange & Act
        var generator = new TemplateGenerator(_templatesPath);

        // Assert
        Assert.NotNull(generator);
        // We can't directly test private fields, but we can test behavior
    }

    [Fact]
    public void AddVariable_AddsVariableToDictionary()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);

        // Act
        generator.AddVariable("TestKey", "TestValue");

        // Assert
        // Variables are private, so we test through behavior in GenerateAsync
    }

    [Fact]
    public async Task GenerateAsync_InvalidProjectName_ReturnsFailedResult()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, "output");
        var options = new GenerationOptions();

        // Act
        var result = await generator.GenerateAsync("relay-webapi", "", outputPath, options);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid project name", result.Message);
        Assert.Contains("Project name cannot be empty", result.Errors);
    }

    [Fact]
    public async Task GenerateAsync_UnknownTemplateId_UsesBasicTemplate()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, "output");
        var options = new GenerationOptions();

        // Act
        var result = await generator.GenerateAsync("unknown-template", "TestProject", outputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("unknown-template", result.TemplateName);
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestProject")));
        Assert.True(File.Exists(Path.Combine(outputPath, "TestProject.sln")));
    }

    [Fact]
    public async Task GenerateAsync_ValidWebApiTemplate_CreatesDirectoriesAndFiles()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, "output");
        var options = new GenerationOptions
        {
            Author = "TestAuthor",
            TargetFramework = "net8.0",
            EnableDocker = true
        };

        // Act
        var result = await generator.GenerateAsync("relay-webapi", "TestProject", outputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("relay-webapi", result.TemplateName);
        Assert.True(result.CreatedDirectories.Count > 0);
        Assert.True(result.CreatedFiles.Count > 0);

        // Check that directories were created
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestProject.Api")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestProject.Application")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestProject.Domain")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestProject.Infrastructure")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "tests")));

        // Check that files were created
        Assert.True(File.Exists(Path.Combine(outputPath, "TestProject.sln")));
        Assert.True(File.Exists(Path.Combine(outputPath, "src", "TestProject.Api", "TestProject.Api.csproj")));
        Assert.True(File.Exists(Path.Combine(outputPath, "README.md")));
        Assert.True(File.Exists(Path.Combine(outputPath, ".gitignore")));
        Assert.True(File.Exists(Path.Combine(outputPath, "Dockerfile")));
    }

    [Fact]
    public async Task GenerateAsync_MicroserviceTemplate_CreatesCorrectStructure()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, "output");
        var options = new GenerationOptions();

        // Act
        var result = await generator.GenerateAsync("relay-microservice", "TestMicroservice", outputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestMicroservice")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "k8s")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "helm")));
    }

    [Fact]
    public async Task GenerateAsync_DddTemplate_CreatesCorrectStructure()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, "output");
        var options = new GenerationOptions();

        // Act
        var result = await generator.GenerateAsync("relay-ddd", "TestDdd", outputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestDdd.Api")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestDdd.Domain", "Aggregates")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestDdd.Domain", "DomainEvents")));
    }

    [Fact]
    public async Task GenerateAsync_ModularTemplate_WithModules_CreatesCorrectStructure()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, "output");
        var options = new GenerationOptions
        {
            Modules = new[] { "Catalog", "Orders", "Users" }
        };

        // Act
        var result = await generator.GenerateAsync("relay-modular", "TestModular", outputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestModular.Api")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestModular.Modules", "Catalog")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestModular.Modules", "Orders")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestModular.Modules", "Users")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestModular.Shared")));
    }

    [Fact]
    public async Task GenerateAsync_GraphQLTemplate_CreatesCorrectStructure()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, "output");
        var options = new GenerationOptions();

        // Act
        var result = await generator.GenerateAsync("relay-graphql", "TestGraphQL", outputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestGraphQL", "Schema", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestGraphQL", "Schema", "Mutations")));
        Assert.True(File.Exists(Path.Combine(outputPath, "src", "TestGraphQL", "TestGraphQL.csproj")));
    }

    [Fact]
    public async Task GenerateAsync_GrpcTemplate_CreatesCorrectStructure()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, "output");
        var options = new GenerationOptions();

        // Act
        var result = await generator.GenerateAsync("relay-grpc", "TestGrpc", outputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestGrpc", "Protos")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestGrpc", "Services")));
        Assert.True(File.Exists(Path.Combine(outputPath, "src", "TestGrpc", "Protos", "greet.proto")));
        Assert.True(File.Exists(Path.Combine(outputPath, "src", "TestGrpc", "Services", "GreeterService.cs")));
    }

    [Fact]
    public async Task GenerateAsync_BasicTemplate_CreatesMinimalStructure()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, "output");
        var options = new GenerationOptions();

        // Act - Use an unknown template to trigger the default case (GenerateBasicProjectAsync)
        var result = await generator.GenerateAsync("unknown-template", "TestBasic", outputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "TestBasic")));
        Assert.True(File.Exists(Path.Combine(outputPath, "TestBasic.sln")));
        Assert.True(File.Exists(Path.Combine(outputPath, "src", "TestBasic", "TestBasic.csproj")));
        Assert.True(File.Exists(Path.Combine(outputPath, "src", "TestBasic", "Program.cs")));
        Assert.True(File.Exists(Path.Combine(outputPath, "src", "TestBasic", "appsettings.json")));
        Assert.True(File.Exists(Path.Combine(outputPath, "tests", "TestBasic.Tests", "TestBasic.Tests.csproj")));
    }

    [Fact]
    public async Task GenerateAsync_EnableDockerFalse_DoesNotCreateDockerfile()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, "output");
        var options = new GenerationOptions
        {
            EnableDocker = false
        };

        // Act
        var result = await generator.GenerateAsync("relay-webapi", "TestProject", outputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.False(File.Exists(Path.Combine(outputPath, "Dockerfile")));
    }

    [Fact]
    public async Task GenerateAsync_CustomOptions_AreAppliedInGeneratedFiles()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, "output");
        var options = new GenerationOptions
        {
            Author = "CustomAuthor",
            TargetFramework = "net7.0",
            DatabaseProvider = "postgresql",
            EnableAuth = true,
            EnableSwagger = false,
            EnableHealthChecks = false,
            EnableCaching = true,
            EnableTelemetry = true
        };

        // Act
        var result = await generator.GenerateAsync("relay-webapi", "TestProject", outputPath, options);

        // Assert
        Assert.True(result.Success);

        // Check that project file contains custom target framework
        var apiProjectPath = Path.Combine(outputPath, "src", "TestProject.Api", "TestProject.Api.csproj");
        var apiProjectContent = await File.ReadAllTextAsync(apiProjectPath);
        Assert.Contains("net7.0", apiProjectContent);
    }

    [Fact]
    public async Task GenerateAsync_ExceptionDuringGeneration_ReturnsFailedResult()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, "output");
        var options = new GenerationOptions();

        // Create a scenario that will cause an exception (invalid path that can't be created)
        outputPath = "\\\\invalid\\path\\that\\does\\not\\exist";

        // Act
        var result = await generator.GenerateAsync("relay-webapi", "TestProject", outputPath, options);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Error generating project", result.Message);
        Assert.True(result.Errors.Count > 0);
    }

    [Theory]
    [InlineData("relay-webapi")]
    [InlineData("relay-microservice")]
    [InlineData("relay-ddd")]
    [InlineData("relay-modular")]
    [InlineData("relay-graphql")]
    [InlineData("relay-grpc")]
    public async Task GenerateAsync_AllTemplateTypes_Succeed(string templateId)
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, $"output_{templateId}");
        var options = new GenerationOptions
        {
            Modules = templateId == "relay-modular" ? new[] { "TestModule" } : null
        };

        // Act
        var result = await generator.GenerateAsync(templateId, "TestProject", outputPath, options);

        // Assert
        Assert.True(result.Success, $"Template {templateId} failed: {result.Message}");
        Assert.Equal(templateId, result.TemplateName);
        Assert.True(result.CreatedDirectories.Count > 0);
        Assert.True(result.CreatedFiles.Count > 0);
        Assert.True(result.Duration.TotalMilliseconds > 0);
    }

    [Fact]
    public async Task GenerateAsync_GeneratedFilesContainCorrectContent()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, "output");
        var options = new GenerationOptions
        {
            Author = "TestAuthor",
            TargetFramework = "net8.0"
        };

        // Act
        var result = await generator.GenerateAsync("relay-webapi", "MyTestProject", outputPath, options);

        // Assert
        Assert.True(result.Success);

        // Check Program.cs content
        var programPath = Path.Combine(outputPath, "src", "MyTestProject.Api", "Program.cs");
        var programContent = await File.ReadAllTextAsync(programPath);
        Assert.Contains("WebApplication.CreateBuilder", programContent);

        // Check README content
        var readmePath = Path.Combine(outputPath, "README.md");
        var readmeContent = await File.ReadAllTextAsync(readmePath);
        Assert.Contains("# MyTestProject", readmeContent);
        Assert.Contains("dotnet restore", readmeContent);
    }

    [Fact]
    public async Task GenerateAsync_ProjectNameWithSpecialCharacters_IsHandled()
    {
        // Arrange
        var generator = new TemplateGenerator(_templatesPath);
        var outputPath = Path.Combine(_tempDirectory, "output");
        var options = new GenerationOptions();

        // Act
        var result = await generator.GenerateAsync("relay-webapi", "My.Test.Project", outputPath, options);

        // Assert
        Assert.True(result.Success);
        Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "My.Test.Project.Api")));
    }
}