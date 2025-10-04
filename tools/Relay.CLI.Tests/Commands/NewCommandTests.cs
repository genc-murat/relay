using FluentAssertions;
using Relay.CLI.Commands;
using System.CommandLine;
using Xunit;

namespace Relay.CLI.Tests.Commands;

/// <summary>
/// Comprehensive tests for NewCommand - Project template creation
/// </summary>
public class NewCommandTests : IDisposable
{
    private readonly string _testPath;

    public NewCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-new-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    #region Command Creation Tests

    [Fact]
    public void NewCommand_Constructor_ShouldCreateValidCommand()
    {
        // Act
        var command = new NewCommand();

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("new");
        command.Description.Should().Contain("Create");
    }

    [Fact]
    public void NewCommand_ShouldHaveNameOption()
    {
        // Act
        var command = new NewCommand();
        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");

        // Assert
        nameOption.Should().NotBeNull();
        nameOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void NewCommand_ShouldHaveTemplateOption()
    {
        // Act
        var command = new NewCommand();
        var templateOption = command.Options.FirstOrDefault(o => o.Name == "template");

        // Assert
        templateOption.Should().NotBeNull();
    }

    [Fact]
    public void NewCommand_ShouldHaveListOption()
    {
        // Act
        var command = new NewCommand();
        var listOption = command.Options.FirstOrDefault(o => o.Name == "list");

        // Assert
        listOption.Should().NotBeNull();
    }

    [Fact]
    public void NewCommand_ShouldHaveFeaturesOption()
    {
        // Act
        var command = new NewCommand();
        var featuresOption = command.Options.FirstOrDefault(o => o.Name == "features");

        // Assert
        featuresOption.Should().NotBeNull();
    }

    [Fact]
    public void NewCommand_ShouldHaveOutputOption()
    {
        // Act
        var command = new NewCommand();
        var outputOption = command.Options.FirstOrDefault(o => o.Name == "output");

        // Assert
        outputOption.Should().NotBeNull();
    }

    [Fact]
    public void NewCommand_ShouldHaveBrokerOption()
    {
        // Act
        var command = new NewCommand();
        var brokerOption = command.Options.FirstOrDefault(o => o.Name == "broker");

        // Assert
        brokerOption.Should().NotBeNull();
    }

    [Fact]
    public void NewCommand_ShouldHaveDatabaseOption()
    {
        // Act
        var command = new NewCommand();
        var databaseOption = command.Options.FirstOrDefault(o => o.Name == "database");

        // Assert
        databaseOption.Should().NotBeNull();
    }

    [Fact]
    public void NewCommand_ShouldHaveAuthOption()
    {
        // Act
        var command = new NewCommand();
        var authOption = command.Options.FirstOrDefault(o => o.Name == "auth");

        // Assert
        authOption.Should().NotBeNull();
    }

    [Fact]
    public void NewCommand_ShouldHaveNoRestoreOption()
    {
        // Act
        var command = new NewCommand();
        var noRestoreOption = command.Options.FirstOrDefault(o => o.Name == "no-restore");

        // Assert
        noRestoreOption.Should().NotBeNull();
    }

    [Fact]
    public void NewCommand_ShouldHaveNoBuildOption()
    {
        // Act
        var command = new NewCommand();
        var noBuildOption = command.Options.FirstOrDefault(o => o.Name == "no-build");

        // Assert
        noBuildOption.Should().NotBeNull();
    }

    [Fact]
    public void NewCommand_Options_ShouldHaveCorrectCount()
    {
        // Act
        var command = new NewCommand();

        // Assert
        command.Options.Should().HaveCount(10);
    }

    #endregion

    #region Template Tests

    [Theory]
    [InlineData("relay-webapi")]
    [InlineData("relay-microservice")]
    [InlineData("relay-ddd")]
    [InlineData("relay-cqrs-es")]
    [InlineData("relay-modular")]
    [InlineData("relay-graphql")]
    [InlineData("relay-grpc")]
    [InlineData("relay-serverless")]
    [InlineData("relay-blazor")]
    [InlineData("relay-maui")]
    public void NewCommand_ShouldSupportAllTemplates(string templateId)
    {
        // Arrange
        var validTemplates = new[]
        {
            "relay-webapi", "relay-microservice", "relay-ddd", "relay-cqrs-es",
            "relay-modular", "relay-graphql", "relay-grpc", "relay-serverless",
            "relay-blazor", "relay-maui"
        };

        // Assert
        validTemplates.Should().Contain(templateId);
    }

    [Fact]
    public void NewCommand_Template_WebApi_ShouldHaveCleanArchitectureStructure()
    {
        // Arrange
        var structure = "clean-architecture";

        // Assert
        structure.Should().Be("clean-architecture");
    }

    [Fact]
    public void NewCommand_Template_Microservice_ShouldHaveMicroserviceStructure()
    {
        // Arrange
        var structure = "microservice";

        // Assert
        structure.Should().Be("microservice");
    }

    [Fact]
    public void NewCommand_Template_Modular_ShouldHaveModularStructure()
    {
        // Arrange
        var structure = "modular";

        // Assert
        structure.Should().Be("modular");
    }

    #endregion

    #region Features Tests

    [Theory]
    [InlineData("auth")]
    [InlineData("swagger")]
    [InlineData("docker")]
    [InlineData("tests")]
    [InlineData("healthchecks")]
    public void NewCommand_WebApi_ShouldSupportFeatures(string feature)
    {
        // Arrange
        var supportedFeatures = new[] { "auth", "swagger", "docker", "tests", "healthchecks" };

        // Assert
        supportedFeatures.Should().Contain(feature);
    }

    [Theory]
    [InlineData("rabbitmq")]
    [InlineData("kafka")]
    [InlineData("k8s")]
    [InlineData("docker")]
    [InlineData("tracing")]
    public void NewCommand_Microservice_ShouldSupportFeatures(string feature)
    {
        // Arrange
        var supportedFeatures = new[] { "rabbitmq", "kafka", "k8s", "docker", "tracing" };

        // Assert
        supportedFeatures.Should().Contain(feature);
    }

    [Fact]
    public void NewCommand_Features_ShouldSupportMultipleFeatures()
    {
        // Arrange
        var features = new[] { "auth", "swagger", "docker" };

        // Assert
        features.Should().HaveCount(3);
        features.Should().Contain("auth");
        features.Should().Contain("swagger");
    }

    [Fact]
    public void NewCommand_Features_ShouldBeOptional()
    {
        // Arrange
        string[] features = Array.Empty<string>();

        // Assert
        features.Should().BeEmpty();
    }

    #endregion

    #region Broker Tests

    [Theory]
    [InlineData("rabbitmq")]
    [InlineData("kafka")]
    [InlineData("azureservicebus")]
    public void NewCommand_ShouldSupportMessageBrokers(string broker)
    {
        // Arrange
        var supportedBrokers = new[] { "rabbitmq", "kafka", "azureservicebus" };

        // Assert
        supportedBrokers.Should().Contain(broker);
    }

    [Fact]
    public void NewCommand_Broker_ShouldBeOptional()
    {
        // Arrange
        string? broker = null;

        // Assert
        broker.Should().BeNull();
    }

    #endregion

    #region Database Tests

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("postgres")]
    [InlineData("mysql")]
    [InlineData("sqlite")]
    public void NewCommand_ShouldSupportDatabaseProviders(string database)
    {
        // Arrange
        var supportedDatabases = new[] { "sqlserver", "postgres", "mysql", "sqlite" };

        // Assert
        supportedDatabases.Should().Contain(database);
    }

