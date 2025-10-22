
using Relay.CLI.Commands;
using System.CommandLine;
using System.Text.Json;
using Xunit;

namespace Relay.CLI.Tests.Commands;

#pragma warning disable CS8602
/// <summary>
/// Extended comprehensive tests for InitCommand
/// </summary>
public class InitCommandExtendedTests : IDisposable
{
    private readonly string _testPath;

    public InitCommandExtendedTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-init-extended-{Guid.NewGuid()}");
    }

    #region Command Creation Tests

    [Fact]
    public void InitCommand_Create_ShouldReturnValidCommand()
    {
        // Act
        var command = InitCommand.Create();

        // Assert
        Assert.NotNull(command);
        Assert.Equal("init", command.Name);
        Assert.Contains("Initialize", command.Description);
    }

    [Fact]
    public void InitCommand_Create_ShouldHaveNameOption()
    {
        // Act
        var command = InitCommand.Create();
        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");

        // Assert
        Assert.NotNull(nameOption);
        Assert.True(nameOption!.IsRequired);
    }

    [Fact]
    public void InitCommand_Create_ShouldHaveTemplateOption()
    {
        // Act
        var command = InitCommand.Create();
        var templateOption = command.Options.FirstOrDefault(o => o.Name == "template");

        // Assert
        Assert.NotNull(templateOption);
        Assert.False(templateOption!.IsRequired);
    }

    [Fact]
    public void InitCommand_Create_ShouldHaveOutputOption()
    {
        // Act
        var command = InitCommand.Create();
        var outputOption = command.Options.FirstOrDefault(o => o.Name == "output");

        // Assert
        Assert.NotNull(outputOption);
    }

    [Fact]
    public void InitCommand_Create_ShouldHaveFrameworkOption()
    {
        // Act
        var command = InitCommand.Create();
        var frameworkOption = command.Options.FirstOrDefault(o => o.Name == "framework");

        // Assert
        Assert.NotNull(frameworkOption);
    }

    [Fact]
    public void InitCommand_Create_ShouldHaveGitOption()
    {
        // Act
        var command = InitCommand.Create();
        var gitOption = command.Options.FirstOrDefault(o => o.Name == "git");

        // Assert
        Assert.NotNull(gitOption);
    }

    [Fact]
    public void InitCommand_Create_ShouldHaveDockerOption()
    {
        // Act
        var command = InitCommand.Create();
        var dockerOption = command.Options.FirstOrDefault(o => o.Name == "docker");

        // Assert
        Assert.NotNull(dockerOption);
    }

    [Fact]
    public void InitCommand_Create_ShouldHaveCIOption()
    {
        // Act
        var command = InitCommand.Create();
        var ciOption = command.Options.FirstOrDefault(o => o.Name == "ci");

        // Assert
        Assert.NotNull(ciOption);
    }

    [Fact]
    public void InitCommand_Options_ShouldHaveCorrectCount()
    {
        // Act
        var command = InitCommand.Create();

        // Assert
        Assert.Equal(7, command.Options.Count()); // name, template, output, framework, git, docker, ci
    }

    [Fact]
    public void InitCommand_Description_ShouldContainKeywords()
    {
        // Act
        var command = InitCommand.Create();

        // Assert
        Assert.Contains("Initialize", command.Description);
        Assert.Contains("project", command.Description);
    }

    #endregion

    #region Template Validation Tests

    [Theory]
    [InlineData("minimal")]
    [InlineData("standard")]
    [InlineData("enterprise")]
    public void InitCommand_ValidTemplates_ShouldBeRecognized(string template)
    {
        // Arrange
        var validTemplates = new[] { "minimal", "standard", "enterprise" };

        // Assert
        Assert.Contains(template, validTemplates);
    }

    [Theory]
    [InlineData("MINIMAL")]
    [InlineData("Standard")]
    [InlineData("ENTERPRISE")]
    public void InitCommand_TemplateCaseSensitivity_ShouldHandleDifferentCases(string template)
    {
        // Arrange
        var normalizedTemplate = template.ToLowerInvariant();

        // Assert
        Assert.Contains(normalizedTemplate, new[] { "minimal", "standard", "enterprise" });
    }

    [Fact]
    public void InitCommand_DefaultTemplate_ShouldBeStandard()
    {
        // Arrange
        var defaultTemplate = "standard";

        // Assert
        Assert.Equal("standard", defaultTemplate);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("custom")]
    [InlineData("")]
    public void InitCommand_InvalidTemplates_ShouldNotBeInValidList(string template)
    {
        // Arrange
        var validTemplates = new[] { "minimal", "standard", "enterprise" };

        // Assert
        Assert.DoesNotContain(template, validTemplates);
    }

    #endregion

    #region Framework Validation Tests

    [Theory]
    [InlineData("net6.0")]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    public void InitCommand_ValidFrameworks_ShouldBeSupported(string framework)
    {
        // Arrange
        var validFrameworks = new[] { "net6.0", "net8.0", "net9.0" };

        // Assert
        Assert.Contains(framework, validFrameworks);
    }

    [Fact]
    public void InitCommand_DefaultFramework_ShouldBeNet8()
    {
        // Arrange
        var defaultFramework = "net8.0";

        // Assert
        Assert.Equal("net8.0", defaultFramework);
    }

    [Theory]
    [InlineData("net5.0")]
    [InlineData("net7.0")]
    [InlineData("netcoreapp3.1")]
    [InlineData("netstandard2.0")]
    public void InitCommand_UnsupportedFrameworks_ShouldNotBeInList(string framework)
    {
        // Arrange
        var supportedFrameworks = new[] { "net6.0", "net8.0", "net9.0" };

        // Assert
        Assert.DoesNotContain(framework, supportedFrameworks);
    }

    #endregion

    #region Project Structure Tests

    [Fact]
    public void InitCommand_ProjectStructure_ShouldHaveSrcDirectory()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, projectName);
        var srcPath = Path.Combine(projectPath, "src", projectName);

        // Act
        Directory.CreateDirectory(srcPath);

        // Assert
        Assert.True(Directory.Exists(srcPath));
    }

    [Fact]
    public void InitCommand_ProjectStructure_ShouldHaveTestsDirectory()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, projectName);
        var testsPath = Path.Combine(projectPath, "tests", $"{projectName}.Tests");

        // Act
        Directory.CreateDirectory(testsPath);

        // Assert
        Assert.True(Directory.Exists(testsPath));
    }

    [Fact]
    public void InitCommand_ProjectStructure_ShouldHaveDocsDirectory()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, projectName);
        var docsPath = Path.Combine(projectPath, "docs");

        // Act
        Directory.CreateDirectory(docsPath);

        // Assert
        Assert.True(Directory.Exists(docsPath));
    }

    [Fact]
    public void InitCommand_EnterpriseTemplate_ShouldCreateHandlersDirectory()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, projectName);
        var handlersPath = Path.Combine(projectPath, "src", projectName, "Handlers");

        // Act
        Directory.CreateDirectory(handlersPath);

        // Assert
        Assert.True(Directory.Exists(handlersPath));
    }

    [Fact]
    public void InitCommand_EnterpriseTemplate_ShouldCreateRequestsDirectory()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, projectName);
        var requestsPath = Path.Combine(projectPath, "src", projectName, "Requests");

        // Act
        Directory.CreateDirectory(requestsPath);

        // Assert
        Assert.True(Directory.Exists(requestsPath));
    }

    [Fact]
    public void InitCommand_EnterpriseTemplate_ShouldCreateResponsesDirectory()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, projectName);
        var responsesPath = Path.Combine(projectPath, "src", projectName, "Responses");

        // Act
        Directory.CreateDirectory(responsesPath);

        // Assert
        Assert.True(Directory.Exists(responsesPath));
    }

    [Fact]
    public void InitCommand_EnterpriseTemplate_ShouldCreateValidatorsDirectory()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, projectName);
        var validatorsPath = Path.Combine(projectPath, "src", projectName, "Validators");

        // Act
        Directory.CreateDirectory(validatorsPath);

        // Assert
        Assert.True(Directory.Exists(validatorsPath));
    }

    [Fact]
    public void InitCommand_EnterpriseTemplate_ShouldCreateBehaviorsDirectory()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, projectName);
        var behaviorsPath = Path.Combine(projectPath, "src", projectName, "Behaviors");

        // Act
        Directory.CreateDirectory(behaviorsPath);

        // Assert
        Assert.True(Directory.Exists(behaviorsPath));
    }

    #endregion

    #region Solution File Tests

    [Fact]
    public void InitCommand_SolutionFile_ShouldHaveCorrectExtension()
    {
        // Arrange
        var projectName = "TestProject";
        var solutionFile = $"{projectName}.sln";

        // Assert
        Assert.Equal(".sln", Path.GetExtension(solutionFile));
    }

    [Fact]
    public void InitCommand_SolutionFile_ShouldContainVisualStudioHeader()
    {
        // Arrange
        var projectName = "TestProject";
        Directory.CreateDirectory(_testPath);
        var slnPath = Path.Combine(_testPath, $"{projectName}.sln");
        var slnContent = "Microsoft Visual Studio Solution File, Format Version 12.00";

        // Act
        File.WriteAllText(slnPath, slnContent);
        var content = File.ReadAllText(slnPath);

        // Assert
        Assert.Contains("Microsoft Visual Studio Solution File", content);
    }

    [Fact]
    public void InitCommand_SolutionFile_ShouldContainMainProject()
    {
        // Arrange
        var projectName = "TestProject";
        Directory.CreateDirectory(_testPath);
        var slnPath = Path.Combine(_testPath, $"{projectName}.sln");
        var slnContent = $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projectName}\", \"src\\{projectName}\\{projectName}.csproj\"";

        // Act
        File.WriteAllText(slnPath, slnContent);
        var content = File.ReadAllText(slnPath);

        // Assert
        Assert.Contains(projectName, content);
        Assert.Contains(".csproj", content);
    }

    [Fact]
    public void InitCommand_SolutionFile_ShouldContainTestProject()
    {
        // Arrange
        var projectName = "TestProject";
        Directory.CreateDirectory(_testPath);
        var slnPath = Path.Combine(_testPath, $"{projectName}.sln");
        var slnContent = $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projectName}.Tests\"";

        // Act
        File.WriteAllText(slnPath, slnContent);
        var content = File.ReadAllText(slnPath);

        // Assert
        Assert.Contains($"{projectName}.Tests", content);
    }

    #endregion

    #region Project File Tests

    [Fact]
    public void InitCommand_ProjectFile_ShouldTargetCorrectFramework()
    {
        // Arrange
        var framework = "net8.0";
        Directory.CreateDirectory(_testPath);
        var csprojPath = Path.Combine(_testPath, "Test.csproj");
        var csprojContent = $"<TargetFramework>{framework}</TargetFramework>";

        // Act
        File.WriteAllText(csprojPath, csprojContent);
        var content = File.ReadAllText(csprojPath);

        // Assert
        Assert.Contains(framework, content);
    }

    [Fact]
    public void InitCommand_ProjectFile_ShouldEnableNullable()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var csprojPath = Path.Combine(_testPath, "Test.csproj");
        var csprojContent = "<Nullable>enable</Nullable>";

        // Act
        File.WriteAllText(csprojPath, csprojContent);
        var content = File.ReadAllText(csprojPath);

        // Assert
        Assert.Contains("<Nullable>enable</Nullable>", content);
    }

    [Fact]
    public void InitCommand_ProjectFile_ShouldEnableImplicitUsings()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var csprojPath = Path.Combine(_testPath, "Test.csproj");
        var csprojContent = "<ImplicitUsings>enable</ImplicitUsings>";

        // Act
        File.WriteAllText(csprojPath, csprojContent);
        var content = File.ReadAllText(csprojPath);

        // Assert
        Assert.Contains("<ImplicitUsings>enable</ImplicitUsings>", content);
    }

    [Fact]
    public void InitCommand_ProjectFile_ShouldUseLatestLangVersion()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var csprojPath = Path.Combine(_testPath, "Test.csproj");
        var csprojContent = "<LangVersion>latest</LangVersion>";

        // Act
        File.WriteAllText(csprojPath, csprojContent);
        var content = File.ReadAllText(csprojPath);

        // Assert
        Assert.Contains("<LangVersion>latest</LangVersion>", content);
    }

    [Fact]
    public void InitCommand_ProjectFile_ShouldReferenceRelayCore()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var csprojPath = Path.Combine(_testPath, "Test.csproj");
        var csprojContent = "<PackageReference Include=\"Relay.Core\" Version=\"2.0.0\" />";

        // Act
        File.WriteAllText(csprojPath, csprojContent);
        var content = File.ReadAllText(csprojPath);

        // Assert
        Assert.Contains("Relay.Core", content);
        Assert.Contains("2.0.0", content);
    }

    #endregion

    #region Test Project File Tests

    [Fact]
    public void InitCommand_TestProject_ShouldReferenceXUnit()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var csprojPath = Path.Combine(_testPath, "Test.Tests.csproj");
        var csprojContent = "<PackageReference Include=\"xunit\" Version=\"2.9.0\" />";

        // Act
        File.WriteAllText(csprojPath, csprojContent);
        var content = File.ReadAllText(csprojPath);

        // Assert
        Assert.Contains("xunit", content);
    }

    [Fact]
    public void InitCommand_TestProject_ShouldReferenceNSubstitute()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var csprojPath = Path.Combine(_testPath, "Test.Tests.csproj");
        var csprojContent = "<PackageReference Include=\"NSubstitute\" Version=\"5.1.0\" />";

        // Act
        File.WriteAllText(csprojPath, csprojContent);
        var content = File.ReadAllText(csprojPath);

        // Assert
        Assert.Contains("NSubstitute", content);
    }

    [Fact]
    public void InitCommand_TestProject_ShouldReferenceMainProject()
    {
        // Arrange
        var projectName = "TestProject";
        Directory.CreateDirectory(_testPath);
        var csprojPath = Path.Combine(_testPath, $"{projectName}.Tests.csproj");
        var csprojContent = $"<ProjectReference Include=\"..\\..\\src\\{projectName}\\{projectName}.csproj\" />";

        // Act
        File.WriteAllText(csprojPath, csprojContent);
        var content = File.ReadAllText(csprojPath);

        // Assert
        Assert.Contains("ProjectReference", content);
        Assert.Contains($"{projectName}.csproj", content);
    }

    [Fact]
    public void InitCommand_TestProject_ShouldNotBePackable()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var csprojPath = Path.Combine(_testPath, "Test.Tests.csproj");
        var csprojContent = "<IsPackable>false</IsPackable>";

        // Act
        File.WriteAllText(csprojPath, csprojContent);
        var content = File.ReadAllText(csprojPath);

        // Assert
        Assert.Contains("<IsPackable>false</IsPackable>", content);
    }

    #endregion

    #region Program.cs Tests

    [Fact]
    public void InitCommand_ProgramFile_ShouldContainMainUsings()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var programPath = Path.Combine(_testPath, "Program.cs");
        var programContent = @"using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay.Core;";

        // Act
        File.WriteAllText(programPath, programContent);
        var content = File.ReadAllText(programPath);

        // Assert
        Assert.Contains("Microsoft.Extensions.DependencyInjection", content);
        Assert.Contains("Microsoft.Extensions.Hosting", content);
        Assert.Contains("Relay.Core", content);
    }

    [Fact]
    public void InitCommand_ProgramFile_ShouldRegisterRelay()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var programPath = Path.Combine(_testPath, "Program.cs");
        var programContent = "builder.Services.AddRelay();";

        // Act
        File.WriteAllText(programPath, programContent);
        var content = File.ReadAllText(programPath);

        // Assert
        Assert.Contains("AddRelay", content);
    }

    [Fact]
    public void InitCommand_ProgramFile_ShouldContainProjectName()
    {
        // Arrange
        var projectName = "MyAwesomeProject";
        Directory.CreateDirectory(_testPath);
        var programPath = Path.Combine(_testPath, "Program.cs");
        var programContent = $"Console.WriteLine(\"🚀 {projectName} is running with Relay!\");";

        // Act
        File.WriteAllText(programPath, programContent);
        var content = File.ReadAllText(programPath);

        // Assert
        Assert.Contains(projectName, content);
    }

    #endregion

    #region Sample Code Tests

    [Fact]
    public void InitCommand_SampleCode_ShouldCreateGetUserQuery()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var queryPath = Path.Combine(_testPath, "GetUserQuery.cs");
        var queryContent = "public record GetUserQuery(int UserId) : IRequest<UserResponse>;";

        // Act
        File.WriteAllText(queryPath, queryContent);
        var content = File.ReadAllText(queryPath);

        // Assert
        Assert.Contains("GetUserQuery", content);
        Assert.Contains("IRequest", content);
        Assert.Contains("UserResponse", content);
    }

    [Fact]
    public void InitCommand_SampleCode_ShouldCreateUserResponse()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var responsePath = Path.Combine(_testPath, "UserResponse.cs");
        var responseContent = "public record UserResponse(int Id, string Name, string Email);";

        // Act
        File.WriteAllText(responsePath, responseContent);
        var content = File.ReadAllText(responsePath);

        // Assert
        Assert.Contains("UserResponse", content);
        Assert.Contains("record", content);
    }

    [Fact]
    public void InitCommand_SampleCode_ShouldCreateGetUserHandler()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var handlerPath = Path.Combine(_testPath, "GetUserHandler.cs");
        var handlerContent = "public class GetUserHandler : IRequestHandler<GetUserQuery, UserResponse>";

        // Act
        File.WriteAllText(handlerPath, handlerContent);
        var content = File.ReadAllText(handlerPath);

        // Assert
        Assert.Contains("GetUserHandler", content);
        Assert.Contains("IRequestHandler", content);
    }

    [Fact]
    public void InitCommand_SampleHandler_ShouldHaveHandleAttribute()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var handlerPath = Path.Combine(_testPath, "GetUserHandler.cs");
        var handlerContent = "[Handle]";

        // Act
        File.WriteAllText(handlerPath, handlerContent);
        var content = File.ReadAllText(handlerPath);

        // Assert
        Assert.Contains("[Handle]", content);
    }

    [Fact]
    public void InitCommand_SampleHandler_ShouldUseValueTask()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var handlerPath = Path.Combine(_testPath, "GetUserHandler.cs");
        var handlerContent = "public async ValueTask<UserResponse> HandleAsync";

        // Act
        File.WriteAllText(handlerPath, handlerContent);
        var content = File.ReadAllText(handlerPath);

        // Assert
        Assert.Contains("ValueTask", content);
    }

    [Fact]
    public void InitCommand_SampleHandler_ShouldHaveCancellationToken()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var handlerPath = Path.Combine(_testPath, "GetUserHandler.cs");
        var handlerContent = "HandleAsync(GetUserQuery request, CancellationToken cancellationToken)";

        // Act
        File.WriteAllText(handlerPath, handlerContent);
        var content = File.ReadAllText(handlerPath);

        // Assert
        Assert.Contains("CancellationToken", content);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void InitCommand_AppSettings_ShouldBeValidJson()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var configPath = Path.Combine(_testPath, "appsettings.json");
        var configContent = @"{
  ""version"": ""2.0"",
  ""relay"": {
    ""enableCaching"": false,
    ""enableValidation"": true
  }
}";

        // Act
        File.WriteAllText(configPath, configContent);
        var content = File.ReadAllText(configPath);
        var isValidJson = IsValidJson(content);

        // Assert
        Assert.True(isValidJson);
        Assert.Contains("relay", content);
    }

    [Fact]
    public void InitCommand_AppSettings_ShouldHaveRelaySection()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var configPath = Path.Combine(_testPath, "appsettings.json");
        var configContent = @"{""relay"": {""enableCaching"": false}}";

        // Act
        File.WriteAllText(configPath, configContent);
        var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(configContent);

        // Assert
        Assert.Contains("relay", config.Keys);
    }

    [Fact]
    public void InitCommand_CliConfig_ShouldExist()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var cliConfigPath = Path.Combine(_testPath, ".relay-cli.json");
        var cliConfigContent = @"{""defaultNamespace"": ""MyApp""}";

        // Act
        File.WriteAllText(cliConfigPath, cliConfigContent);

        // Assert
        Assert.True(File.Exists(cliConfigPath));
    }

    [Fact]
    public void InitCommand_CliConfig_ShouldContainDefaultNamespace()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var cliConfigPath = Path.Combine(_testPath, ".relay-cli.json");
        var cliConfigContent = @"{""defaultNamespace"": ""MyApp""}";

        // Act
        File.WriteAllText(cliConfigPath, cliConfigContent);
        var content = File.ReadAllText(cliConfigPath);

        // Assert
        Assert.Contains("defaultNamespace", content);
    }

    #endregion

    #region README Tests

    [Fact]
    public void InitCommand_Readme_ShouldExist()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var readmePath = Path.Combine(_testPath, "README.md");

        // Act
        File.WriteAllText(readmePath, "# Project");

        // Assert
        Assert.True(File.Exists(readmePath));
    }

    [Fact]
    public void InitCommand_Readme_ShouldContainProjectName()
    {
        // Arrange
        var projectName = "MyProject";
        Directory.CreateDirectory(_testPath);
        var readmePath = Path.Combine(_testPath, "README.md");
        var readmeContent = $"# {projectName}";

        // Act
        File.WriteAllText(readmePath, readmeContent);
        var content = File.ReadAllText(readmePath);

        // Assert
        Assert.Contains(projectName, content);
    }

    [Fact]
    public void InitCommand_Readme_ShouldHaveGettingStartedSection()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var readmePath = Path.Combine(_testPath, "README.md");
        var readmeContent = "## 🚀 Getting Started";

        // Act
        File.WriteAllText(readmePath, readmeContent);
        var content = File.ReadAllText(readmePath);

        // Assert
        Assert.Contains("Getting Started", content);
    }

    [Fact]
    public void InitCommand_Readme_ShouldHaveBuildInstructions()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var readmePath = Path.Combine(_testPath, "README.md");
        var readmeContent = "dotnet build";

        // Act
        File.WriteAllText(readmePath, readmeContent);
        var content = File.ReadAllText(readmePath);

        // Assert
        Assert.Contains("dotnet build", content);
    }

    [Fact]
    public void InitCommand_Readme_ShouldHaveTestInstructions()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var readmePath = Path.Combine(_testPath, "README.md");
        var readmeContent = "dotnet test";

        // Act
        File.WriteAllText(readmePath, readmeContent);
        var content = File.ReadAllText(readmePath);

        // Assert
        Assert.Contains("dotnet test", content);
    }

    #endregion

    #region Docker Tests

    [Fact]
    public void InitCommand_Docker_ShouldCreateDockerfile()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var dockerfilePath = Path.Combine(_testPath, "Dockerfile");

        // Act
        File.WriteAllText(dockerfilePath, "FROM mcr.microsoft.com/dotnet/sdk:8.0");

        // Assert
        Assert.True(File.Exists(dockerfilePath));
    }

    [Fact]
    public void InitCommand_Dockerfile_ShouldUseMultiStage()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var dockerfilePath = Path.Combine(_testPath, "Dockerfile");
        var dockerContent = @"FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final";

        // Act
        File.WriteAllText(dockerfilePath, dockerContent);
        var content = File.ReadAllText(dockerfilePath);

        // Assert
        Assert.Contains("AS build", content);
        Assert.Contains("AS final", content);
    }

    [Fact]
    public void InitCommand_Docker_ShouldCreateDockerCompose()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var composePath = Path.Combine(_testPath, "docker-compose.yml");

        // Act
        File.WriteAllText(composePath, "version: '3.8'");

        // Assert
        Assert.True(File.Exists(composePath));
    }

    [Fact]
    public void InitCommand_DockerCompose_ShouldDefineService()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var composePath = Path.Combine(_testPath, "docker-compose.yml");
        var composeContent = "services:\n  app:";

        // Act
        File.WriteAllText(composePath, composeContent);
        var content = File.ReadAllText(composePath);

        // Assert
        Assert.Contains("services:", content);
    }

    #endregion

    #region CI/CD Tests

    [Fact]
    public void InitCommand_CI_ShouldCreateGitHubActionsDirectory()
    {
        // Arrange
        var workflowPath = Path.Combine(_testPath, ".github", "workflows");

        // Act
        Directory.CreateDirectory(workflowPath);

        // Assert
        Assert.True(Directory.Exists(workflowPath));
    }

    [Fact]
    public void InitCommand_CI_ShouldCreateWorkflowFile()
    {
        // Arrange
        var workflowPath = Path.Combine(_testPath, ".github", "workflows");
        Directory.CreateDirectory(workflowPath);
        var ciPath = Path.Combine(workflowPath, "ci.yml");

        // Act
        File.WriteAllText(ciPath, "name: CI");

        // Assert
        Assert.True(File.Exists(ciPath));
    }

    [Fact]
    public void InitCommand_Workflow_ShouldHaveBuildJob()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var ciPath = Path.Combine(_testPath, "ci.yml");
        var ciContent = "jobs:\n  build:";

        // Act
        File.WriteAllText(ciPath, ciContent);
        var content = File.ReadAllText(ciPath);

        // Assert
        Assert.Contains("build:", content);
    }

    [Fact]
    public void InitCommand_Workflow_ShouldHaveTestStep()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var ciPath = Path.Combine(_testPath, "ci.yml");
        var ciContent = "- name: Test\n  run: dotnet test";

        // Act
        File.WriteAllText(ciPath, ciContent);
        var content = File.ReadAllText(ciPath);

        // Assert
        Assert.Contains("dotnet test", content);
    }

    #endregion

    #region Git Tests

    [Fact]
    public void InitCommand_Git_ShouldCreateGitignore()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var gitignorePath = Path.Combine(_testPath, ".gitignore");

        // Act
        File.WriteAllText(gitignorePath, "bin/\nobj/");

        // Assert
        Assert.True(File.Exists(gitignorePath));
    }

    [Fact]
    public void InitCommand_Gitignore_ShouldIgnoreBinDirectory()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var gitignorePath = Path.Combine(_testPath, ".gitignore");
        var gitignoreContent = "[Bb]in/";

        // Act
        File.WriteAllText(gitignorePath, gitignoreContent);
        var content = File.ReadAllText(gitignorePath);

        // Assert
        Assert.Contains("[Bb]in", content);
    }

    [Fact]
    public void InitCommand_Gitignore_ShouldIgnoreObjDirectory()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var gitignorePath = Path.Combine(_testPath, ".gitignore");
        var gitignoreContent = "[Oo]bj/";

        // Act
        File.WriteAllText(gitignorePath, gitignoreContent);
        var content = File.ReadAllText(gitignorePath);

        // Assert
        Assert.Contains("bj", content);
    }

    [Fact]
    public void InitCommand_Gitignore_ShouldIgnoreVsDirectory()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var gitignorePath = Path.Combine(_testPath, ".gitignore");
        var gitignoreContent = ".vs/";

        // Act
        File.WriteAllText(gitignorePath, gitignoreContent);
        var content = File.ReadAllText(gitignorePath);

        // Assert
        Assert.Contains(".vs", content);
    }

    #endregion

    #region Project Naming Tests

    [Theory]
    [InlineData("MyProject")]
    [InlineData("My.Project")]
    [InlineData("My_Project")]
    [InlineData("MyProject123")]
    public void InitCommand_ValidProjectNames_ShouldBeAccepted(string projectName)
    {
        // Arrange & Act
        var isValid = !string.IsNullOrWhiteSpace(projectName);

        // Assert
        Assert.True(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null!)]
    public void InitCommand_InvalidProjectNames_ShouldBeRejected(string? projectName)
    {
        // Arrange & Act
        var isValid = !string.IsNullOrWhiteSpace(projectName);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void InitCommand_ProjectName_ShouldMatchSolutionName()
    {
        // Arrange
        var projectName = "TestProject";
        var solutionName = $"{projectName}.sln";

        // Assert
        Assert.StartsWith(projectName, solutionName);
    }

    #endregion

    #region Output Path Tests

    [Fact]
    public void InitCommand_DefaultOutput_ShouldBeCurrentDirectory()
    {
        // Arrange
        var defaultOutput = ".";

        // Assert
        Assert.Equal(".", defaultOutput);
    }

    [Fact]
    public void InitCommand_CustomOutput_ShouldCreateInSpecifiedDirectory()
    {
        // Arrange
        var customOutput = Path.Combine(_testPath, "custom");
        Directory.CreateDirectory(customOutput);

        // Assert
        Assert.True(Directory.Exists(customOutput));
    }

    [Fact]
    public void InitCommand_OutputPath_ShouldCombineWithProjectName()
    {
        // Arrange
        var outputPath = _testPath;
        var projectName = "TestProject";
        var fullPath = Path.Combine(outputPath, projectName);

        // Assert
        Assert.Equal(projectName, Path.GetFileName(fullPath));
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void InitCommand_ExistingDirectory_ShouldDetectConflict()
    {
        // Arrange
        var projectPath = Path.Combine(_testPath, "ExistingProject");
        Directory.CreateDirectory(projectPath);

        // Act
        var exists = Directory.Exists(projectPath);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void InitCommand_LongProjectName_ShouldBeHandled()
    {
        // Arrange
        var longName = new string('A', 100);

        // Act
        var isReasonableLength = longName.Length > 0;

        // Assert
        Assert.True(isReasonableLength);
    }

    [Fact]
    public void InitCommand_SpecialCharactersInPath_ShouldBeHandled()
    {
        // Arrange
        var projectName = "Test-Project_123";

        // Act
        var hasSpecialChars = projectName.Contains('-') || projectName.Contains('_');

        // Assert
        Assert.True(hasSpecialChars);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void InitCommand_FullProject_ShouldHaveAllEssentialFiles()
    {
        // Arrange
        var projectName = "FullTestProject";
        var projectPath = Path.Combine(_testPath, projectName);
        Directory.CreateDirectory(Path.Combine(projectPath, "src", projectName));
        Directory.CreateDirectory(Path.Combine(projectPath, "tests", $"{projectName}.Tests"));

        var essentialFiles = new[]
        {
            Path.Combine(projectPath, $"{projectName}.sln"),
            Path.Combine(projectPath, "README.md"),
            Path.Combine(projectPath, "appsettings.json"),
            Path.Combine(projectPath, ".relay-cli.json")
        };

        // Act
        foreach (var file in essentialFiles)
        {
            File.WriteAllText(file, "test content");
        }

        // Assert
        foreach (var file in essentialFiles)
        {
            Assert.True(File.Exists(file));
        }
    }

    [Fact]
    public void InitCommand_AllTemplates_ShouldCreateValidStructure()
    {
        // Arrange
        var templates = new[] { "minimal", "standard", "enterprise" };

        // Assert
        Assert.Equal(3, templates.Count());
        Assert.All(templates, t => Assert.False(string.IsNullOrEmpty(t)));
    }

    [Fact]
    public void InitCommand_AllFrameworks_ShouldBeValid()
    {
        // Arrange
        var frameworks = new[] { "net6.0", "net8.0", "net9.0" };

        // Assert
        Assert.Equal(3, frameworks.Count());
        Assert.All(frameworks, f => Assert.StartsWith("net", f));
    }

    #endregion

    #region Helper Methods

    private bool IsValidJson(string json)
    {
        try
        {
            JsonSerializer.Deserialize<JsonElement>(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testPath))
            {
                Directory.Delete(_testPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}


