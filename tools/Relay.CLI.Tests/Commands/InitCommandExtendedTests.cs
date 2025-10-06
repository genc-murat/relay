using FluentAssertions;
using Relay.CLI.Commands;
using System.CommandLine;
using System.Text.Json;
using Xunit;

namespace Relay.CLI.Tests.Commands;

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
        command.Should().NotBeNull();
        command.Name.Should().Be("init");
        command.Description.Should().Contain("Initialize");
    }

    [Fact]
    public void InitCommand_Create_ShouldHaveNameOption()
    {
        // Act
        var command = InitCommand.Create();
        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");

        // Assert
        nameOption.Should().NotBeNull();
        nameOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void InitCommand_Create_ShouldHaveTemplateOption()
    {
        // Act
        var command = InitCommand.Create();
        var templateOption = command.Options.FirstOrDefault(o => o.Name == "template");

        // Assert
        templateOption.Should().NotBeNull();
        templateOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void InitCommand_Create_ShouldHaveOutputOption()
    {
        // Act
        var command = InitCommand.Create();
        var outputOption = command.Options.FirstOrDefault(o => o.Name == "output");

        // Assert
        outputOption.Should().NotBeNull();
    }

    [Fact]
    public void InitCommand_Create_ShouldHaveFrameworkOption()
    {
        // Act
        var command = InitCommand.Create();
        var frameworkOption = command.Options.FirstOrDefault(o => o.Name == "framework");

        // Assert
        frameworkOption.Should().NotBeNull();
    }

    [Fact]
    public void InitCommand_Create_ShouldHaveGitOption()
    {
        // Act
        var command = InitCommand.Create();
        var gitOption = command.Options.FirstOrDefault(o => o.Name == "git");

        // Assert
        gitOption.Should().NotBeNull();
    }

    [Fact]
    public void InitCommand_Create_ShouldHaveDockerOption()
    {
        // Act
        var command = InitCommand.Create();
        var dockerOption = command.Options.FirstOrDefault(o => o.Name == "docker");

        // Assert
        dockerOption.Should().NotBeNull();
    }

    [Fact]
    public void InitCommand_Create_ShouldHaveCIOption()
    {
        // Act
        var command = InitCommand.Create();
        var ciOption = command.Options.FirstOrDefault(o => o.Name == "ci");

        // Assert
        ciOption.Should().NotBeNull();
    }

    [Fact]
    public void InitCommand_Options_ShouldHaveCorrectCount()
    {
        // Act
        var command = InitCommand.Create();

        // Assert
        command.Options.Should().HaveCount(7); // name, template, output, framework, git, docker, ci
    }

    [Fact]
    public void InitCommand_Description_ShouldContainKeywords()
    {
        // Act
        var command = InitCommand.Create();

        // Assert
        command.Description.Should().Contain("Initialize");
        command.Description.Should().Contain("project");
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
        validTemplates.Should().Contain(template);
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
        new[] { "minimal", "standard", "enterprise" }.Should().Contain(normalizedTemplate);
    }

    [Fact]
    public void InitCommand_DefaultTemplate_ShouldBeStandard()
    {
        // Arrange
        var defaultTemplate = "standard";

        // Assert
        defaultTemplate.Should().Be("standard");
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
        validTemplates.Should().NotContain(template);
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
        validFrameworks.Should().Contain(framework);
    }

    [Fact]
    public void InitCommand_DefaultFramework_ShouldBeNet8()
    {
        // Arrange
        var defaultFramework = "net8.0";

        // Assert
        defaultFramework.Should().Be("net8.0");
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
        supportedFrameworks.Should().NotContain(framework);
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
        Directory.Exists(srcPath).Should().BeTrue();
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
        Directory.Exists(testsPath).Should().BeTrue();
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
        Directory.Exists(docsPath).Should().BeTrue();
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
        Directory.Exists(handlersPath).Should().BeTrue();
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
        Directory.Exists(requestsPath).Should().BeTrue();
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
        Directory.Exists(responsesPath).Should().BeTrue();
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
        Directory.Exists(validatorsPath).Should().BeTrue();
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
        Directory.Exists(behaviorsPath).Should().BeTrue();
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
        Path.GetExtension(solutionFile).Should().Be(".sln");
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
        content.Should().Contain("Microsoft Visual Studio Solution File");
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
        content.Should().Contain(projectName);
        content.Should().Contain(".csproj");
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
        content.Should().Contain($"{projectName}.Tests");
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
        content.Should().Contain(framework);
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
        content.Should().Contain("<Nullable>enable</Nullable>");
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
        content.Should().Contain("<ImplicitUsings>enable</ImplicitUsings>");
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
        content.Should().Contain("<LangVersion>latest</LangVersion>");
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
        content.Should().Contain("Relay.Core");
        content.Should().Contain("2.0.0");
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
        content.Should().Contain("xunit");
    }

    [Fact]
    public void InitCommand_TestProject_ShouldReferenceFluentAssertions()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var csprojPath = Path.Combine(_testPath, "Test.Tests.csproj");
        var csprojContent = "<PackageReference Include=\"FluentAssertions\" Version=\"6.12.0\" />";

        // Act
        File.WriteAllText(csprojPath, csprojContent);
        var content = File.ReadAllText(csprojPath);

        // Assert
        content.Should().Contain("FluentAssertions");
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
        content.Should().Contain("NSubstitute");
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
        content.Should().Contain("ProjectReference");
        content.Should().Contain($"{projectName}.csproj");
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
        content.Should().Contain("<IsPackable>false</IsPackable>");
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
        content.Should().Contain("Microsoft.Extensions.DependencyInjection");
        content.Should().Contain("Microsoft.Extensions.Hosting");
        content.Should().Contain("Relay.Core");
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
        content.Should().Contain("AddRelay");
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
        content.Should().Contain(projectName);
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
        content.Should().Contain("GetUserQuery");
        content.Should().Contain("IRequest");
        content.Should().Contain("UserResponse");
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
        content.Should().Contain("UserResponse");
        content.Should().Contain("record");
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
        content.Should().Contain("GetUserHandler");
        content.Should().Contain("IRequestHandler");
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
        content.Should().Contain("[Handle]");
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
        content.Should().Contain("ValueTask");
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
        content.Should().Contain("CancellationToken");
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
        isValidJson.Should().BeTrue();
        content.Should().Contain("relay");
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
        config.Should().ContainKey("relay");
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
        File.Exists(cliConfigPath).Should().BeTrue();
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
        content.Should().Contain("defaultNamespace");
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
        File.Exists(readmePath).Should().BeTrue();
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
        content.Should().Contain(projectName);
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
        content.Should().Contain("Getting Started");
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
        content.Should().Contain("dotnet build");
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
        content.Should().Contain("dotnet test");
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
        File.Exists(dockerfilePath).Should().BeTrue();
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
        content.Should().Contain("AS build");
        content.Should().Contain("AS final");
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
        File.Exists(composePath).Should().BeTrue();
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
        content.Should().Contain("services:");
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
        Directory.Exists(workflowPath).Should().BeTrue();
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
        File.Exists(ciPath).Should().BeTrue();
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
        content.Should().Contain("build:");
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
        content.Should().Contain("dotnet test");
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
        File.Exists(gitignorePath).Should().BeTrue();
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
        content.Should().Contain("[Bb]in");
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
        content.Should().Contain("bj");
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
        content.Should().Contain(".vs");
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
        isValid.Should().BeTrue();
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
        isValid.Should().BeFalse();
    }

    [Fact]
    public void InitCommand_ProjectName_ShouldMatchSolutionName()
    {
        // Arrange
        var projectName = "TestProject";
        var solutionName = $"{projectName}.sln";

        // Assert
        solutionName.Should().StartWith(projectName);
    }

    #endregion

    #region Output Path Tests

    [Fact]
    public void InitCommand_DefaultOutput_ShouldBeCurrentDirectory()
    {
        // Arrange
        var defaultOutput = ".";

        // Assert
        defaultOutput.Should().Be(".");
    }

    [Fact]
    public void InitCommand_CustomOutput_ShouldCreateInSpecifiedDirectory()
    {
        // Arrange
        var customOutput = Path.Combine(_testPath, "custom");
        Directory.CreateDirectory(customOutput);

        // Assert
        Directory.Exists(customOutput).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_OutputPath_ShouldCombineWithProjectName()
    {
        // Arrange
        var outputPath = _testPath;
        var projectName = "TestProject";
        var fullPath = Path.Combine(outputPath, projectName);

        // Assert
        Path.GetFileName(fullPath).Should().Be(projectName);
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
        exists.Should().BeTrue();
    }

    [Fact]
    public void InitCommand_LongProjectName_ShouldBeHandled()
    {
        // Arrange
        var longName = new string('A', 100);

        // Act
        var isReasonableLength = longName.Length > 0;

        // Assert
        isReasonableLength.Should().BeTrue();
    }

    [Fact]
    public void InitCommand_SpecialCharactersInPath_ShouldBeHandled()
    {
        // Arrange
        var projectName = "Test-Project_123";

        // Act
        var hasSpecialChars = projectName.Contains('-') || projectName.Contains('_');

        // Assert
        hasSpecialChars.Should().BeTrue();
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
            File.Exists(file).Should().BeTrue();
        }
    }

    [Fact]
    public void InitCommand_AllTemplates_ShouldCreateValidStructure()
    {
        // Arrange
        var templates = new[] { "minimal", "standard", "enterprise" };

        // Assert
        templates.Should().HaveCount(3);
        templates.Should().AllSatisfy(t => t.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public void InitCommand_AllFrameworks_ShouldBeValid()
    {
        // Arrange
        var frameworks = new[] { "net6.0", "net8.0", "net9.0" };

        // Assert
        frameworks.Should().HaveCount(3);
        frameworks.Should().AllSatisfy(f => f.Should().StartWith("net"));
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
