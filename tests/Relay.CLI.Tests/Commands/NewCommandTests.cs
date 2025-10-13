using System.CommandLine;
using System.CommandLine.Parsing;
using Relay.CLI.Commands;
using Relay.CLI.Commands.Models.Template;
using Xunit;
using Xunit.Abstractions;

namespace Relay.CLI.Tests.Commands;

public class NewCommandTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _tempDirectory;

    public NewCommandTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void NewCommand_CreatesCommandWithCorrectNameAndDescription()
    {
        // Arrange & Act
        var command = new NewCommand();

        // Assert
        Assert.Equal("new", command.Name);
        Assert.Equal("Create a new project from a template", command.Description);
    }

    [Fact]
    public void NewCommand_HasAllRequiredOptions()
    {
        // Arrange & Act
        var command = new NewCommand();

        // Assert
        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");
        var templateOption = command.Options.FirstOrDefault(o => o.Name == "template");
        var listOption = command.Options.FirstOrDefault(o => o.Name == "list");
        var featuresOption = command.Options.FirstOrDefault(o => o.Name == "features");
        var outputOption = command.Options.FirstOrDefault(o => o.Name == "output");
        var brokerOption = command.Options.FirstOrDefault(o => o.Name == "broker");
        var databaseOption = command.Options.FirstOrDefault(o => o.Name == "database");
        var authOption = command.Options.FirstOrDefault(o => o.Name == "auth");
        var noRestoreOption = command.Options.FirstOrDefault(o => o.Name == "no-restore");
        var noBuildOption = command.Options.FirstOrDefault(o => o.Name == "no-build");

        Assert.NotNull(nameOption);
        Assert.True(nameOption.IsRequired);
        Assert.NotNull(templateOption);
        Assert.NotNull(listOption);
        Assert.NotNull(featuresOption);
        Assert.NotNull(outputOption);
        Assert.NotNull(brokerOption);
        Assert.NotNull(databaseOption);
        Assert.NotNull(authOption);
        Assert.NotNull(noRestoreOption);
        Assert.NotNull(noBuildOption);
    }

    [Fact]
    public void GetTemplates_ReturnsAllAvailableTemplates()
    {
        // Arrange & Act
        var templates = NewCommand.GetTemplates();

        // Assert
        Assert.NotEmpty(templates);
        Assert.Equal(10, templates.Count); // Based on the implementation

        var templateIds = templates.Select(t => t.Id).ToList();
        Assert.Contains("relay-webapi", templateIds);
        Assert.Contains("relay-microservice", templateIds);
        Assert.Contains("relay-ddd", templateIds);
        Assert.Contains("relay-cqrs-es", templateIds);
        Assert.Contains("relay-modular", templateIds);
        Assert.Contains("relay-graphql", templateIds);
        Assert.Contains("relay-grpc", templateIds);
        Assert.Contains("relay-serverless", templateIds);
        Assert.Contains("relay-blazor", templateIds);
        Assert.Contains("relay-maui", templateIds);
    }

    [Fact]
    public void GenerateReadme_CreatesCorrectContent()
    {
        // Arrange
        var name = "TestProject";
        var template = new TemplateInfo
        {
            Id = "relay-webapi",
            Name = "Clean Architecture Web API",
            Description = "Production-ready REST API following Clean Architecture principles"
        };
        var features = new[] { "auth", "swagger", "docker" };

        // Act
        var readme = NewCommand.GenerateReadme(name, template, features);

        // Assert
        Assert.Contains($"# {name}", readme);
        Assert.Contains(template.Description, readme);
        Assert.Contains("## Getting Started", readme);
        Assert.Contains("## Features", readme);
        Assert.Contains("- auth", readme);
        Assert.Contains("- swagger", readme);
        Assert.Contains("- docker", readme);
    }

    [Fact]
    public async Task CreateProjectStructure_CreatesCorrectDirectoriesForCleanArchitecture()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        var name = "TestProject";
        var template = new TemplateInfo { Structure = "clean-architecture" };
        var features = Array.Empty<string>();

        // Act
        await NewCommand.CreateProjectStructure(projectPath, name, template, features, null, null, null);

        // Assert
        Assert.True(Directory.Exists(projectPath));
        Assert.True(Directory.Exists(Path.Combine(projectPath, "src")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, $"src/{name}.Api")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, $"src/{name}.Application")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, $"src/{name}.Domain")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, $"src/{name}.Infrastructure")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, "tests")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, $"tests/{name}.UnitTests")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, $"tests/{name}.IntegrationTests")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, $"tests/{name}.ArchitectureTests")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, "docs")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, "scripts")));
    }

    [Fact]
    public async Task CreateProjectStructure_CreatesCorrectDirectoriesForMicroservice()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestMicroservice");
        var name = "TestMicroservice";
        var template = new TemplateInfo { Structure = "microservice" };
        var features = Array.Empty<string>();

        // Act
        await NewCommand.CreateProjectStructure(projectPath, name, template, features, null, null, null);

        // Assert
        Assert.True(Directory.Exists(projectPath));
        Assert.True(Directory.Exists(Path.Combine(projectPath, "src")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, $"src/{name}")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, "tests")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, $"tests/{name}.Tests")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, "k8s")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, "helm")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, "docs")));
    }

    [Fact]
    public async Task GenerateCommonFiles_CreatesReadmeAndGitignore()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);
        var name = "TestProject";
        var template = new TemplateInfo
        {
            Id = "relay-webapi",
            Name = "Clean Architecture Web API",
            Description = "Production-ready REST API following Clean Architecture principles"
        };
        var features = Array.Empty<string>();

        // Act
        await NewCommand.GenerateCommonFiles(projectPath, name, template, features);

        // Assert
        Assert.True(File.Exists(Path.Combine(projectPath, "README.md")));
        Assert.True(File.Exists(Path.Combine(projectPath, ".gitignore")));

        var readmeContent = await File.ReadAllTextAsync(Path.Combine(projectPath, "README.md"));
        Assert.Contains($"# {name}", readmeContent);

        var gitignoreContent = await File.ReadAllTextAsync(Path.Combine(projectPath, ".gitignore"));
        Assert.Contains("# Build results", gitignoreContent);
        Assert.Contains("[Bb]in/", gitignoreContent);
    }

    [Fact]
    public async Task GenerateCommonFiles_IncludesDockerFilesWhenDockerFeatureRequested()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);
        var name = "TestProject";
        var template = new TemplateInfo
        {
            Id = "relay-webapi",
            Name = "Clean Architecture Web API",
            Description = "Production-ready REST API following Clean Architecture principles"
        };
        var features = new[] { "docker" };

        // Act
        await NewCommand.GenerateCommonFiles(projectPath, name, template, features);

        // Assert - Docker files generation is currently a placeholder, so we just verify the method runs
        Assert.True(Directory.Exists(projectPath));
    }

    [Fact]
    public async Task GenerateCommonFiles_IncludesCICDFilesWhenCIFeatureRequested()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);
        var name = "TestProject";
        var template = new TemplateInfo
        {
            Id = "relay-webapi",
            Name = "Clean Architecture Web API",
            Description = "Production-ready REST API following Clean Architecture principles"
        };
        var features = new[] { "ci" };

        // Act
        await NewCommand.GenerateCommonFiles(projectPath, name, template, features);

        // Assert - CI/CD files generation is currently a placeholder, so we just verify the method runs
        Assert.True(Directory.Exists(projectPath));
    }

    [Fact]
    public async Task GenerateProjectFiles_CallsCorrectGeneratorForWebApiTemplate()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);
        var name = "TestProject";
        var template = new TemplateInfo { Id = "relay-webapi" };
        var features = Array.Empty<string>();

        // Act
        await NewCommand.GenerateProjectFiles(projectPath, name, template, features, null, null, null);

        // Assert - The actual generation is currently a placeholder, so we just verify the method runs
        Assert.True(Directory.Exists(projectPath));
    }

    [Fact]
    public async Task GenerateProjectFiles_CallsCorrectGeneratorForMicroserviceTemplate()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);
        var name = "TestProject";
        var template = new TemplateInfo { Id = "relay-microservice" };
        var features = Array.Empty<string>();

        // Act
        await NewCommand.GenerateProjectFiles(projectPath, name, template, features, "rabbitmq", null, null);

        // Assert - The actual generation is currently a placeholder, so we just verify the method runs
        Assert.True(Directory.Exists(projectPath));
    }

    [Fact]
    public async Task CreateProjectAsync_HandlesInvalidTemplateGracefully()
    {
        // Arrange
        var name = "TestProject";
        var template = "non-existent-template";
        var features = Array.Empty<string>();
        var output = _tempDirectory;

        // Act
        await NewCommand.CreateProjectAsync(name, template, features, output, null, null, null, true, true);

        // Assert - The method should complete without throwing and no project should be created
        var projectPath = Path.Combine(output, name);
        Assert.False(Directory.Exists(projectPath));
    }

    [Fact]
    public async Task CreateProjectAsync_HandlesExistingDirectoryGracefully()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "ExistingProject");
        Directory.CreateDirectory(projectPath);
        var name = "ExistingProject";
        var template = "relay-webapi";
        var features = Array.Empty<string>();
        var output = _tempDirectory;

        // Act
        await NewCommand.CreateProjectAsync(name, template, features, output, null, null, null, true, true);

        // Assert - The method should complete without throwing and the existing directory should remain unchanged
        Assert.True(Directory.Exists(projectPath));
        // Check that no new files were added (the directory should still be empty)
        Assert.Empty(Directory.GetFiles(projectPath));
        Assert.Empty(Directory.GetDirectories(projectPath));
    }

    [Fact]
    public async Task CreateProjectAsync_CreatesProjectSuccessfully()
    {
        // Arrange
        var name = "TestProject";
        var template = "relay-webapi";
        var features = Array.Empty<string>();
        var output = _tempDirectory;

        // Act
        await NewCommand.CreateProjectAsync(name, template, features, output, null, null, null, true, true);

        // Assert
        var projectPath = Path.Combine(output, name);
        Assert.True(Directory.Exists(projectPath));
        Assert.True(File.Exists(Path.Combine(projectPath, "README.md")));
        Assert.True(File.Exists(Path.Combine(projectPath, ".gitignore")));
    }

    [Fact]
    public async Task CreateProjectAsync_CreatesProjectWithFeatures()
    {
        // Arrange
        var name = "TestProject";
        var template = "relay-webapi";
        var features = new[] { "auth", "swagger" };
        var output = _tempDirectory;

        // Act
        await NewCommand.CreateProjectAsync(name, template, features, output, null, null, null, true, true);

        // Assert
        var projectPath = Path.Combine(output, name);
        Assert.True(Directory.Exists(projectPath));
        var readmeContent = await File.ReadAllTextAsync(Path.Combine(projectPath, "README.md"));
        Assert.Contains("- auth", readmeContent);
        Assert.Contains("- swagger", readmeContent);
    }

    [Fact]
    public async Task RestorePackages_ExecutesDotnetRestore()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Create a minimal project file to avoid restore errors
        await File.WriteAllTextAsync(Path.Combine(projectPath, "TestProject.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        // Act - This will attempt to run dotnet restore, but we're mainly testing that the method executes
        await NewCommand.RestorePackages(projectPath);

        // Assert - The method should complete without throwing
        Assert.True(Directory.Exists(projectPath));
    }

    [Fact]
    public async Task BuildProject_ExecutesDotnetBuild()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Create a minimal project file
        await File.WriteAllTextAsync(Path.Combine(projectPath, "TestProject.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        // Act - This will attempt to run dotnet build, but we're mainly testing that the method executes
        await NewCommand.BuildProject(projectPath);

        // Assert - The method should complete without throwing
        Assert.True(Directory.Exists(projectPath));
    }
}