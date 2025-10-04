using Relay.CLI.Commands;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;

namespace Relay.CLI.Tests.Commands;

public class ScaffoldCommandTests : IDisposable
{
    private readonly string _testPath;

    public ScaffoldCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-scaffold-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    [Fact]
    public async Task ScaffoldCommand_WithValidOptions_GeneratesAllFiles()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync(
            $"--handler CreateUserHandler --request CreateUserRequest --response CreateUserResponse --namespace TestApp --output {_testPath} --include-tests true",
            console);

        // Assert
        result.Should().Be(0);
        File.Exists(Path.Combine(_testPath, "CreateUserRequest.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_testPath, "CreateUserHandler.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_testPath, "CreateUserHandlerTests.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_testPath, "CreateUserHandlerIntegrationTests.cs")).Should().BeTrue();
    }

    [Fact]
    public async Task ScaffoldCommand_StandardTemplate_GeneratesCorrectRequestFile()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler TestHandler --request TestRequest --response TestResponse --namespace MyApp --output {_testPath} --template standard --include-tests false",
            console);

        // Assert
        var requestFile = Path.Combine(_testPath, "TestRequest.cs");
        File.Exists(requestFile).Should().BeTrue();

        var content = await File.ReadAllTextAsync(requestFile);
        content.Should().Contain("namespace MyApp");
        content.Should().Contain("public record TestRequest");
        content.Should().Contain("IRequest<TestResponse>");
        content.Should().Contain("ExampleParameter");
    }

    [Fact]
    public async Task ScaffoldCommand_MinimalTemplate_GeneratesMinimalCode()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler MinimalHandler --request MinimalRequest --namespace App --output {_testPath} --template minimal --include-tests false",
            console);

        // Assert
        var requestFile = Path.Combine(_testPath, "MinimalRequest.cs");
        var handlerFile = Path.Combine(_testPath, "MinimalHandler.cs");

        var requestContent = await File.ReadAllTextAsync(requestFile);
        var handlerContent = await File.ReadAllTextAsync(handlerFile);

        requestContent.Should().Contain("using Relay.Core;");
        requestContent.Should().NotContain("System.ComponentModel.DataAnnotations");
        handlerContent.Should().NotContain("ILogger");
    }

    [Fact]
    public async Task ScaffoldCommand_EnterpriseTemplate_GeneratesEnterpriseFeatures()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler EnterpriseHandler --request EnterpriseRequest --response EnterpriseResponse --namespace EntApp --output {_testPath} --template enterprise --include-tests false",
            console);

        // Assert
        var requestFile = Path.Combine(_testPath, "EnterpriseRequest.cs");
        var handlerFile = Path.Combine(_testPath, "EnterpriseHandler.cs");

        var requestContent = await File.ReadAllTextAsync(requestFile);
        var handlerContent = await File.ReadAllTextAsync(handlerFile);

        requestContent.Should().Contain("[Cacheable");
        requestContent.Should().Contain("[Authorize");
        requestContent.Should().Contain("CorrelationId");
        handlerContent.Should().Contain("ILogger<EnterpriseHandler>");
        handlerContent.Should().Contain("PERFORMANCE TIPS");
    }

    [Fact]
    public async Task ScaffoldCommand_WithValidation_GeneratesValidationAttributes()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler ValidatedHandler --request ValidatedRequest --namespace App --output {_testPath} --include-validation true --include-tests false",
            console);

        // Assert
        var requestFile = Path.Combine(_testPath, "ValidatedRequest.cs");
        var content = await File.ReadAllTextAsync(requestFile);

        content.Should().Contain("[Required");
        content.Should().Contain("[StringLength");
    }

    [Fact]
    public async Task ScaffoldCommand_WithoutResponse_GeneratesIRequest()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler CommandHandler --request DoSomethingCommand --namespace App --output {_testPath} --include-tests false",
            console);

        // Assert
        var requestFile = Path.Combine(_testPath, "DoSomethingCommand.cs");
        var handlerFile = Path.Combine(_testPath, "CommandHandler.cs");

        var requestContent = await File.ReadAllTextAsync(requestFile);
        var handlerContent = await File.ReadAllTextAsync(handlerFile);

        requestContent.Should().Contain(": IRequest;");
        handlerContent.Should().Contain("public async ValueTask Handle");
    }

    [Fact]
    public async Task ScaffoldCommand_GeneratesHandlerWithHandleAttribute()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler TestHandler --request TestRequest --namespace App --output {_testPath} --include-tests false",
            console);

        // Assert
        var handlerFile = Path.Combine(_testPath, "TestHandler.cs");
        var content = await File.ReadAllTextAsync(handlerFile);

        content.Should().Contain("[Handle]");
        content.Should().Contain("CancellationToken cancellationToken = default");
    }

    [Fact]
    public async Task ScaffoldCommand_GeneratesUnitTests_WithProperStructure()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler UserHandler --request UserRequest --response UserResponse --namespace MyApp --output {_testPath} --include-tests true",
            console);

        // Assert
        var testFile = Path.Combine(_testPath, "UserHandlerTests.cs");
        var content = await File.ReadAllTextAsync(testFile);

        content.Should().Contain("public class UserHandlerTests");
        content.Should().Contain("[Fact]");
        content.Should().Contain("WithValidRequest_ShouldReturnExpectedResult");
        content.Should().Contain("WithCancellation_ShouldRespectCancellationToken");
        content.Should().Contain("[Theory]");
        content.Should().Contain("Mock<ILogger<UserHandler>>");
    }

    [Fact]
    public async Task ScaffoldCommand_EnterpriseTemplate_GeneratesPerformanceTest()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler PerfHandler --request PerfRequest --response PerfResponse --namespace App --output {_testPath} --template enterprise --include-tests true",
            console);

        // Assert
        var testFile = Path.Combine(_testPath, "PerfHandlerTests.cs");
        var content = await File.ReadAllTextAsync(testFile);

        content.Should().Contain("Performance_ShouldBeFast");
        content.Should().Contain("Stopwatch");
        content.Should().Contain("avgTime < 10");
    }

    [Fact]
    public async Task ScaffoldCommand_GeneratesIntegrationTests()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler IntHandler --request IntRequest --response IntResponse --namespace App --output {_testPath} --include-tests true",
            console);

        // Assert
        var testFile = Path.Combine(_testPath, "IntHandlerIntegrationTests.cs");
        var content = await File.ReadAllTextAsync(testFile);

        content.Should().Contain("public class IntHandlerIntegrationTests");
        content.Should().Contain("ThroughRelay_ShouldWorkEndToEnd");
        content.Should().Contain("WithTestHarness_ShouldProvideTestingUtilities");
        content.Should().Contain("IRelay");
        content.Should().Contain("RelayTestHarness");
    }

    [Fact]
    public async Task ScaffoldCommand_WithoutTests_DoesNotGenerateTestFiles()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler NoTestHandler --request NoTestRequest --namespace App --output {_testPath} --include-tests false",
            console);

        // Assert
        File.Exists(Path.Combine(_testPath, "NoTestHandlerTests.cs")).Should().BeFalse();
        File.Exists(Path.Combine(_testPath, "NoTestHandlerIntegrationTests.cs")).Should().BeFalse();
    }

    [Fact]
    public async Task ScaffoldCommand_GeneratesResponseRecord()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler RespHandler --request RespRequest --response RespResponse --namespace App --output {_testPath} --include-tests false",
            console);

        // Assert
        var requestFile = Path.Combine(_testPath, "RespRequest.cs");
        var content = await File.ReadAllTextAsync(requestFile);

        content.Should().Contain("public record RespResponse");
        content.Should().Contain("ExampleResult");
        content.Should().Contain("IsSuccess");
        content.Should().Contain("ErrorMessage");
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("minimal")]
    [InlineData("enterprise")]
    public async Task ScaffoldCommand_AllTemplates_GenerateValidCode(string template)
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();
        var handlerName = $"{template}Handler";
        var requestName = $"{template}Request";

        // Act
        var result = await command.InvokeAsync(
            $"--handler {handlerName} --request {requestName} --namespace App --output {_testPath} --template {template} --include-tests false",
            console);

        // Assert
        result.Should().Be(0);
        File.Exists(Path.Combine(_testPath, $"{requestName}.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_testPath, $"{handlerName}.cs")).Should().BeTrue();
    }

    [Fact]
    public async Task ScaffoldCommand_CustomNamespace_GeneratesCorrectNamespace()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();
        var customNamespace = "MyCompany.MyProduct.Features.Users";

        // Act
        await command.InvokeAsync(
            $"--handler UserHandler --request CreateUserRequest --namespace {customNamespace} --output {_testPath} --include-tests false",
            console);

        // Assert
        var requestFile = Path.Combine(_testPath, "CreateUserRequest.cs");
        var content = await File.ReadAllTextAsync(requestFile);

        content.Should().Contain($"namespace {customNamespace}");
    }

    [Fact]
    public async Task ScaffoldCommand_DefaultNamespace_UsesYourApp()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler DefaultHandler --request DefaultRequest --output {_testPath} --include-tests false",
            console);

        // Assert
        var requestFile = Path.Combine(_testPath, "DefaultRequest.cs");
        var content = await File.ReadAllTextAsync(requestFile);

        content.Should().Contain("namespace YourApp");
    }

    [Fact]
    public async Task ScaffoldCommand_HandlerWithDependencies_GeneratesConstructor()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler DependentHandler --request DependentRequest --namespace App --output {_testPath} --template standard --include-tests false",
            console);

        // Assert
        var handlerFile = Path.Combine(_testPath, "DependentHandler.cs");
        var content = await File.ReadAllTextAsync(handlerFile);

        content.Should().Contain("private readonly ILogger<DependentHandler> _logger");
        content.Should().Contain("public DependentHandler(ILogger<DependentHandler> logger)");
    }

    [Fact]
    public async Task ScaffoldCommand_RequestWithResponse_UsesCorrectGenericType()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler QueryHandler --request GetDataQuery --response DataResult --namespace App --output {_testPath} --include-tests false",
            console);

        // Assert
        var requestFile = Path.Combine(_testPath, "GetDataQuery.cs");
        var content = await File.ReadAllTextAsync(requestFile);

        content.Should().Contain(": IRequest<DataResult>");
    }

    [Fact]
    public async Task ScaffoldCommand_Tests_IncludeMockingSetup()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler MockHandler --request MockRequest --namespace App --output {_testPath} --include-tests true",
            console);

        // Assert
        var testFile = Path.Combine(_testPath, "MockHandlerTests.cs");
        var content = await File.ReadAllTextAsync(testFile);

        content.Should().Contain("CreateHandler");
        content.Should().Contain("new Mock<ILogger<MockHandler>>()");
        content.Should().Contain("mockLogger.Object");
    }

    [Fact]
    public async Task ScaffoldCommand_HandlerWithoutResponse_UsesValueTaskReturnType()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler CommandOnlyHandler --request ExecuteCommand --namespace App --output {_testPath} --include-tests false",
            console);

        // Assert
        var handlerFile = Path.Combine(_testPath, "CommandOnlyHandler.cs");
        var content = await File.ReadAllTextAsync(handlerFile);

        content.Should().Contain("public async ValueTask Handle");
        content.Should().NotContain("ValueTask<");
    }

    [Fact]
    public async Task ScaffoldCommand_IntegrationTest_UsesRelayTestHarness()
    {
        // Arrange
        var command = ScaffoldCommand.Create();
        var console = new TestConsole();

        // Act
        await command.InvokeAsync(
            $"--handler TestHarnessHandler --request TestRequest --response TestResponse --namespace App --output {_testPath} --include-tests true",
            console);

        // Assert
        var integrationTestFile = Path.Combine(_testPath, "TestHarnessHandlerIntegrationTests.cs");
        var content = await File.ReadAllTextAsync(integrationTestFile);

        content.Should().Contain("RelayTestHarness.CreateTestRelay");
        content.Should().Contain("Relay.Core.Testing");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, true);
        }
    }
}