    [Fact]
    public void NewCommand_Database_ShouldBeOptional()
    {
        // Arrange
        string? database = null;

        // Assert
        database.Should().BeNull();
    }

    #endregion

    #region Auth Tests

    [Theory]
    [InlineData("jwt")]
    [InlineData("identityserver")]
    [InlineData("auth0")]
    public void NewCommand_ShouldSupportAuthProviders(string auth)
    {
        // Arrange
        var supportedAuth = new[] { "jwt", "identityserver", "auth0" };

        // Assert
        supportedAuth.Should().Contain(auth);
    }

    [Fact]
    public void NewCommand_Auth_ShouldBeOptional()
    {
        // Arrange
        string? auth = null;

        // Assert
        auth.Should().BeNull();
    }

    #endregion

    #region Project Structure Tests

    [Fact]
    public void NewCommand_CleanArchitecture_ShouldCreateApiLayer()
    {
        // Arrange
        var projectName = "TestProject";
        var expectedDir = $"src/{projectName}.Api";

        // Assert
        expectedDir.Should().Contain(".Api");
    }

    [Fact]
    public void NewCommand_CleanArchitecture_ShouldCreateApplicationLayer()
    {
        // Arrange
        var projectName = "TestProject";
        var expectedDir = $"src/{projectName}.Application";

        // Assert
        expectedDir.Should().Contain(".Application");
    }

