using Relay.CLI.Commands;
using System.CommandLine;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;

namespace Relay.CLI.Tests.Commands;

[Collection("Sequential")]
public class InitCommandTests : IDisposable
{
    private readonly string _testPath;

    public InitCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-init-{Guid.NewGuid()}");
    }

    private async Task ExecuteInitWithMockedConsole(string projectName, string template, string outputPath, string framework, bool initGit, bool includeDocker, bool includeCI)
    {
        // Mock console to avoid concurrency issues with Spectre.Console
        // Use a lock to prevent concurrent access to Spectre.Console
        lock (typeof(AnsiConsole))
        {
            var testConsole = new Spectre.Console.Testing.TestConsole();
            var originalConsole = AnsiConsole.Console;

            AnsiConsole.Console = testConsole;

            try
            {
                InitCommand.ExecuteInit(projectName, template, outputPath, framework, initGit, includeDocker, includeCI).Wait();
            }
            finally
            {
                AnsiConsole.Console = originalConsole;
            }
        }
    }

    [Fact]
    public void Create_ReturnsConfiguredCommand()
    {
        // Act
        var command = InitCommand.Create();

        // Assert
        Assert.NotNull(command);
        Assert.Equal("init", command.Name);
        Assert.Equal("Initialize a new Relay project with complete scaffolding", command.Description);

        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");
        Assert.NotNull(nameOption);
        Assert.True(nameOption.IsRequired);

        var templateOption = command.Options.FirstOrDefault(o => o.Name == "template");
        Assert.NotNull(templateOption);
        Assert.False(templateOption.IsRequired);

        var outputOption = command.Options.FirstOrDefault(o => o.Name == "output");
        Assert.NotNull(outputOption);
        Assert.False(outputOption.IsRequired);

        var frameworkOption = command.Options.FirstOrDefault(o => o.Name == "framework");
        Assert.NotNull(frameworkOption);
        Assert.False(frameworkOption.IsRequired);

        var gitOption = command.Options.FirstOrDefault(o => o.Name == "git");
        Assert.NotNull(gitOption);
        Assert.False(gitOption.IsRequired);

        var dockerOption = command.Options.FirstOrDefault(o => o.Name == "docker");
        Assert.NotNull(dockerOption);
        Assert.False(dockerOption.IsRequired);

        var ciOption = command.Options.FirstOrDefault(o => o.Name == "ci");
        Assert.NotNull(ciOption);
        Assert.False(ciOption.IsRequired);
    }

    [Theory]
    [InlineData("minimal")]
    [InlineData("standard")]
    [InlineData("enterprise")]
    public async Task ExecuteInit_WithValidTemplate_CreatesProjectStructure(string template)
    {
        // Arrange
        var projectName = $"TestProject_{template}";
        var projectPath = Path.Combine(_testPath, projectName);

        // Act
        await ExecuteInitWithMockedConsole(projectName, template, _testPath, "net8.0", true, false, false);

        // Assert
        Assert.True(Directory.Exists(projectPath));
        Assert.True(Directory.Exists(Path.Combine(projectPath, "src", projectName)));
        Assert.True(Directory.Exists(Path.Combine(projectPath, "tests", $"{projectName}.Tests")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, "docs")));

        // Check solution file
        Assert.True(File.Exists(Path.Combine(projectPath, $"{projectName}.sln")));

        // Check main project files
        var mainProjectPath = Path.Combine(projectPath, "src", projectName);
        Assert.True(File.Exists(Path.Combine(mainProjectPath, $"{projectName}.csproj")));
        Assert.True(File.Exists(Path.Combine(mainProjectPath, "Program.cs")));

        // Check test project files
        var testProjectPath = Path.Combine(projectPath, "tests", $"{projectName}.Tests");
        Assert.True(File.Exists(Path.Combine(testProjectPath, $"{projectName}.Tests.csproj")));
        Assert.True(File.Exists(Path.Combine(testProjectPath, "SampleTests.cs")));

        // Check configuration files
        Assert.True(File.Exists(Path.Combine(projectPath, "README.md")));
        Assert.True(File.Exists(Path.Combine(projectPath, "appsettings.json")));
        Assert.True(File.Exists(Path.Combine(projectPath, ".relay-cli.json")));
        Assert.True(File.Exists(Path.Combine(projectPath, ".gitignore")));
    }

    [Fact]
    public async Task ExecuteInit_WithExistingDirectory_ThrowsNoException()
    {
        // Arrange
        var projectName = "ExistingProject";
        var projectPath = Path.Combine(_testPath, projectName);
        Directory.CreateDirectory(projectPath);

        // Act & Assert
        // The method should not throw an exception but should handle the existing directory gracefully
        // In the current implementation, it checks for existing directory and returns early
        await ExecuteInitWithMockedConsole(projectName, "standard", _testPath, "net8.0", true, false, false);

        // The directory should still exist but no new files should be created
        Assert.True(Directory.Exists(projectPath));
    }

    [Theory]
    [InlineData("net6.0")]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    public async Task ExecuteInit_WithDifferentFrameworks_SetsCorrectTargetFramework(string framework)
    {
        // Arrange
        var projectName = $"TestProject_{framework.Replace(".", "")}";
        var projectPath = Path.Combine(_testPath, projectName);
        var mainProjectPath = Path.Combine(projectPath, "src", projectName);

        // Act
        await ExecuteInitWithMockedConsole(projectName, "standard", _testPath, framework, true, false, false);

        // Assert
        var csprojContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, $"{projectName}.csproj"));
        Assert.Contains($"<TargetFramework>{framework}</TargetFramework>", csprojContent);
    }

    [Fact]
    public async Task ExecuteInit_WithDockerOption_CreatesDockerFiles()
    {
        // Arrange
        var projectName = "DockerProject";
        var projectPath = Path.Combine(_testPath, projectName);

        // Act
        await ExecuteInitWithMockedConsole(projectName, "standard", _testPath, "net8.0", false, true, false);

        // Assert
        Assert.True(File.Exists(Path.Combine(projectPath, "Dockerfile")));
        Assert.True(File.Exists(Path.Combine(projectPath, "docker-compose.yml")));

        var dockerfileContent = await File.ReadAllTextAsync(Path.Combine(projectPath, "Dockerfile"));
        Assert.Contains("FROM mcr.microsoft.com/dotnet/sdk:8.0", dockerfileContent);
        Assert.Contains($"ENTRYPOINT [\"dotnet\", \"{projectName}.dll\"]", dockerfileContent);

        var dockerComposeContent = await File.ReadAllTextAsync(Path.Combine(projectPath, "docker-compose.yml"));
        Assert.Contains("version: '3.8'", dockerComposeContent);
        Assert.Contains(projectName.ToLower(), dockerComposeContent);
    }

    [Fact]
    public async Task ExecuteInit_WithCIOption_CreatesGitHubActionsWorkflow()
    {
        // Arrange
        var projectName = "CIProject";
        var projectPath = Path.Combine(_testPath, projectName);
        var workflowsPath = Path.Combine(projectPath, ".github", "workflows");

        // Act
        await ExecuteInitWithMockedConsole(projectName, "standard", _testPath, "net8.0", false, false, true);

        // Assert
        Assert.True(Directory.Exists(workflowsPath));
        Assert.True(File.Exists(Path.Combine(workflowsPath, "ci.yml")));

        var ciContent = await File.ReadAllTextAsync(Path.Combine(workflowsPath, "ci.yml"));
        Assert.Contains("name: CI", ciContent);
        Assert.Contains("on:", ciContent);
        Assert.Contains("push:", ciContent);
        Assert.Contains("branches: [ main ]", ciContent);
        Assert.Contains("dotnet build", ciContent);
        Assert.Contains("dotnet test", ciContent);
    }

    [Fact]
    public async Task ExecuteInit_WithGitOption_CreatesGitIgnoreFile()
    {
        // Arrange
        var projectName = "GitProject";
        var projectPath = Path.Combine(_testPath, projectName);

        // Act
        await ExecuteInitWithMockedConsole(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        Assert.True(File.Exists(Path.Combine(projectPath, ".gitignore")));

        var gitignoreContent = await File.ReadAllTextAsync(Path.Combine(projectPath, ".gitignore"));
        Assert.Contains("[Bb]in/", gitignoreContent);
        Assert.Contains("[Oo]bj/", gitignoreContent);
        Assert.Contains(".vs/", gitignoreContent);
        Assert.Contains("*.user", gitignoreContent);
        Assert.Contains("*.suo", gitignoreContent);
        Assert.Contains("*.nupkg", gitignoreContent);
    }

    [Fact]
    public async Task ExecuteInit_EnterpriseTemplate_CreatesAdditionalFolders()
    {
        // Arrange
        var projectName = "EnterpriseProject";
        var projectPath = Path.Combine(_testPath, projectName);
        var mainProjectPath = Path.Combine(projectPath, "src", projectName);

        // Act
        await ExecuteInitWithMockedConsole(projectName, "enterprise", _testPath, "net8.0", true, false, false);

        // Assert
        Assert.True(Directory.Exists(Path.Combine(mainProjectPath, "Handlers")));
        Assert.True(Directory.Exists(Path.Combine(mainProjectPath, "Requests")));
        Assert.True(Directory.Exists(Path.Combine(mainProjectPath, "Responses")));
        Assert.True(Directory.Exists(Path.Combine(mainProjectPath, "Validators")));
        Assert.True(Directory.Exists(Path.Combine(mainProjectPath, "Behaviors")));
    }

    [Fact]
    public async Task ExecuteInit_CreatesSolutionFileWithCorrectContent()
    {
        // Arrange
        var projectName = "SolutionTestProject";
        var projectPath = Path.Combine(_testPath, projectName);

        // Act
        await ExecuteInitWithMockedConsole(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        var slnContent = await File.ReadAllTextAsync(Path.Combine(projectPath, $"{projectName}.sln"));
        Assert.Contains("Microsoft Visual Studio Solution File", slnContent);
        Assert.Contains($"\"{projectName}\"", slnContent);
        Assert.Contains($"\"{projectName}.Tests\"", slnContent);
        Assert.Contains("src\\", slnContent);
        Assert.Contains("tests\\", slnContent);
    }

    [Fact]
    public async Task ExecuteInit_CreatesMainProjectWithCorrectDependencies()
    {
        // Arrange
        var projectName = "MainProjectTest";
        var projectPath = Path.Combine(_testPath, projectName);
        var mainProjectPath = Path.Combine(projectPath, "src", projectName);

        // Act
        await ExecuteInitWithMockedConsole(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        var csprojContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, $"{projectName}.csproj"));
        Assert.Contains("<TargetFramework>net8.0</TargetFramework>", csprojContent);
        Assert.Contains("<LangVersion>latest</LangVersion>", csprojContent);
        Assert.Contains("<Nullable>enable</Nullable>", csprojContent);
        Assert.Contains("<ImplicitUsings>enable</ImplicitUsings>", csprojContent);
        Assert.Contains("<PackageReference Include=\"Relay.Core\" Version=\"2.0.0\" />", csprojContent);
    }

    [Fact]
    public async Task ExecuteInit_CreatesTestProjectWithCorrectDependencies()
    {
        // Arrange
        var projectName = "TestProjectTest";
        var projectPath = Path.Combine(_testPath, projectName);
        var testProjectPath = Path.Combine(projectPath, "tests", $"{projectName}.Tests");

        // Act
        await ExecuteInitWithMockedConsole(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        var csprojContent = await File.ReadAllTextAsync(Path.Combine(testProjectPath, $"{projectName}.Tests.csproj"));
        Assert.Contains("<TargetFramework>net8.0</TargetFramework>", csprojContent);
        Assert.Contains("<IsPackable>false</IsPackable>", csprojContent);
        Assert.Contains("<PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"17.11.0\" />", csprojContent);
        Assert.Contains("<PackageReference Include=\"xunit\" Version=\"2.9.0\" />", csprojContent);
        Assert.Contains("<PackageReference Include=\"FluentAssertions\" Version=\"6.12.0\" />", csprojContent);
        Assert.Contains("<PackageReference Include=\"NSubstitute\" Version=\"5.1.0\" />", csprojContent);
        Assert.Contains($"<ProjectReference Include=\"..\\..\\src\\{projectName}\\{projectName}.csproj\" />", csprojContent);
    }

    [Fact]
    public async Task ExecuteInit_CreatesProgramFileWithCorrectContent()
    {
        // Arrange
        var projectName = "ProgramTestProject";
        var projectPath = Path.Combine(_testPath, projectName);
        var mainProjectPath = Path.Combine(projectPath, "src", projectName);

        // Act
        await ExecuteInitWithMockedConsole(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        var programContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, "Program.cs"));
        Assert.Contains("using Microsoft.Extensions.DependencyInjection;", programContent);
        Assert.Contains("using Microsoft.Extensions.Hosting;", programContent);
        Assert.Contains("using Relay.Core;", programContent);
        Assert.Contains("Host.CreateApplicationBuilder(args)", programContent);
        Assert.Contains("builder.Services.AddRelay();", programContent);
        Assert.Contains($"🚀 {projectName} is running with Relay!", programContent);
    }

    [Fact]
    public async Task ExecuteInit_CreatesReadmeWithCorrectContent()
    {
        // Arrange
        var projectName = "ReadmeTestProject";
        var projectPath = Path.Combine(_testPath, projectName);

        // Act
        await ExecuteInitWithMockedConsole(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        var readmeContent = await File.ReadAllTextAsync(Path.Combine(projectPath, "README.md"));
        Assert.Contains($"# {projectName}", readmeContent);
        Assert.Contains("A high-performance application built with [Relay]", readmeContent);
        Assert.Contains("## 🚀 Getting Started", readmeContent);
        Assert.Contains("dotnet build", readmeContent);
        Assert.Contains("dotnet run", readmeContent);
        Assert.Contains("dotnet test", readmeContent);
        Assert.Contains("## 📖 Project Structure", readmeContent);
        Assert.Contains("## 🎯 Features", readmeContent);
        Assert.Contains("⚡ High-performance request/response handling", readmeContent);
    }

    [Fact]
    public async Task ExecuteInit_CreatesConfigurationFiles()
    {
        // Arrange
        var projectName = "ConfigTestProject";
        var projectPath = Path.Combine(_testPath, projectName);

        // Mock console to avoid concurrency issues with Spectre.Console
        var testConsole = new Spectre.Console.Testing.TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        try
        {
            // Act
            await InitCommand.ExecuteInit(projectName, "standard", _testPath, "net8.0", true, false, false);

            // Assert
            var appsettingsContent = await File.ReadAllTextAsync(Path.Combine(projectPath, "appsettings.json"));
            Assert.Contains("\"version\": \"2.0\"", appsettingsContent);
            Assert.Contains("\"relay\": {", appsettingsContent);
            Assert.Contains("\"enableCaching\": false", appsettingsContent);

            var cliConfigContent = await File.ReadAllTextAsync(Path.Combine(projectPath, ".relay-cli.json"));
            Assert.Contains("\"defaultNamespace\": \"MyApp\"", cliConfigContent);
            Assert.Contains("\"templatePreference\": \"standard\"", cliConfigContent);
            Assert.Contains("\"includeTests\": true", cliConfigContent);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public async Task ExecuteInit_StandardTemplate_CreatesSampleCodeInRoot()
    {
        // Arrange
        var projectName = "Test";
        var projectPath = Path.Combine(_testPath, projectName);
        var mainProjectPath = Path.Combine(projectPath, "src", projectName);

        // Act
        await ExecuteInitWithMockedConsole(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        Assert.True(File.Exists(Path.Combine(mainProjectPath, "GetUserQuery.cs")));
        Assert.True(File.Exists(Path.Combine(mainProjectPath, "UserResponse.cs")));
        Assert.True(File.Exists(Path.Combine(mainProjectPath, "GetUserHandler.cs")));

        var queryContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, "GetUserQuery.cs"));
        Assert.Contains($"namespace {projectName}.Requests;", queryContent);
        Assert.Contains("public record GetUserQuery(int UserId) : IRequest<UserResponse>;", queryContent);

        var responseContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, "UserResponse.cs"));
        Assert.Contains($"namespace {projectName}.Requests;", responseContent);
        Assert.Contains("public record UserResponse(int Id, string Name, string Email);", responseContent);

        var handlerContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, "GetUserHandler.cs"));
        Assert.Contains($"namespace {projectName}.Handlers;", handlerContent);
        Assert.Contains("public class GetUserHandler : IRequestHandler<GetUserQuery, UserResponse>", handlerContent);
        Assert.Contains("[Handle]", handlerContent);
        Assert.Contains("ValueTask<UserResponse>", handlerContent);
    }

    [Fact]
    public async Task ExecuteInit_EnterpriseTemplate_CreatesSampleCodeInFolders()
    {
        // Arrange
        var projectName = "EnterpriseSampleTest";
        var projectPath = Path.Combine(_testPath, projectName);
        var mainProjectPath = Path.Combine(projectPath, "src", projectName);

        // Act
        await ExecuteInitWithMockedConsole(projectName, "enterprise", _testPath, "net8.0", true, false, false);

        // Assert
        Assert.True(File.Exists(Path.Combine(mainProjectPath, "Requests", "GetUserQuery.cs")));
        Assert.True(File.Exists(Path.Combine(mainProjectPath, "Responses", "UserResponse.cs")));
        Assert.True(File.Exists(Path.Combine(mainProjectPath, "Handlers", "GetUserHandler.cs")));

        var queryContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, "Requests", "GetUserQuery.cs"));
        Assert.Contains($"namespace {projectName}.Requests;", queryContent);

        var responseContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, "Responses", "UserResponse.cs"));
        Assert.Contains($"namespace {projectName}.Requests;", responseContent);

        var handlerContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, "Handlers", "GetUserHandler.cs"));
        Assert.Contains($"namespace {projectName}.Handlers;", handlerContent);
    }

    [Fact]
    public async Task ExecuteInit_CreatesSampleTestFile()
    {
        // Arrange
        var projectName = "SampleTestTest";
        var projectPath = Path.Combine(_testPath, projectName);
        var testProjectPath = Path.Combine(projectPath, "tests", $"{projectName}.Tests");

        // Act
        await ExecuteInitWithMockedConsole(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        var testContent = await File.ReadAllTextAsync(Path.Combine(testProjectPath, "SampleTests.cs"));
        Assert.Contains($"namespace {projectName}.Tests;", testContent);
        Assert.Contains("public class SampleTests", testContent);
        Assert.Contains("[Fact]", testContent);
        Assert.Contains("SampleTest_ShouldPass", testContent);
        Assert.Contains("using Xunit;", testContent);
        Assert.Contains("", testContent);
    }

    [Fact]
    public async Task ExecuteInit_CustomOutputDirectory_CreatesProjectInCorrectLocation()
    {
        // Arrange
        var projectName = "CustomOutputTest";
        var customOutputPath = Path.Combine(_testPath, "custom");
        Directory.CreateDirectory(customOutputPath);

        // Act
        await ExecuteInitWithMockedConsole(projectName, "standard", customOutputPath, "net8.0", true, false, false);

        // Assert
        var expectedProjectPath = Path.Combine(customOutputPath, projectName);
        Assert.True(Directory.Exists(expectedProjectPath));
        Assert.True(File.Exists(Path.Combine(expectedProjectPath, $"{projectName}.sln")));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testPath))
                Directory.Delete(_testPath, true);
        }
        catch { }
    }
}


