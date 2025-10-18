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
        Assert.Equal(0, result);
        Assert.True(File.Exists(Path.Combine(_testPath, "CreateUserRequest.cs")));
        Assert.True(File.Exists(Path.Combine(_testPath, "CreateUserHandler.cs")));
        Assert.True(File.Exists(Path.Combine(_testPath, "CreateUserHandlerTests.cs")));
        Assert.True(File.Exists(Path.Combine(_testPath, "CreateUserHandlerIntegrationTests.cs")));
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
        Assert.True(File.Exists(requestFile));

        var content = await File.ReadAllTextAsync(requestFile);
        Assert.Contains("namespace MyApp", content);
        Assert.Contains("public record TestRequest", content);
        Assert.Contains("IRequest<TestResponse>", content);
        Assert.Contains("ExampleParameter", content);
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

        Assert.Contains("using Relay.Core;", requestContent);
        Assert.DoesNotContain("System.ComponentModel.DataAnnotations", requestContent);
        Assert.DoesNotContain("ILogger", handlerContent);
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

        Assert.Contains("[Cacheable", requestContent);
        Assert.Contains("[Authorize", requestContent);
        Assert.Contains("CorrelationId", requestContent);
        Assert.Contains("ILogger<EnterpriseHandler>", handlerContent);
        Assert.Contains("PERFORMANCE TIPS", handlerContent);
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

        Assert.Contains("[Required", content);
        Assert.Contains("[StringLength", content);
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

        Assert.Contains(": IRequest;", requestContent);
        Assert.Contains("public async ValueTask Handle", handlerContent);
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

        Assert.Contains("[Handle]", content);
        Assert.Contains("CancellationToken cancellationToken = default", content);
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

        Assert.Contains("public class UserHandlerTests", content);
        Assert.Contains("[Fact]", content);
        Assert.Contains("WithValidRequest_ShouldReturnExpectedResult", content);
        Assert.Contains("WithCancellation_ShouldRespectCancellationToken", content);
        Assert.Contains("[Theory]", content);
        Assert.Contains("Mock<ILogger<UserHandler>>", content);
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

        Assert.Contains("Performance_ShouldBeFast", content);
        Assert.Contains("Stopwatch", content);
        Assert.Contains("avgTime < 10", content);
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

        Assert.Contains("public class IntHandlerIntegrationTests", content);
        Assert.Contains("ThroughRelay_ShouldWorkEndToEnd", content);
        Assert.Contains("WithTestHarness_ShouldProvideTestingUtilities", content);
        Assert.Contains("IRelay", content);
        Assert.Contains("RelayTestHarness", content);
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
        Assert.False(File.Exists(Path.Combine(_testPath, "NoTestHandlerTests.cs")));
        Assert.False(File.Exists(Path.Combine(_testPath, "NoTestHandlerIntegrationTests.cs")));
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

        Assert.Contains("public record RespResponse", content);
        Assert.Contains("ExampleResult", content);
        Assert.Contains("IsSuccess", content);
        Assert.Contains("ErrorMessage", content);
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
        Assert.Equal(0, result);
        Assert.True(File.Exists(Path.Combine(_testPath, $"{requestName}.cs")));
        Assert.True(File.Exists(Path.Combine(_testPath, $"{handlerName}.cs")));
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

        Assert.Contains($"namespace {customNamespace}", content);
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

        Assert.Contains("namespace YourApp", content);
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

        Assert.Contains("private readonly ILogger<DependentHandler> _logger", content);
        Assert.Contains("public DependentHandler(ILogger<DependentHandler> logger)", content);
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

        Assert.Contains(": IRequest<DataResult>", content);
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

        Assert.Contains("CreateHandler", content);
        Assert.Contains("new Mock<ILogger<MockHandler>>()", content);
        Assert.Contains("mockLogger.Object", content);
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

        Assert.Contains("public async ValueTask Handle", content);
        Assert.DoesNotContain("ValueTask<", content);
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

        Assert.Contains("RelayTestHarness.CreateTestRelay", content);
        Assert.Contains("Relay.Core.Testing", content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, true);
        }
    }
}