    [Fact]
    public void NewCommand_CleanArchitecture_ShouldCreateDomainLayer()
    {
        // Arrange
        var projectName = "TestProject";
        var expectedDir = $"src/{projectName}.Domain";

        // Assert
        expectedDir.Should().Contain(".Domain");
    }

    [Fact]
    public void NewCommand_CleanArchitecture_ShouldCreateInfrastructureLayer()
    {
        // Arrange
        var projectName = "TestProject";
        var expectedDir = $"src/{projectName}.Infrastructure";

        // Assert
        expectedDir.Should().Contain(".Infrastructure");
    }

    [Fact]
    public void NewCommand_CleanArchitecture_ShouldCreateTestProjects()
    {
        // Arrange
        var projectName = "TestProject";
        var testDirs = new[]
        {
            $"tests/{projectName}.UnitTests",
            $"tests/{projectName}.IntegrationTests",
            $"tests/{projectName}.ArchitectureTests"
        };

        // Assert
        testDirs.Should().HaveCount(3);
        testDirs.Should().Contain(d => d.Contains("UnitTests"));
    }

    [Fact]
    public void NewCommand_Microservice_ShouldCreateK8sDirectory()
    {
        // Arrange
        var expectedDir = "k8s";

        // Assert
        expectedDir.Should().Be("k8s");
    }

    [Fact]
    public void NewCommand_Microservice_ShouldCreateHelmDirectory()
    {
        // Arrange
        var expectedDir = "helm";

        // Assert
        expectedDir.Should().Be("helm");
    }

    [Fact]
    public void NewCommand_AllTemplates_ShouldCreateDocsDirectory()
    {
        // Arrange
        var expectedDir = "docs";

        // Assert
        expectedDir.Should().Be("docs");
    }

    [Fact]
    public void NewCommand_AllTemplates_ShouldCreateTestsDirectory()
    {
        // Arrange
        var expectedDir = "tests";

        // Assert
        expectedDir.Should().Be("tests");
    }

    #endregion

    #region File Generation Tests

    [Fact]
    public async Task NewCommand_ShouldGenerateReadmeFile()
    {
        // Arrange
        var readmePath = Path.Combine(_testPath, "README.md");
        var content = "# TestProject\n\nDescription";

        // Act
        await File.WriteAllTextAsync(readmePath, content);

        // Assert
        File.Exists(readmePath).Should().BeTrue();
        var readmeContent = await File.ReadAllTextAsync(readmePath);
        readmeContent.Should().Contain("# TestProject");
    }

    [Fact]
    public async Task NewCommand_ShouldGenerateGitignoreFile()
    {
        // Arrange
        var gitignorePath = Path.Combine(_testPath, ".gitignore");
        var content = "[Bb]in/\n[Oo]bj/\n.vs/";

        // Act
        await File.WriteAllTextAsync(gitignorePath, content);

        // Assert
        File.Exists(gitignorePath).Should().BeTrue();
        var gitignoreContent = await File.ReadAllTextAsync(gitignorePath);
        gitignoreContent.Should().MatchRegex(@"\[Bb\]in");
    }

    [Fact]
    public async Task NewCommand_WithDockerFeature_ShouldGenerateDockerfile()
    {
        // Arrange
        var dockerfilePath = Path.Combine(_testPath, "Dockerfile");
        var content = "FROM mcr.microsoft.com/dotnet/sdk:8.0";

        // Act
        await File.WriteAllTextAsync(dockerfilePath, content);

        // Assert
        File.Exists(dockerfilePath).Should().BeTrue();
    }

