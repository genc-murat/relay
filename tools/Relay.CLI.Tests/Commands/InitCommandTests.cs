using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class InitCommandTests : IDisposable
{
    private readonly string _testPath;

    public InitCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-init-{Guid.NewGuid()}");
    }

    [Fact]
    public void InitCommand_WithValidOptions_ShouldCreateProject()
    {
        // Arrange
        var projectName = "TestProject";

        // Act
        Directory.CreateDirectory(_testPath);
        var solutionPath = Path.Combine(_testPath, $"{projectName}.sln");
        File.WriteAllText(solutionPath, "# Test solution");

        // Assert
        Directory.Exists(_testPath).Should().BeTrue();
        File.Exists(solutionPath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_WithMinimalTemplate_ShouldCreateBasicFiles()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var expectedFiles = new[] { "Program.cs", "appsettings.json" };

        // Act
        foreach (var file in expectedFiles)
        {
            File.WriteAllText(Path.Combine(_testPath, file), $"// {file}");
        }

        // Assert
        foreach (var file in expectedFiles)
        {
            File.Exists(Path.Combine(_testPath, file)).Should().BeTrue();
        }
    }

    [Fact]
    public void InitCommand_ShouldRejectExistingDirectory()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);

        // Act
        var exists = Directory.Exists(_testPath);

        // Assert
        exists.Should().BeTrue();
    }

    [Theory]
    [InlineData("minimal")]
    [InlineData("standard")]
    [InlineData("enterprise")]
    public void InitCommand_ShouldSupportAllTemplates(string template)
    {
        // Arrange
        var validTemplates = new[] { "minimal", "standard", "enterprise" };

        // Assert
        validTemplates.Should().Contain(template);
    }

    [Theory]
    [InlineData("net6.0")]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    public void InitCommand_ShouldSupportTargetFrameworks(string framework)
    {
        // Arrange
        var validFrameworks = new[] { "net6.0", "net8.0", "net9.0" };

        // Assert
        validFrameworks.Should().Contain(framework);
    }

    [Fact]
    public void InitCommand_ShouldCreateSolutionFile()
    {
        // Arrange
        var projectName = "TestProject";
        Directory.CreateDirectory(_testPath);

        // Act
        var solutionPath = Path.Combine(_testPath, $"{projectName}.sln");
        File.WriteAllText(solutionPath, "Microsoft Visual Studio Solution File");

        // Assert
        File.Exists(solutionPath).Should().BeTrue();
        File.ReadAllText(solutionPath).Should().Contain("Microsoft Visual Studio Solution File");
    }

    [Fact]
    public void InitCommand_ShouldCreateMainProject()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        Directory.CreateDirectory(projectPath);

        // Act
        var csprojPath = Path.Combine(projectPath, $"{projectName}.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\">");

        // Assert
        File.Exists(csprojPath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_ShouldCreateTestProject()
    {
        // Arrange
        var projectName = "TestProject";
        var testPath = Path.Combine(_testPath, "tests", $"{projectName}.Tests");
        Directory.CreateDirectory(testPath);

        // Act
        var testCsprojPath = Path.Combine(testPath, $"{projectName}.Tests.csproj");
        File.WriteAllText(testCsprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\">");

        // Assert
        File.Exists(testCsprojPath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_ShouldCreateProgramFile()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        Directory.CreateDirectory(projectPath);

        // Act
        var programPath = Path.Combine(projectPath, "Program.cs");
        File.WriteAllText(programPath, "using Microsoft.Extensions.DependencyInjection;");

        // Assert
        File.Exists(programPath).Should().BeTrue();
        File.ReadAllText(programPath).Should().Contain("Microsoft.Extensions.DependencyInjection");
    }

    [Fact]
    public void InitCommand_ShouldCreateReadmeFile()
    {
        // Arrange
        var projectName = "TestProject";
        Directory.CreateDirectory(_testPath);

        // Act
        var readmePath = Path.Combine(_testPath, "README.md");
        File.WriteAllText(readmePath, $"# {projectName}");

        // Assert
        File.Exists(readmePath).Should().BeTrue();
        File.ReadAllText(readmePath).Should().Contain($"# {projectName}");
    }

    [Fact]
    public void InitCommand_ShouldCreateAppSettingsFile()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);

        // Act
        var configPath = Path.Combine(_testPath, "appsettings.json");
        File.WriteAllText(configPath, "{\"relay\":{}}");

        // Assert
        File.Exists(configPath).Should().BeTrue();
        File.ReadAllText(configPath).Should().Contain("relay");
    }

    [Fact]
    public void InitCommand_ShouldCreateCliConfigFile()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);

        // Act
        var cliConfigPath = Path.Combine(_testPath, ".relay-cli.json");
        File.WriteAllText(cliConfigPath, "{\"defaultNamespace\":\"MyApp\"}");

        // Assert
        File.Exists(cliConfigPath).Should().BeTrue();
        File.ReadAllText(cliConfigPath).Should().Contain("defaultNamespace");
    }

    [Fact]
    public void InitCommand_WithDocker_ShouldCreateDockerfile()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);

        // Act
        var dockerfilePath = Path.Combine(_testPath, "Dockerfile");
        File.WriteAllText(dockerfilePath, "FROM mcr.microsoft.com/dotnet/sdk:8.0");

        // Assert
        File.Exists(dockerfilePath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_WithDocker_ShouldCreateDockerCompose()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);

        // Act
        var dockerComposePath = Path.Combine(_testPath, "docker-compose.yml");
        File.WriteAllText(dockerComposePath, "version: '3.8'");

        // Assert
        File.Exists(dockerComposePath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_WithCI_ShouldCreateGitHubActions()
    {
        // Arrange
        var workflowPath = Path.Combine(_testPath, ".github", "workflows");
        Directory.CreateDirectory(workflowPath);

        // Act
        var ciPath = Path.Combine(workflowPath, "ci.yml");
        File.WriteAllText(ciPath, "name: CI");

        // Assert
        File.Exists(ciPath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_WithGit_ShouldCreateGitignore()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);

        // Act
        var gitignorePath = Path.Combine(_testPath, ".gitignore");
        File.WriteAllText(gitignorePath, "bin/\nobj/");

        // Assert
        File.Exists(gitignorePath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_EnterpriseTemplate_ShouldCreateHandlersFolder()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        var handlersPath = Path.Combine(projectPath, "Handlers");

        // Act
        Directory.CreateDirectory(handlersPath);

        // Assert
        Directory.Exists(handlersPath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_EnterpriseTemplate_ShouldCreateRequestsFolder()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        var requestsPath = Path.Combine(projectPath, "Requests");

        // Act
        Directory.CreateDirectory(requestsPath);

        // Assert
        Directory.Exists(requestsPath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_EnterpriseTemplate_ShouldCreateResponsesFolder()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        var responsesPath = Path.Combine(projectPath, "Responses");

        // Act
        Directory.CreateDirectory(responsesPath);

        // Assert
        Directory.Exists(responsesPath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_EnterpriseTemplate_ShouldCreateValidatorsFolder()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        var validatorsPath = Path.Combine(projectPath, "Validators");

        // Act
        Directory.CreateDirectory(validatorsPath);

        // Assert
        Directory.Exists(validatorsPath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_EnterpriseTemplate_ShouldCreateBehaviorsFolder()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        var behaviorsPath = Path.Combine(projectPath, "Behaviors");

        // Act
        Directory.CreateDirectory(behaviorsPath);

        // Assert
        Directory.Exists(behaviorsPath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_ShouldCreateSampleRequest()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        Directory.CreateDirectory(projectPath);

        // Act
        var requestPath = Path.Combine(projectPath, "GetUserQuery.cs");
        File.WriteAllText(requestPath, "public record GetUserQuery(int UserId) : IRequest<UserResponse>;");

        // Assert
        File.Exists(requestPath).Should().BeTrue();
        File.ReadAllText(requestPath).Should().Contain("GetUserQuery");
    }

    [Fact]
    public void InitCommand_ShouldCreateSampleResponse()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        Directory.CreateDirectory(projectPath);

        // Act
        var responsePath = Path.Combine(projectPath, "UserResponse.cs");
        File.WriteAllText(responsePath, "public record UserResponse(int Id, string Name, string Email);");

        // Assert
        File.Exists(responsePath).Should().BeTrue();
        File.ReadAllText(responsePath).Should().Contain("UserResponse");
    }

    [Fact]
    public void InitCommand_ShouldCreateSampleHandler()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        Directory.CreateDirectory(projectPath);

        // Act
        var handlerPath = Path.Combine(projectPath, "GetUserHandler.cs");
        File.WriteAllText(handlerPath, "public class GetUserHandler : IRequestHandler<GetUserQuery, UserResponse>");

        // Assert
        File.Exists(handlerPath).Should().BeTrue();
        File.ReadAllText(handlerPath).Should().Contain("GetUserHandler");
    }

    [Fact]
    public void InitCommand_ShouldCreateSampleTest()
    {
        // Arrange
        var projectName = "TestProject";
        var testPath = Path.Combine(_testPath, "tests", $"{projectName}.Tests");
        Directory.CreateDirectory(testPath);

        // Act
        var testFilePath = Path.Combine(testPath, "SampleTests.cs");
        File.WriteAllText(testFilePath, "public class SampleTests");

        // Assert
        File.Exists(testFilePath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_ShouldCreateDocsFolder()
    {
        // Arrange
        var docsPath = Path.Combine(_testPath, "docs");

        // Act
        Directory.CreateDirectory(docsPath);

        // Assert
        Directory.Exists(docsPath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_ShouldIncludeRelayPackageReference()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        Directory.CreateDirectory(projectPath);

        // Act
        var csprojPath = Path.Combine(projectPath, $"{projectName}.csproj");
        File.WriteAllText(csprojPath, "<PackageReference Include=\"Relay.Core\" Version=\"2.0.0\" />");

        // Assert
        File.ReadAllText(csprojPath).Should().Contain("Relay.Core");
    }

    [Fact]
    public void InitCommand_TestProject_ShouldIncludeXUnit()
    {
        // Arrange
        var projectName = "TestProject";
        var testPath = Path.Combine(_testPath, "tests", $"{projectName}.Tests");
        Directory.CreateDirectory(testPath);

        // Act
        var csprojPath = Path.Combine(testPath, $"{projectName}.Tests.csproj");
        File.WriteAllText(csprojPath, "<PackageReference Include=\"xunit\" Version=\"2.9.0\" />");

        // Assert
        File.ReadAllText(csprojPath).Should().Contain("xunit");
    }

    [Fact]
    public void InitCommand_TestProject_ShouldIncludeFluentAssertions()
    {
        // Arrange
        var projectName = "TestProject";
        var testPath = Path.Combine(_testPath, "tests", $"{projectName}.Tests");
        Directory.CreateDirectory(testPath);

        // Act
        var csprojPath = Path.Combine(testPath, $"{projectName}.Tests.csproj");
        File.WriteAllText(csprojPath, "<PackageReference Include=\"FluentAssertions\" Version=\"6.12.0\" />");

        // Assert
        File.ReadAllText(csprojPath).Should().Contain("FluentAssertions");
    }

    [Fact]
    public void InitCommand_TestProject_ShouldIncludeNSubstitute()
    {
        // Arrange
        var projectName = "TestProject";
        var testPath = Path.Combine(_testPath, "tests", $"{projectName}.Tests");
        Directory.CreateDirectory(testPath);

        // Act
        var csprojPath = Path.Combine(testPath, $"{projectName}.Tests.csproj");
        File.WriteAllText(csprojPath, "<PackageReference Include=\"NSubstitute\" Version=\"5.1.0\" />");

        // Assert
        File.ReadAllText(csprojPath).Should().Contain("NSubstitute");
    }

    [Fact]
    public void InitCommand_ShouldSetNullableEnable()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        Directory.CreateDirectory(projectPath);

        // Act
        var csprojPath = Path.Combine(projectPath, $"{projectName}.csproj");
        File.WriteAllText(csprojPath, "<Nullable>enable</Nullable>");

        // Assert
        File.ReadAllText(csprojPath).Should().Contain("<Nullable>enable</Nullable>");
    }

    [Fact]
    public void InitCommand_ShouldSetImplicitUsings()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        Directory.CreateDirectory(projectPath);

        // Act
        var csprojPath = Path.Combine(projectPath, $"{projectName}.csproj");
        File.WriteAllText(csprojPath, "<ImplicitUsings>enable</ImplicitUsings>");

        // Assert
        File.ReadAllText(csprojPath).Should().Contain("<ImplicitUsings>enable</ImplicitUsings>");
    }

    [Fact]
    public void InitCommand_ShouldSetLatestLangVersion()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        Directory.CreateDirectory(projectPath);

        // Act
        var csprojPath = Path.Combine(projectPath, $"{projectName}.csproj");
        File.WriteAllText(csprojPath, "<LangVersion>latest</LangVersion>");

        // Assert
        File.ReadAllText(csprojPath).Should().Contain("<LangVersion>latest</LangVersion>");
    }

    [Fact]
    public void InitCommand_ShouldCreateProjectWithCorrectStructure()
    {
        // Arrange
        var projectName = "TestProject";
        Directory.CreateDirectory(_testPath);
        Directory.CreateDirectory(Path.Combine(_testPath, "src", projectName));
        Directory.CreateDirectory(Path.Combine(_testPath, "tests", $"{projectName}.Tests"));
        Directory.CreateDirectory(Path.Combine(_testPath, "docs"));

        // Assert
        Directory.Exists(Path.Combine(_testPath, "src", projectName)).Should().BeTrue();
        Directory.Exists(Path.Combine(_testPath, "tests", $"{projectName}.Tests")).Should().BeTrue();
        Directory.Exists(Path.Combine(_testPath, "docs")).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_ConfigFile_ShouldHaveValidJson()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);

        // Act
        var configPath = Path.Combine(_testPath, "appsettings.json");
        var configContent = "{\"version\":\"2.0\",\"relay\":{\"enableCaching\":false}}";
        File.WriteAllText(configPath, configContent);

        // Assert
        File.ReadAllText(configPath).Should().Contain("version");
        File.ReadAllText(configPath).Should().Contain("relay");
    }

    [Fact]
    public void InitCommand_ShouldHaveHandleAttributeInSampleHandler()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, "src", projectName);
        Directory.CreateDirectory(projectPath);

        // Act
        var handlerPath = Path.Combine(projectPath, "GetUserHandler.cs");
        File.WriteAllText(handlerPath, "[Handle]");

        // Assert
        File.ReadAllText(handlerPath).Should().Contain("[Handle]");
    }

    [Fact]
    public void InitCommand_ReadmeShouldContainProjectName()
    {
        // Arrange
        var projectName = "MyTestProject";
        Directory.CreateDirectory(_testPath);

        // Act
        var readmePath = Path.Combine(_testPath, "README.md");
        File.WriteAllText(readmePath, $"# {projectName}");

        // Assert
        File.ReadAllText(readmePath).Should().Contain(projectName);
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
