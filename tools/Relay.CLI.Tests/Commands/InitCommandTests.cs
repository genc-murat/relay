using Relay.CLI.Commands;
using System.CommandLine;

namespace Relay.CLI.Tests.Commands;

public class InitCommandTests : IDisposable
{
    private readonly string _testPath;

    public InitCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-init-{Guid.NewGuid()}");
    }

    [Fact]
    public void Create_ReturnsConfiguredCommand()
    {
        // Act
        var command = InitCommand.Create();

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("init");
        command.Description.Should().Be("Initialize a new Relay project with complete scaffolding");

        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");
        nameOption.Should().NotBeNull();
        nameOption.IsRequired.Should().BeTrue();

        var templateOption = command.Options.FirstOrDefault(o => o.Name == "template");
        templateOption.Should().NotBeNull();
        templateOption.IsRequired.Should().BeFalse();

        var outputOption = command.Options.FirstOrDefault(o => o.Name == "output");
        outputOption.Should().NotBeNull();
        outputOption.IsRequired.Should().BeFalse();

        var frameworkOption = command.Options.FirstOrDefault(o => o.Name == "framework");
        frameworkOption.Should().NotBeNull();
        frameworkOption.IsRequired.Should().BeFalse();

        var gitOption = command.Options.FirstOrDefault(o => o.Name == "git");
        gitOption.Should().NotBeNull();
        gitOption.IsRequired.Should().BeFalse();

        var dockerOption = command.Options.FirstOrDefault(o => o.Name == "docker");
        dockerOption.Should().NotBeNull();
        dockerOption.IsRequired.Should().BeFalse();

        var ciOption = command.Options.FirstOrDefault(o => o.Name == "ci");
        ciOption.Should().NotBeNull();
        ciOption.IsRequired.Should().BeFalse();
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
        await InitCommand.ExecuteInit(projectName, template, _testPath, "net8.0", true, false, false);

        // Assert
        Directory.Exists(projectPath).Should().BeTrue();
        Directory.Exists(Path.Combine(projectPath, "src", projectName)).Should().BeTrue();
        Directory.Exists(Path.Combine(projectPath, "tests", $"{projectName}.Tests")).Should().BeTrue();
        Directory.Exists(Path.Combine(projectPath, "docs")).Should().BeTrue();

        // Check solution file
        File.Exists(Path.Combine(projectPath, $"{projectName}.sln")).Should().BeTrue();

        // Check main project files
        var mainProjectPath = Path.Combine(projectPath, "src", projectName);
        File.Exists(Path.Combine(mainProjectPath, $"{projectName}.csproj")).Should().BeTrue();
        File.Exists(Path.Combine(mainProjectPath, "Program.cs")).Should().BeTrue();

        // Check test project files
        var testProjectPath = Path.Combine(projectPath, "tests", $"{projectName}.Tests");
        File.Exists(Path.Combine(testProjectPath, $"{projectName}.Tests.csproj")).Should().BeTrue();
        File.Exists(Path.Combine(testProjectPath, "SampleTests.cs")).Should().BeTrue();

        // Check configuration files
        File.Exists(Path.Combine(projectPath, "README.md")).Should().BeTrue();
        File.Exists(Path.Combine(projectPath, "appsettings.json")).Should().BeTrue();
        File.Exists(Path.Combine(projectPath, ".relay-cli.json")).Should().BeTrue();
        File.Exists(Path.Combine(projectPath, ".gitignore")).Should().BeTrue();
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
        await InitCommand.ExecuteInit(projectName, "standard", _testPath, "net8.0", true, false, false);

        // The directory should still exist but no new files should be created
        Directory.Exists(projectPath).Should().BeTrue();
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
        await InitCommand.ExecuteInit(projectName, "standard", _testPath, framework, true, false, false);

        // Assert
        var csprojContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, $"{projectName}.csproj"));
        csprojContent.Should().Contain($"<TargetFramework>{framework}</TargetFramework>");
    }

    [Fact]
    public async Task ExecuteInit_WithDockerOption_CreatesDockerFiles()
    {
        // Arrange
        var projectName = "DockerProject";
        var projectPath = Path.Combine(_testPath, projectName);

        // Act
        await InitCommand.ExecuteInit(projectName, "standard", _testPath, "net8.0", false, true, false);

        // Assert
        File.Exists(Path.Combine(projectPath, "Dockerfile")).Should().BeTrue();
        File.Exists(Path.Combine(projectPath, "docker-compose.yml")).Should().BeTrue();

        var dockerfileContent = await File.ReadAllTextAsync(Path.Combine(projectPath, "Dockerfile"));
        dockerfileContent.Should().Contain("FROM mcr.microsoft.com/dotnet/sdk:8.0");
        dockerfileContent.Should().Contain($"ENTRYPOINT [\"dotnet\", \"{projectName}.dll\"]");

        var dockerComposeContent = await File.ReadAllTextAsync(Path.Combine(projectPath, "docker-compose.yml"));
        dockerComposeContent.Should().Contain("version: '3.8'");
        dockerComposeContent.Should().Contain(projectName.ToLower());
    }

    [Fact]
    public async Task ExecuteInit_WithCIOption_CreatesGitHubActionsWorkflow()
    {
        // Arrange
        var projectName = "CIProject";
        var projectPath = Path.Combine(_testPath, projectName);
        var workflowsPath = Path.Combine(projectPath, ".github", "workflows");

        // Act
        await InitCommand.ExecuteInit(projectName, "standard", _testPath, "net8.0", false, false, true);

        // Assert
        Directory.Exists(workflowsPath).Should().BeTrue();
        File.Exists(Path.Combine(workflowsPath, "ci.yml")).Should().BeTrue();

        var ciContent = await File.ReadAllTextAsync(Path.Combine(workflowsPath, "ci.yml"));
        ciContent.Should().Contain("name: CI");
        ciContent.Should().Contain("on:");
        ciContent.Should().Contain("push:");
        ciContent.Should().Contain("branches: [ main ]");
        ciContent.Should().Contain("dotnet build");
        ciContent.Should().Contain("dotnet test");
    }

    [Fact]
    public async Task ExecuteInit_WithGitOption_CreatesGitIgnoreFile()
    {
        // Arrange
        var projectName = "GitProject";
        var projectPath = Path.Combine(_testPath, projectName);

        // Act
        await InitCommand.ExecuteInit(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        File.Exists(Path.Combine(projectPath, ".gitignore")).Should().BeTrue();

        var gitignoreContent = await File.ReadAllTextAsync(Path.Combine(projectPath, ".gitignore"));
        gitignoreContent.Should().Contain("[Bb]in/");
        gitignoreContent.Should().Contain("[Oo]bj/");
        gitignoreContent.Should().Contain(".vs/");
        gitignoreContent.Should().Contain("*.user");
        gitignoreContent.Should().Contain("*.suo");
        gitignoreContent.Should().Contain("*.nupkg");
    }

    [Fact]
    public async Task ExecuteInit_EnterpriseTemplate_CreatesAdditionalFolders()
    {
        // Arrange
        var projectName = "EnterpriseProject";
        var projectPath = Path.Combine(_testPath, projectName);
        var mainProjectPath = Path.Combine(projectPath, "src", projectName);

        // Act
        await InitCommand.ExecuteInit(projectName, "enterprise", _testPath, "net8.0", true, false, false);

        // Assert
        Directory.Exists(Path.Combine(mainProjectPath, "Handlers")).Should().BeTrue();
        Directory.Exists(Path.Combine(mainProjectPath, "Requests")).Should().BeTrue();
        Directory.Exists(Path.Combine(mainProjectPath, "Responses")).Should().BeTrue();
        Directory.Exists(Path.Combine(mainProjectPath, "Validators")).Should().BeTrue();
        Directory.Exists(Path.Combine(mainProjectPath, "Behaviors")).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteInit_CreatesSolutionFileWithCorrectContent()
    {
        // Arrange
        var projectName = "SolutionTestProject";
        var projectPath = Path.Combine(_testPath, projectName);

        // Act
        await InitCommand.ExecuteInit(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        var slnContent = await File.ReadAllTextAsync(Path.Combine(projectPath, $"{projectName}.sln"));
        slnContent.Should().Contain("Microsoft Visual Studio Solution File");
        slnContent.Should().Contain($"\"{projectName}\"");
        slnContent.Should().Contain($"\"{projectName}.Tests\"");
        slnContent.Should().Contain("src\\");
        slnContent.Should().Contain("tests\\");
    }

    [Fact]
    public async Task ExecuteInit_CreatesMainProjectWithCorrectDependencies()
    {
        // Arrange
        var projectName = "MainProjectTest";
        var projectPath = Path.Combine(_testPath, projectName);
        var mainProjectPath = Path.Combine(projectPath, "src", projectName);

        // Act
        await InitCommand.ExecuteInit(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        var csprojContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, $"{projectName}.csproj"));
        csprojContent.Should().Contain("<TargetFramework>net8.0</TargetFramework>");
        csprojContent.Should().Contain("<LangVersion>latest</LangVersion>");
        csprojContent.Should().Contain("<Nullable>enable</Nullable>");
        csprojContent.Should().Contain("<ImplicitUsings>enable</ImplicitUsings>");
        csprojContent.Should().Contain("<PackageReference Include=\"Relay.Core\" Version=\"2.0.0\" />");
    }

    [Fact]
    public async Task ExecuteInit_CreatesTestProjectWithCorrectDependencies()
    {
        // Arrange
        var projectName = "TestProjectTest";
        var projectPath = Path.Combine(_testPath, projectName);
        var testProjectPath = Path.Combine(projectPath, "tests", $"{projectName}.Tests");

        // Act
        await InitCommand.ExecuteInit(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        var csprojContent = await File.ReadAllTextAsync(Path.Combine(testProjectPath, $"{projectName}.Tests.csproj"));
        csprojContent.Should().Contain("<TargetFramework>net8.0</TargetFramework>");
        csprojContent.Should().Contain("<IsPackable>false</IsPackable>");
        csprojContent.Should().Contain("<PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"17.11.0\" />");
        csprojContent.Should().Contain("<PackageReference Include=\"xunit\" Version=\"2.9.0\" />");
        csprojContent.Should().Contain("<PackageReference Include=\"FluentAssertions\" Version=\"6.12.0\" />");
        csprojContent.Should().Contain("<PackageReference Include=\"NSubstitute\" Version=\"5.1.0\" />");
        csprojContent.Should().Contain($"<ProjectReference Include=\"..\\..\\src\\{projectName}\\{projectName}.csproj\" />");
    }

    [Fact]
    public async Task ExecuteInit_CreatesProgramFileWithCorrectContent()
    {
        // Arrange
        var projectName = "ProgramTestProject";
        var projectPath = Path.Combine(_testPath, projectName);
        var mainProjectPath = Path.Combine(projectPath, "src", projectName);

        // Act
        await InitCommand.ExecuteInit(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        var programContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, "Program.cs"));
        programContent.Should().Contain("using Microsoft.Extensions.DependencyInjection;");
        programContent.Should().Contain("using Microsoft.Extensions.Hosting;");
        programContent.Should().Contain("using Relay.Core;");
        programContent.Should().Contain("Host.CreateApplicationBuilder(args)");
        programContent.Should().Contain("builder.Services.AddRelay();");
        programContent.Should().Contain($"🚀 {projectName} is running with Relay!");
    }

    [Fact]
    public async Task ExecuteInit_CreatesReadmeWithCorrectContent()
    {
        // Arrange
        var projectName = "ReadmeTestProject";
        var projectPath = Path.Combine(_testPath, projectName);

        // Act
        await InitCommand.ExecuteInit(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        var readmeContent = await File.ReadAllTextAsync(Path.Combine(projectPath, "README.md"));
        readmeContent.Should().Contain($"# {projectName}");
        readmeContent.Should().Contain("A high-performance application built with [Relay]");
        readmeContent.Should().Contain("## 🚀 Getting Started");
        readmeContent.Should().Contain("dotnet build");
        readmeContent.Should().Contain("dotnet run");
        readmeContent.Should().Contain("dotnet test");
        readmeContent.Should().Contain("## 📖 Project Structure");
        readmeContent.Should().Contain("## 🎯 Features");
        readmeContent.Should().Contain("⚡ High-performance request/response handling");
    }

    [Fact]
    public async Task ExecuteInit_CreatesConfigurationFiles()
    {
        // Arrange
        var projectName = "ConfigTestProject";
        var projectPath = Path.Combine(_testPath, projectName);

        // Act
        await InitCommand.ExecuteInit(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        var appsettingsContent = await File.ReadAllTextAsync(Path.Combine(projectPath, "appsettings.json"));
        appsettingsContent.Should().Contain("\"version\": \"2.0\"");
        appsettingsContent.Should().Contain("\"relay\": {");
        appsettingsContent.Should().Contain("\"enableCaching\": false");

        var cliConfigContent = await File.ReadAllTextAsync(Path.Combine(projectPath, ".relay-cli.json"));
        cliConfigContent.Should().Contain("\"defaultNamespace\": \"MyApp\"");
        cliConfigContent.Should().Contain("\"templatePreference\": \"standard\"");
        cliConfigContent.Should().Contain("\"includeTests\": true");
    }

    [Fact]
    public async Task ExecuteInit_StandardTemplate_CreatesSampleCodeInRoot()
    {
        // Arrange
        var projectName = "SampleCodeTest";
        var projectPath = Path.Combine(_testPath, projectName);
        var mainProjectPath = Path.Combine(projectPath, "src", projectName);

        // Act
        await InitCommand.ExecuteInit(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        File.Exists(Path.Combine(mainProjectPath, "GetUserQuery.cs")).Should().BeTrue();
        File.Exists(Path.Combine(mainProjectPath, "UserResponse.cs")).Should().BeTrue();
        File.Exists(Path.Combine(mainProjectPath, "GetUserHandler.cs")).Should().BeTrue();

        var queryContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, "GetUserQuery.cs"));
        queryContent.Should().Contain($"namespace {projectName}.Requests;");
        queryContent.Should().Contain("public record GetUserQuery(int UserId) : IRequest<UserResponse>;");

        var responseContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, "UserResponse.cs"));
        responseContent.Should().Contain($"namespace {projectName}.Requests;");
        responseContent.Should().Contain("public record UserResponse(int Id, string Name, string Email);");

        var handlerContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, "GetUserHandler.cs"));
        handlerContent.Should().Contain($"namespace {projectName}.Handlers;");
        handlerContent.Should().Contain("public class GetUserHandler : IRequestHandler<GetUserQuery, UserResponse>");
        handlerContent.Should().Contain("[Handle]");
        handlerContent.Should().Contain("ValueTask<UserResponse>");
    }

    [Fact]
    public async Task ExecuteInit_EnterpriseTemplate_CreatesSampleCodeInFolders()
    {
        // Arrange
        var projectName = "EnterpriseSampleTest";
        var projectPath = Path.Combine(_testPath, projectName);
        var mainProjectPath = Path.Combine(projectPath, "src", projectName);

        // Act
        await InitCommand.ExecuteInit(projectName, "enterprise", _testPath, "net8.0", true, false, false);

        // Assert
        File.Exists(Path.Combine(mainProjectPath, "Requests", "GetUserQuery.cs")).Should().BeTrue();
        File.Exists(Path.Combine(mainProjectPath, "Responses", "UserResponse.cs")).Should().BeTrue();
        File.Exists(Path.Combine(mainProjectPath, "Handlers", "GetUserHandler.cs")).Should().BeTrue();

        var queryContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, "Requests", "GetUserQuery.cs"));
        queryContent.Should().Contain($"namespace {projectName}.Requests;");

        var responseContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, "Responses", "UserResponse.cs"));
        responseContent.Should().Contain($"namespace {projectName}.Requests;");

        var handlerContent = await File.ReadAllTextAsync(Path.Combine(mainProjectPath, "Handlers", "GetUserHandler.cs"));
        handlerContent.Should().Contain($"namespace {projectName}.Handlers;");
    }

    [Fact]
    public async Task ExecuteInit_CreatesSampleTestFile()
    {
        // Arrange
        var projectName = "SampleTestTest";
        var projectPath = Path.Combine(_testPath, projectName);
        var testProjectPath = Path.Combine(projectPath, "tests", $"{projectName}.Tests");

        // Act
        await InitCommand.ExecuteInit(projectName, "standard", _testPath, "net8.0", true, false, false);

        // Assert
        var testContent = await File.ReadAllTextAsync(Path.Combine(testProjectPath, "SampleTests.cs"));
        testContent.Should().Contain($"namespace {projectName}.Tests;");
        testContent.Should().Contain("public class SampleTests");
        testContent.Should().Contain("[Fact]");
        testContent.Should().Contain("SampleTest_ShouldPass");
        testContent.Should().Contain("using Xunit;");
        testContent.Should().Contain("using FluentAssertions;");
    }

    [Fact]
    public async Task ExecuteInit_CustomOutputDirectory_CreatesProjectInCorrectLocation()
    {
        // Arrange
        var projectName = "CustomOutputTest";
        var customOutputPath = Path.Combine(_testPath, "custom");
        Directory.CreateDirectory(customOutputPath);

        // Act
        await InitCommand.ExecuteInit(projectName, "standard", customOutputPath, "net8.0", true, false, false);

        // Assert
        var expectedProjectPath = Path.Combine(customOutputPath, projectName);
        Directory.Exists(expectedProjectPath).Should().BeTrue();
        File.Exists(Path.Combine(expectedProjectPath, $"{projectName}.sln")).Should().BeTrue();
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