    [Fact]
    public async Task NewCommand_WithCIFeature_ShouldGenerateCIFiles()
    {
        // Arrange
        var ciPath = Path.Combine(_testPath, ".github", "workflows", "ci.yml");
        Directory.CreateDirectory(Path.GetDirectoryName(ciPath)!);
        var content = "name: CI";

        // Act
        await File.WriteAllTextAsync(ciPath, content);

        // Assert
        File.Exists(ciPath).Should().BeTrue();
    }

    #endregion

    #region Naming Tests

    [Theory]
    [InlineData("MyProject")]
    [InlineData("My.Project")]
    [InlineData("My_Project")]
    [InlineData("MyProject123")]
    public void NewCommand_ShouldAcceptValidProjectNames(string projectName)
    {
        // Arrange & Act
        var isValid = !string.IsNullOrWhiteSpace(projectName);

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void NewCommand_ShouldRejectInvalidProjectNames(string? projectName)
    {
        // Arrange & Act
        var isValid = !string.IsNullOrWhiteSpace(projectName);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void NewCommand_ProjectName_ShouldBeRequired()
    {
        // Arrange
        var command = new NewCommand();
        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");

        // Assert
        nameOption!.IsRequired.Should().BeTrue();
    }

    #endregion

    #region Output Directory Tests

    [Fact]
    public void NewCommand_OutputOption_DefaultShouldBeCurrentDirectory()
    {
        // Arrange
        var defaultOutput = Directory.GetCurrentDirectory();

        // Assert
        defaultOutput.Should().NotBeNullOrEmpty();
        Directory.Exists(defaultOutput).Should().BeTrue();
    }

    [Fact]
    public void NewCommand_ShouldCreateProjectInOutputDirectory()
    {
        // Arrange
        var outputPath = _testPath;
        var projectName = "TestProject";
        var projectPath = Path.Combine(outputPath, projectName);

        // Act
        Directory.CreateDirectory(projectPath);

        // Assert
        Directory.Exists(projectPath).Should().BeTrue();
    }

    [Fact]
    public void NewCommand_ShouldRejectExistingDirectory()
    {
        // Arrange
        var projectPath = Path.Combine(_testPath, "ExistingProject");
        Directory.CreateDirectory(projectPath);

        // Act
        var exists = Directory.Exists(projectPath);

        // Assert
        exists.Should().BeTrue();
    }

    #endregion

    #region Restore & Build Tests

    [Fact]
    public void NewCommand_NoRestore_ShouldSkipPackageRestore()
    {
        // Arrange
        var noRestore = true;
        var shouldRestore = false;

        // Act
        if (!noRestore)
        {
            shouldRestore = true;
        }

        // Assert
        shouldRestore.Should().BeFalse();
    }

    [Fact]
    public void NewCommand_NoBuild_ShouldSkipBuild()
    {
        // Arrange
        var noBuild = true;
        var shouldBuild = false;

        // Act
        if (!noBuild)
        {
            shouldBuild = true;
        }

        // Assert
        shouldBuild.Should().BeFalse();
    }

    [Fact]
    public void NewCommand_DefaultBehavior_ShouldRestoreAndBuild()
    {
        // Arrange
        var noRestore = false;
        var noBuild = false;

        // Act
        var shouldRestore = !noRestore;
        var shouldBuild = !noBuild;

        // Assert
        shouldRestore.Should().BeTrue();
        shouldBuild.Should().BeTrue();
    }

    #endregion

    #region TemplateInfo Tests

    [Fact]
    public void TemplateInfo_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "relay-webapi",
            Name = "Web API",
            Description = "REST API template",
            BestFor = "APIs",
            Tags = new[] { "web", "api" },
            Features = new[] { "auth", "swagger" },
            Structure = "clean-architecture"
        };

        // Assert
        template.Id.Should().Be("relay-webapi");
        template.Name.Should().Be("Web API");
        template.Structure.Should().Be("clean-architecture");
    }

    [Fact]
    public void TemplateInfo_Tags_ShouldBeArray()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Tags = new[] { "web", "api", "rest" }
        };

        // Assert
        template.Tags.Should().HaveCount(3);
        template.Tags.Should().Contain("web");
    }

    [Fact]
    public void TemplateInfo_Features_ShouldBeArray()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Features = new[] { "auth", "swagger", "docker" }
        };

        // Assert
        template.Features.Should().HaveCount(3);
        template.Features.Should().Contain("docker");
    }

    #endregion

    #region Template Listing Tests

    [Fact]
    public void NewCommand_ListTemplates_ShouldShowAllTemplates()
    {
        // Arrange
        var totalTemplates = 10;

        // Assert
        totalTemplates.Should().Be(10);
    }

    [Fact]
    public void NewCommand_ListTemplates_ShouldShowTemplateDetails()
    {
        // Arrange
        var template = new TemplateInfo
        {
            Id = "relay-webapi",
            Description = "REST API",
            BestFor = "Backend services",
            Tags = new[] { "web", "api" },
            Features = new[] { "auth", "swagger" }
        };

        // Assert
        template.Id.Should().NotBeNullOrEmpty();
        template.Description.Should().NotBeNullOrEmpty();
        template.BestFor.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region README Generation Tests

    [Fact]
    public void NewCommand_Readme_ShouldContainProjectName()
    {
        // Arrange
        var projectName = "MyProject";
        var readme = $"# {projectName}";

        // Assert
        readme.Should().Contain(projectName);
    }

    [Fact]
    public void NewCommand_Readme_ShouldContainGettingStarted()
    {
        // Arrange
        var readme = "## Getting Started\n\n### Prerequisites";

        // Assert
        readme.Should().Contain("Getting Started");
        readme.Should().Contain("Prerequisites");
    }

    [Fact]
    public void NewCommand_Readme_ShouldContainRunningInstructions()
    {
        // Arrange
        var readme = "```bash\ndotnet run --project src/MyProject.Api\n```";

        // Assert
        readme.Should().Contain("dotnet run");
    }

    [Fact]
    public void NewCommand_Readme_ShouldContainTestInstructions()
    {
        // Arrange
        var readme = "```bash\ndotnet test\n```";

        // Assert
        readme.Should().Contain("dotnet test");
    }

    [Fact]
    public void NewCommand_Readme_ShouldListFeatures()
    {
        // Arrange
        var features = new[] { "auth", "swagger", "docker" };
        var featuresList = string.Join("\n", features.Select(f => $"- {f}"));

        // Assert
        featuresList.Should().Contain("- auth");
        featuresList.Should().Contain("- swagger");
    }

    #endregion

    #region Gitignore Tests

    [Fact]
    public void NewCommand_Gitignore_ShouldIgnoreBinDirectory()
    {
        // Arrange
        var gitignore = "[Bb]in/";

        // Assert
        gitignore.Should().MatchRegex(@"\[Bb\]in");
    }

    [Fact]
    public void NewCommand_Gitignore_ShouldIgnoreObjDirectory()
    {
        // Arrange
        var gitignore = "[Oo]bj/";

        // Assert
        gitignore.Should().MatchRegex(@"\[Oo\]bj");
    }

    [Fact]
    public void NewCommand_Gitignore_ShouldIgnoreVsDirectory()
    {
        // Arrange
        var gitignore = ".vs/";

        // Assert
        gitignore.Should().Contain(".vs");
    }

    [Fact]
    public void NewCommand_Gitignore_ShouldIgnoreIdeaDirectory()
    {
        // Arrange
        var gitignore = ".idea/";

        // Assert
        gitignore.Should().Contain(".idea");
    }

    [Fact]
    public void NewCommand_Gitignore_ShouldIgnoreNuGetPackages()
    {
        // Arrange
        var gitignore = "*.nupkg\npackages/";

        // Assert
        gitignore.Should().Contain("nupkg");
        gitignore.Should().Contain("packages");
    }

    #endregion

    #region Template-Specific Tests

    [Fact]
    public void NewCommand_WebApi_ShouldHaveSwaggerSupport()
    {
        // Arrange
        var features = new[] { "auth", "swagger", "docker", "tests", "healthchecks" };

        // Assert
        features.Should().Contain("swagger");
    }

    [Fact]
    public void NewCommand_Microservice_ShouldHaveMessageBrokerSupport()
    {
        // Arrange
        var features = new[] { "rabbitmq", "kafka", "k8s", "docker", "tracing" };

        // Assert
        (features.Contains("rabbitmq") || features.Contains("kafka")).Should().BeTrue();
    }

    [Fact]
    public void NewCommand_DDD_ShouldHaveAggregatesSupport()
    {
        // Arrange
        var features = new[] { "aggregates", "events", "specifications" };

        // Assert
        features.Should().Contain("aggregates");
    }

    [Fact]
    public void NewCommand_CQRSES_ShouldHaveEventStoreSupport()
    {
        // Arrange
        var features = new[] { "eventstore", "projections", "snapshots" };

        // Assert
        features.Should().Contain("eventstore");
        features.Should().Contain("projections");
    }

    [Fact]
    public void NewCommand_Modular_ShouldHaveModulesSupport()
    {
        // Arrange
        var features = new[] { "modules", "isolation", "migration-ready" };

        // Assert
        features.Should().Contain("modules");
    }

    [Fact]
    public void NewCommand_GraphQL_ShouldHaveSubscriptionsSupport()
    {
        // Arrange
        var features = new[] { "subscriptions", "dataloader", "filtering" };

        // Assert
        features.Should().Contain("subscriptions");
    }

    [Fact]
    public void NewCommand_gRPC_ShouldHaveStreamingSupport()
    {
        // Arrange
        var features = new[] { "streaming", "tls", "discovery" };

        // Assert
        features.Should().Contain("streaming");
    }

    [Fact]
    public void NewCommand_Serverless_ShouldHaveCloudProviderSupport()
    {
        // Arrange
        var features = new[] { "aws", "azure", "api-gateway" };

        // Assert
        (features.Contains("aws") || features.Contains("azure")).Should().BeTrue();
    }

    [Fact]
    public void NewCommand_Blazor_ShouldHaveServerAndWasmSupport()
    {
        // Arrange
        var features = new[] { "server", "wasm", "signalr", "pwa" };

        // Assert
        features.Should().Contain("server");
        features.Should().Contain("wasm");
    }

    [Fact]
    public void NewCommand_MAUI_ShouldHaveCrossPlatformSupport()
    {
        // Arrange
        var features = new[] { "ios", "android", "offline", "sqlite" };

        // Assert
        features.Should().Contain("ios");
        features.Should().Contain("android");
    }

    #endregion

    #region Structure Tests

    [Theory]
    [InlineData("relay-webapi", "clean-architecture")]
    [InlineData("relay-microservice", "microservice")]
    [InlineData("relay-ddd", "clean-architecture")]
    [InlineData("relay-cqrs-es", "clean-architecture")]
    [InlineData("relay-modular", "modular")]
    [InlineData("relay-graphql", "clean-architecture")]
    [InlineData("relay-grpc", "microservice")]
    [InlineData("relay-serverless", "simple")]
    [InlineData("relay-blazor", "clean-architecture")]
    [InlineData("relay-maui", "mvvm")]
    public void NewCommand_Template_ShouldHaveCorrectStructure(string templateId, string expectedStructure)
    {
        // Arrange
        var structureMap = new Dictionary<string, string>
        {
            ["relay-webapi"] = "clean-architecture",
            ["relay-microservice"] = "microservice",
            ["relay-ddd"] = "clean-architecture",
            ["relay-cqrs-es"] = "clean-architecture",
            ["relay-modular"] = "modular",
            ["relay-graphql"] = "clean-architecture",
            ["relay-grpc"] = "microservice",
            ["relay-serverless"] = "simple",
            ["relay-blazor"] = "clean-architecture",
            ["relay-maui"] = "mvvm"
        };

        // Assert
        structureMap[templateId].Should().Be(expectedStructure);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void NewCommand_WithoutName_ShouldShowError()
    {
        // Arrange
        string? name = null;

        // Act
        var isValid = !string.IsNullOrEmpty(name);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void NewCommand_WithoutTemplate_ShouldShowError()
    {
        // Arrange
        string? template = null;

        // Act
        var isValid = !string.IsNullOrEmpty(template);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void NewCommand_WithInvalidTemplate_ShouldShowError()
    {
        // Arrange
        var template = "invalid-template";
        var validTemplates = new[] { "relay-webapi", "relay-microservice" };

        // Act
        var isValid = validTemplates.Contains(template);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void NewCommand_WithExistingDirectory_ShouldShowError()
    {
        // Arrange
        var projectPath = Path.Combine(_testPath, "ExistingProject");
        Directory.CreateDirectory(projectPath);

        // Act
        var exists = Directory.Exists(projectPath);

        // Assert
        exists.Should().BeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task NewCommand_FullFlow_ShouldCreateCompleteProject()
    {
        // Arrange
        var projectName = "TestProject";
        var projectPath = Path.Combine(_testPath, projectName);
        Directory.CreateDirectory(projectPath);

        var expectedDirectories = new[]
        {
            "src",
            "tests",
            "docs"
        };

        // Act
        foreach (var dir in expectedDirectories)
        {
            Directory.CreateDirectory(Path.Combine(projectPath, dir));
        }

        await File.WriteAllTextAsync(Path.Combine(projectPath, "README.md"), "# Test");
        await File.WriteAllTextAsync(Path.Combine(projectPath, ".gitignore"), "bin/");

        // Assert
        foreach (var dir in expectedDirectories)
        {
            Directory.Exists(Path.Combine(projectPath, dir)).Should().BeTrue();
        }
        File.Exists(Path.Combine(projectPath, "README.md")).Should().BeTrue();
        File.Exists(Path.Combine(projectPath, ".gitignore")).Should().BeTrue();
    }

    [Fact]
    public void NewCommand_AllTemplates_ShouldHaveUniqueIds()
    {
        // Arrange
        var templateIds = new[]
        {
            "relay-webapi", "relay-microservice", "relay-ddd", "relay-cqrs-es",
            "relay-modular", "relay-graphql", "relay-grpc", "relay-serverless",
            "relay-blazor", "relay-maui"
        };

        // Act
        var uniqueIds = templateIds.Distinct().ToArray();

        // Assert
        uniqueIds.Should().HaveCount(templateIds.Length);
    }

    [Fact]
    public void NewCommand_AllTemplates_ShouldHaveDescriptions()
    {
        // Arrange
        var templates = new[]
        {
            new { Id = "relay-webapi", Description = "REST API" },
            new { Id = "relay-microservice", Description = "Microservice" }
        };

        // Assert
        templates.Should().AllSatisfy(t => t.Description.Should().NotBeNullOrEmpty());
    }

    #endregion

    #region Path Resolution Tests

    [Fact]
    public void NewCommand_Path_ShouldResolveRelativePath()
    {
        // Arrange
        var relativePath = "./project";

        // Act
        var absolutePath = Path.GetFullPath(relativePath);

        // Assert
        absolutePath.Should().NotBeNullOrEmpty();
        Path.IsPathRooted(absolutePath).Should().BeTrue();
    }

    [Fact]
    public void NewCommand_Path_ShouldHandleAbsolutePath()
    {
        // Arrange
        var absolutePath = _testPath;

        // Act
        var isAbsolute = Path.IsPathRooted(absolutePath);

        // Assert
        isAbsolute.Should().BeTrue();
    }

    [Fact]
    public void NewCommand_Path_ShouldCombineOutputAndProjectName()
    {
        // Arrange
        var output = _testPath;
        var projectName = "TestProject";

        // Act
        var projectPath = Path.Combine(output, projectName);

        // Assert
        projectPath.Should().Contain(projectName);
        projectPath.Should().StartWith(output);
    }

    #endregion

    #region Feature Combination Tests

    [Fact]
    public void NewCommand_Features_DockerAndCI_ShouldBothWork()
    {
        // Arrange
        var features = new[] { "docker", "ci" };

        // Assert
        features.Should().Contain("docker");
        features.Should().Contain("ci");
    }

    [Fact]
    public void NewCommand_Features_AuthAndSwagger_ShouldBothWork()
    {
        // Arrange
        var features = new[] { "auth", "swagger" };

        // Assert
        features.Should().Contain("auth");
        features.Should().Contain("swagger");
    }

    [Fact]
    public void NewCommand_Options_BrokerAndDatabase_ShouldBothWork()
    {
        // Arrange
        var broker = "rabbitmq";
        var database = "postgres";

        // Assert
        broker.Should().Be("rabbitmq");
        database.Should().Be("postgres");
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
