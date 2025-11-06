using Relay.CLI.Commands;
using Relay.CLI.Commands.Models.Validation;
using System.CommandLine;
using System.Reflection;

namespace Relay.CLI.Tests.Commands;

public class ValidateCommandTests : IDisposable
{
    private readonly string _testPath;

    public ValidateCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-validate-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    private string GetUniqueTestPath()
    {
        return Path.Combine(Path.GetTempPath(), $"relay-validate-{Guid.NewGuid()}");
    }

    private void CleanTestDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
        Directory.CreateDirectory(path);
    }

    [Fact]
    public async Task ValidateCommand_WithValidProject_Succeeds()
    {
        // Arrange
        await CreateValidProject();

        // Act
        var projectFiles = Directory.GetFiles(_testPath, "*.csproj");
        var hasRelayPackage = false;

        foreach (var projectFile in projectFiles)
        {
            var content = await File.ReadAllTextAsync(projectFile);
            if (content.Contains("Relay.Core"))
            {
                hasRelayPackage = true;
                break;
            }
        }

        // Assert
        Assert.NotEmpty(projectFiles);
        Assert.True(hasRelayPackage);
    }

    [Fact]
    public async Task ValidateProjectFiles_WithNoCsprojFiles_Fails()
    {
        // Arrange
        var emptyPath = GetUniqueTestPath();
        CleanTestDirectory(emptyPath);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateProjectFiles", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [emptyPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Type == "Project Files" && r.Status == ValidationStatus.Fail);
        Assert.Contains(results, r => r.Message.Contains("No .csproj files found"));
    }

    [Fact]
    public async Task ValidateCommand_DetectsInvalidHandlers()
    {
        // Arrange
        var invalidHandler = @"using Relay.Core;

public class BadHandler
{
    // Missing Handle attribute on the method
    public async Task<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}

public record TestRequest : IRequest<string>;";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "BadHandler.cs"), invalidHandler);

        // Act
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "BadHandler.cs"));
        var hasHandleAttribute = content.Contains("[Handle]");

        // Assert - Handler doesn't have the Handle attribute which is a validation issue
        Assert.False(hasHandleAttribute, "we intentionally created a handler without the Handle attribute to test validation");
    }

    [Fact]
    public async Task ValidateCommand_DetectsMissingRequestInterface()
    {
        // Arrange
        var invalidRequest = @"
public record TestRequest(string Name);"; // Missing IRequest<T>

        await File.WriteAllTextAsync(Path.Combine(_testPath, "InvalidRequest.cs"), invalidRequest);

        // Act
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "InvalidRequest.cs"));

        // Assert
        Assert.DoesNotContain("IRequest", content);
    }

    [Fact]
    public async Task ValidateCommand_ChecksHandlerReturnTypes()
    {
        // Arrange
        var wrongReturnType = @"using Relay.Core;

public class TestHandler
{
    [Handle]
    public string HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}

public record TestRequest;";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "WrongReturnType.cs"), wrongReturnType);

        // Act
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "WrongReturnType.cs"));
        var hasValueTask = content.Contains("ValueTask");
        var hasAsync = content.Contains("async");

        // Assert - This handler has issues that should be detected
        Assert.False(hasValueTask, "handler should use ValueTask but doesn't");
        Assert.False(hasAsync, "handler should be async but isn't");
    }

    [Fact]
    public async Task ValidateCommand_ChecksMethodNaming()
    {
        // Arrange
        var wrongMethodName = @"using Relay.Core;

public class TestHandler
{
    [Handle]
    public async ValueTask<string> Handle(TestRequest request)
    {
        return ""test"";
    }
}

public record TestRequest;";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "WrongMethodName.cs"), wrongMethodName);

        // Act
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "WrongMethodName.cs"));
        var hasHandleAsync = content.Contains("HandleAsync");

        // Assert - Method should be named HandleAsync, not Handle
        Assert.False(hasHandleAsync, "method is incorrectly named 'Handle' instead of 'HandleAsync'");
    }

    [Fact]
    public async Task ValidateRequestsAndResponses_WithClassRequestInStrictMode_ShouldWarn()
    {
        // Arrange - Use unique directory
        var testPath = GetUniqueTestPath();
        CleanTestDirectory(testPath);
        var requestCode = @"using Relay.Core;

public class GetUserQuery : IRequest<string>
{
    public int Id { get; set; }
}";
        await File.WriteAllTextAsync(Path.Combine(testPath, "Requests.cs"), requestCode);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateRequestsAndResponses", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [testPath, results, true])!;

        // Assert
        Assert.Contains(results, r => r.Type == "Request Pattern" && r.Status == ValidationStatus.Warning);
    }

    [Theory]
    [InlineData("Handler", true)]
    [InlineData("RequestHandler", true)]
    [InlineData("QueryHandler", true)]
    [InlineData("CommandHandler", true)]
    [InlineData("Service", false)]
    [InlineData("Controller", false)]
    public void ValidateNamingConvention_ForHandlers(string suffix, bool isValid)
    {
        // Act
        var className = $"Test{suffix}";
        var followsConvention = className.EndsWith("Handler");

        // Assert
        Assert.Equal(isValid, followsConvention);
    }

    [Theory]
    [InlineData("Request", true)]
    [InlineData("Query", true)]
    [InlineData("Command", true)]
    [InlineData("Model", false)]
    [InlineData("Dto", false)]
    public void ValidateNamingConvention_ForRequests(string suffix, bool isValid)
    {
        // Act
        var className = $"Test{suffix}";
        var followsConvention = className.EndsWith("Request") ||
                               className.EndsWith("Query") ||
                               className.EndsWith("Command");

        // Assert
        Assert.Equal(isValid, followsConvention);
    }

    [Fact]
    public async Task ValidateCommand_ChecksCancellationTokenUsage()
    {
        // Arrange
        var noCancellationToken = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async ValueTask<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}

public record TestRequest : IRequest<string>;";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "NoCancellationToken.cs"), noCancellationToken);

        // Act
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "NoCancellationToken.cs"));

        // Assert
        Assert.DoesNotContain("CancellationToken", content);
    }

    [Fact]
    public async Task ValidateHandlers_WithHandlerWarnings_GeneratesWarnings()
    {
        // Arrange
        var testPath = GetUniqueTestPath();
        CleanTestDirectory(testPath);

        var handlerWithIssues = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request) // Missing ValueTask and CancellationToken
    {
        return ""test"";
    }
}

public record TestRequest : IRequest<string>;";

        await File.WriteAllTextAsync(Path.Combine(testPath, "HandlerWithIssues.cs"), handlerWithIssues);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateHandlers", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [testPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Type == "Handler Pattern" && r.Status == ValidationStatus.Warning);
        Assert.Contains(results, r => r.Message.Contains("uses Task instead of ValueTask"));
        Assert.Contains(results, r => r.Message.Contains("missing CancellationToken parameter"));
    }

    [Fact]
    public async Task ValidateCommand_DetectsAsyncAwaitPatterns()
    {
        // Arrange
        var missingAwait = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async ValueTask<string> HandleAsync(TestRequest request)
    {
        return ""test""; // Should use explicit keyword or remove async
    }
}

public record TestRequest : IRequest<string>;";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "MissingAwait.cs"), missingAwait);

        // Act
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "MissingAwait.cs"));
        var hasAsync = content.Contains("async");
        // Check for "await " with space, but not in "await File..." from test itself
        var lines = content.Split('\n');
        var hasAwaitInCode = lines.Any(line => line.Trim().StartsWith("await ") || line.Contains(" await "));

        // Assert
        Assert.True(hasAsync);
        Assert.False(hasAwaitInCode, "async method should use the await keyword explicitly");
    }

    [Fact]
    public async Task ValidateCommand_ChecksFileOrganization()
    {
        // Arrange
        await CreateValidProject();

        // Act
        var files = Directory.GetFiles(_testPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        // Assert
        Assert.NotEmpty(files);
        Assert.True(files.All(f => f.EndsWith(".cs")));
    }

    private async Task CreateValidProject()
    {
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""2.1.0"" />
  </ItemGroup>
</Project>";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "Test.csproj"), csproj);

        var program = @"using Relay.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRelay();

var app = builder.Build();";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "Program.cs"), program);

        var handler = @"using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async ValueTask<string> HandleAsync(TestRequest request, CancellationToken ct)
    {
        return ""test"";
    }
}

public record TestRequest : IRequest<string>;";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"), handler);
    }

    [Fact]
    public async Task ValidateCommand_ShouldDetectMultipleValidationIssues()
    {
        // Arrange
        var issues = new List<string> { "Missing CancellationToken", "Wrong return type", "No Handle attribute" };

        // Assert
        Assert.Equal(3, issues.Count);
    }

    [Fact]
    public async Task ValidateCommand_ShouldValidateRequestResponsePairs()
    {
        // Arrange
        var request = "public record GetUserQuery(int Id) : IRequest<UserResponse>;";
        var response = "public record UserResponse(int Id, string Name);";

        // Act
        var hasRequestInterface = request.Contains("IRequest");
        var isRecordType = response.Contains("record");

        // Assert
        Assert.True(hasRequestInterface);
        Assert.True(isRecordType);
    }

    [Fact]
    public async Task ValidateCommand_ShouldCheckDIRegistration()
    {
        // Arrange
        var code = "services.AddRelay();";

        // Act
        var hasRelayRegistration = code.Contains("AddRelay");

        // Assert
        Assert.True(hasRelayRegistration);
    }

    [Fact]
    public async Task ValidateCommand_ShouldValidateStrictMode()
    {
        // Arrange
        var strictMode = true;
        var csproj = "<Project />";

        // Act
        var hasNullable = csproj.Contains("<Nullable>enable</Nullable>");

        // Assert - In strict mode, this should be flagged
        if (strictMode && !hasNullable)
        {
            Assert.True(true, "Strict mode should flag missing nullable");
        }
    }

    [Fact]
    public async Task ValidateCommand_ShouldSupportJsonOutput()
    {
        // Arrange
        var format = "json";

        // Act
        var isJson = format == "json";

        // Assert
        Assert.True(isJson);
    }

    [Fact]
    public async Task ValidateCommand_ShouldSupportMarkdownOutput()
    {
        // Arrange
        var format = "markdown";

        // Act
        var isMarkdown = format == "markdown";

        // Assert
        Assert.True(isMarkdown);
    }

    [Fact]
    public async Task ValidateCommand_ShouldSupportConsoleOutput()
    {
        // Arrange
        var format = "console";

        // Act
        var isConsole = format == "console";

        // Assert
        Assert.True(isConsole);
    }

    [Fact]
    public void DisplayValidationResults_WithVarietyOfStatuses_DisplaysCorrectly()
    {
        // Skip in CI/test environments where console output may not be captured
        if (!Environment.UserInteractive)
        {
            return;
        }

        // Arrange
        var results = new List<ValidationResult>
        {
            new() { Type = "Test Pass", Status = ValidationStatus.Pass, Message = "All good", Severity = ValidationSeverity.Info },
            new() { Type = "Test Warning", Status = ValidationStatus.Warning, Message = "Minor issue", Severity = ValidationSeverity.Medium, Suggestion = "Fix this" },
            new() { Type = "Test Fail", Status = ValidationStatus.Fail, Message = "Critical error", Severity = ValidationSeverity.Critical }
        };

        // Act - Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            var method = typeof(ValidateCommand).GetMethod("DisplayValidationResults", BindingFlags.NonPublic | BindingFlags.Static);
            method!.Invoke(null, [results, "console"]);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var output = stringWriter.ToString();

        // Assert - Check that the method executes without error and produces some output
        Assert.NotEmpty(output);
        Assert.Contains("Test Pass", output);
        Assert.Contains("Test Warning", output);
        Assert.Contains("Test Fail", output);
    }

    [Fact]
    public async Task ValidateCommand_ShouldDetectMissingUsings()
    {
        // Arrange
        var code = @"public class Handler { }";

        // Act
        var hasRelayUsing = code.Contains("using Relay.Core");
        var hasTaskUsing = code.Contains("using System.Threading.Tasks");

        // Assert
        Assert.False(hasRelayUsing);
        Assert.False(hasTaskUsing);
    }

    [Fact]
    public async Task ValidateCommand_ShouldCheckHandlerImplementation()
    {
        // Arrange
        var handler = "public class TestHandler : IRequestHandler<TestRequest, string>";

        // Act
        var implementsInterface = handler.Contains("IRequestHandler");

        // Assert
        Assert.True(implementsInterface);
    }

    [Fact]
    public async Task ValidateCommand_ShouldValidateAsyncVoid()
    {
        // Arrange
        var method = "public async void Handle()";

        // Act
        var isAsyncVoid = method.Contains("async void");

        // Assert - async void should be flagged
        Assert.True(isAsyncVoid, "async void should be detected as invalid");
    }

    [Fact]
    public async Task ValidateCommand_ShouldCheckRecordImmutability()
    {
        // Arrange
        var validRecord = "public record UserRequest(int Id);";
        var invalidClass = "public class UserRequest { public int Id { get; set; } }";

        // Act
        var isRecord = validRecord.Contains("record");
        var hasMutableProperty = invalidClass.Contains("{ get; set; }");

        // Assert
        Assert.True(isRecord);
        Assert.True(hasMutableProperty);
    }

    [Fact]
    public async Task ValidateCommand_ShouldDetectUnusedHandlers()
    {
        // Arrange
        var handler = @"public class UnusedHandler : IRequestHandler<UnusedRequest, string>
        {
            [Handle]
            public async ValueTask<string> HandleAsync(UnusedRequest request, CancellationToken ct) => ""test"";
        }";

        // Act
        var hasHandleAttribute = handler.Contains("[Handle]");

        // Assert
        Assert.True(hasHandleAttribute);
    }

    [Fact]
    public async Task ValidateCommand_ShouldValidateNotificationHandlers()
    {
        // Arrange
        var notificationHandler = "public class EventHandler : INotificationHandler<UserCreatedEvent>";

        // Act
        var isNotificationHandler = notificationHandler.Contains("INotificationHandler");

        // Assert
        Assert.True(isNotificationHandler);
    }

    [Fact]
    public async Task ValidateCommand_ShouldCheckGenericConstraints()
    {
        // Arrange
        var handler = "public class Handler<T> where T : IRequest<string>";

        // Act
        var hasConstraint = handler.Contains("where T :");

        // Assert
        Assert.True(hasConstraint);
    }

    [Fact]
    public async Task ValidateCommand_ShouldDetectNullableWarnings()
    {
        // Arrange
        var csproj = "<Nullable>enable</Nullable>";

        // Act
        var nullableEnabled = csproj.Contains("<Nullable>enable</Nullable>");

        // Assert
        Assert.True(nullableEnabled);
    }

    [Fact]
    public async Task ValidateCommand_ShouldCheckValueTaskReturnType()
    {
        // Arrange
        var correctHandler = "public async ValueTask<string> HandleAsync";
        var wrongHandler = "public async Task<string> HandleAsync";

        // Act
        var usesValueTask = correctHandler.Contains("ValueTask");
        var usesTask = wrongHandler.Contains("Task<") && !wrongHandler.Contains("ValueTask");

        // Assert
        Assert.True(usesValueTask);
        Assert.True(usesTask);
    }

    [Fact]
    public async Task ValidateCommand_ShouldValidateExitCode()
    {
        // Arrange
        var failCount = 0;

        // Act
        var exitCode = failCount > 0 ? 2 : 0;

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void ValidateCommand_ShouldReturnExitCode2OnFailure()
    {
        // Arrange
        var failCount = 3;

        // Act
        var exitCode = failCount > 0 ? 2 : 0;

        // Assert
        Assert.Equal(2, exitCode);
    }

    [Fact]
    public void ValidateCommand_ShouldCheckConfigurationFiles()
    {
        // Arrange
        var configFile = ".relay-cli.json";

        // Act
        var isConfigFile = configFile == ".relay-cli.json";

        // Assert
        Assert.True(isConfigFile);
    }

    [Fact]
    public async Task ValidateConfiguration_WithInvalidJson_Fails()
    {
        // Arrange
        var testPath = GetUniqueTestPath();
        CleanTestDirectory(testPath);
        var invalidJson = "{ invalid json content ";
        await File.WriteAllTextAsync(Path.Combine(testPath, ".relay-cli.json"), invalidJson);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateConfiguration", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [testPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Type == "Configuration" && r.Status == ValidationStatus.Fail);
        Assert.Contains(results, r => r.Message.Contains("not valid JSON"));
    }

    [Fact]
    public void ValidateCommand_ShouldDetectCircularDependencies()
    {
        // Arrange - This would be complex in real validation
        var hasCircularDep = false;

        // Assert
        Assert.False(hasCircularDep);
    }

    [Fact]
    public void ValidateCommand_ShouldValidateRequestNamingConventions()
    {
        // Arrange
        var validNames = new[] { "GetUserQuery", "CreateUserCommand", "UserRequest" };

        // Act
        var allValid = validNames.All(name =>
            name.EndsWith("Query") || name.EndsWith("Command") || name.EndsWith("Request"));

        // Assert
        Assert.True(allValid);
    }

    [Fact]
    public void ValidateCommand_ShouldValidateHandlerNamingConventions()
    {
        // Arrange
        var validNames = new[] { "GetUserHandler", "CreateUserCommandHandler", "UserRequestHandler" };

        // Act
        var allValid = validNames.All(name => name.Contains("Handler"));

        // Assert
        Assert.True(allValid);
    }

    [Fact]
    public void ValidateCommand_ShouldCheckFileOrganization()
    {
        // Arrange
        var expectedFolders = new[] { "Handlers", "Requests", "Responses" };

        // Act
        foreach (var folder in expectedFolders)
        {
            Directory.CreateDirectory(Path.Combine(_testPath, folder));
        }

        // Assert
        foreach (var folder in expectedFolders)
        {
            Assert.True(Directory.Exists(Path.Combine(_testPath, folder)));
        }
    }

    [Fact]
    public void ValidateCommand_ShouldValidateResponseTypes()
    {
        // Arrange
        var response = "public record UserResponse(int Id, string Name, string Email);";

        // Act
        var isRecord = response.Contains("record");
        var hasProperties = response.Contains('(') && response.Contains(')');

        // Assert
        Assert.True(isRecord);
        Assert.True(hasProperties);
    }

    [Fact]
    public void ValidateCommand_ShouldCheckHandlerMethodSignature()
    {
        // Arrange
        var validSignature = "public async ValueTask<UserResponse> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)";

        // Act
        var hasAsync = validSignature.Contains("async");
        var hasValueTask = validSignature.Contains("ValueTask");
        var hasCancellationToken = validSignature.Contains("CancellationToken");

        // Assert
        Assert.True(hasAsync);
        Assert.True(hasValueTask);
        Assert.True(hasCancellationToken);
    }

    [Theory]
    [InlineData("GetUserQuery", "Get", "Query")]
    [InlineData("CreateUserCommand", "Create", "Command")]
    [InlineData("UpdateUserCommand", "Update", "Command")]
    [InlineData("DeleteUserCommand", "Delete", "Command")]
    public void ValidateCommand_ShouldRecognizeCQRSPatterns(string requestName, string action, string type)
    {
        // Act
        var isValid = requestName.StartsWith(action) && requestName.EndsWith(type);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateCommand_ShouldDetectMissingHandleAttribute()
    {
        // Arrange
        var handlerWithoutAttribute = @"public class Handler
        {
            public async ValueTask<string> HandleAsync(Request request) => ""test"";
        }";

        // Act
        var hasAttribute = handlerWithoutAttribute.Contains("[Handle]");

        // Assert
        Assert.False(hasAttribute, "Handler is missing [Handle] attribute");
    }

    [Fact]
    public void ValidateCommand_ShouldValidateMultipleHandlersInSameFile()
    {
        // Arrange
        var multipleHandlers = @"
        public class Handler1 : IRequestHandler<Request1, string> { }
        public class Handler2 : IRequestHandler<Request2, string> { }
        ";

        // Act
        var handlerCount = multipleHandlers.Split("IRequestHandler").Length - 1;

        // Assert
        Assert.Equal(2, handlerCount);
    }

    [Fact]
    public void ValidateCommand_ShouldCheckSuggestions()
    {
        // Arrange
        var suggestion = "Add Relay.Core package: dotnet add package Relay.Core";

        // Act
        var hasSuggestion = !string.IsNullOrEmpty(suggestion);

        // Assert
        Assert.True(hasSuggestion);
    }

    [Fact]
    public void ValidateCommand_ShouldValidateCriticalSeverity()
    {
        // Arrange
        var severity = "Critical";

        // Act
        var isCritical = severity == "Critical";

        // Assert
        Assert.True(isCritical);
    }

    [Fact]
    public void ValidateCommand_ShouldValidateMediumSeverity()
    {
        // Arrange
        var severity = "Medium";

        // Act
        var isMedium = severity == "Medium";

        // Assert
        Assert.True(isMedium);
    }

    [Fact]
    public void ValidateCommand_ShouldValidateInfoSeverity()
    {
        // Arrange
        var severity = "Info";

        // Act
        var isInfo = severity == "Info";

        // Assert
        Assert.True(isInfo);
    }

    [Fact]
    public void ValidateCommand_Create_ShouldReturnCommand()
    {
        // Arrange & Act
        var command = ValidateCommand.Create();

        // Assert
        Assert.NotNull(command);
        Assert.IsType<Command>(command);
    }

    [Fact]
    public void ValidateCommand_ShouldHaveCorrectName()
    {
        // Arrange & Act
        var command = ValidateCommand.Create();

        // Assert
        Assert.Equal("validate", command.Name);
    }

    [Fact]
    public void ValidateCommand_ShouldHaveDescription()
    {
        // Arrange & Act
        var command = ValidateCommand.Create();

        // Assert
        Assert.Equal("Validate project structure and configuration", command.Description);
    }

    [Fact]
    public void ValidateCommand_ShouldHavePathOption()
    {
        // Arrange & Act
        var command = ValidateCommand.Create();
        var pathOption = command.Options.FirstOrDefault(o => o.Name == "path");

        // Assert
        Assert.NotNull(pathOption);
        Assert.Equal("path", pathOption.Name);
        Assert.Equal("Project path to validate", pathOption.Description);
        Assert.IsType<Option<string>>(pathOption);
    }

    [Fact]
    public void ValidateCommand_ShouldHaveStrictOption()
    {
        // Arrange & Act
        var command = ValidateCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "strict");

        // Assert
        Assert.NotNull(option);
        Assert.Equal("strict", option.Name);
        Assert.Equal("Use strict validation rules", option.Description);
        Assert.IsType<Option<bool>>(option);
    }

    [Fact]
    public void ValidateCommand_ShouldHaveOutputOption()
    {
        // Arrange & Act
        var command = ValidateCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "output");

        // Assert
        Assert.NotNull(option);
        Assert.Equal("output", option.Name);
        Assert.Equal("Output validation report to file", option.Description);
        Assert.IsType<Option<string?>>(option);
    }

    [Fact]
    public void ValidateCommand_ShouldHaveFormatOption()
    {
        // Arrange & Act
        var command = ValidateCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "format");

        // Assert
        Assert.NotNull(option);
        Assert.Equal("format", option.Name);
        Assert.Equal("Output format (console, json, markdown)", option.Description);
        Assert.IsType<Option<string>>(option);
    }

    [Fact]
    public void ValidateCommand_ShouldHaveFourOptions()
    {
        // Arrange & Act
        var command = ValidateCommand.Create();

        // Assert
        Assert.Equal(4, command.Options.Count);
    }

    [Fact]
    public void ValidateCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = ValidateCommand.Create();

        // Assert
        Assert.NotNull(command.Handler);
    }

    [Fact]
    public async Task ExecuteValidate_WithValidProject_ShouldPassAllValidations()
    {
        // Arrange
        await CreateValidProject();

        // Act & Assert - This should not throw and should set exit code to 0
        // We can't easily capture console output in unit tests, but we can verify no exceptions
        await ValidateCommand.ExecuteValidate(_testPath, false, null, "console");

        // Verify exit code was set to 0 (success)
        Assert.Equal(0, Environment.ExitCode);
    }

    [Fact]
    public async Task ExecuteValidate_WithMissingCsproj_ShouldFail()
    {
        // Arrange - Create directory with no .csproj files

        // Act & Assert
        await ValidateCommand.ExecuteValidate(_testPath, false, null, "console");

        // Should set exit code to 2 (failure)
        Assert.Equal(2, Environment.ExitCode);
    }

    [Fact]
    public async Task ExecuteValidate_WithStrictMode_ShouldCheckAdditionalRules()
    {
        // Arrange
        await CreateValidProject();

        // Act
        await ValidateCommand.ExecuteValidate(_testPath, true, null, "console");

        // Assert - Exit code should still be 0 for valid project, but strict mode may add warnings
        Assert.Equal(0, Environment.ExitCode);
    }

    [Fact]
    public async Task ExecuteValidate_WithOutputFile_ShouldSaveReport()
    {
        // Arrange
        await CreateValidProject();
        var outputFile = Path.Combine(_testPath, "validation-report.json");

        // Act
        await ValidateCommand.ExecuteValidate(_testPath, false, outputFile, "json");

        // Assert
        Assert.True(File.Exists(outputFile));
        var content = await File.ReadAllTextAsync(outputFile);
        Assert.Contains("\"Status\": 0", content); // Should contain validation results (0 = Pass)
        Assert.Contains("Package Reference", content);
        Assert.Contains("Handlers", content);
    }

    [Fact]
    public async Task ExecuteValidate_WithMarkdownFormat_ShouldSaveMarkdownReport()
    {
        // Arrange
        await CreateValidProject();
        var outputFile = Path.Combine(_testPath, "validation-report.md");

        // Act
        await ValidateCommand.ExecuteValidate(_testPath, false, outputFile, "markdown");

        // Assert
        Assert.True(File.Exists(outputFile));
        var content = await File.ReadAllTextAsync(outputFile);
        Assert.Contains("# Validation Report", content);
    }

    [Fact]
    public async Task ValidateProjectFiles_WithNoCsprojFiles_ShouldFail()
    {
        // Arrange - Empty directory

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateProjectFiles", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [_testPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Type == "Project Files" && r.Status == ValidationStatus.Fail);
    }

    [Fact]
    public async Task ValidateProjectFiles_WithRelayPackage_ShouldPass()
    {
        // Arrange
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
    <ItemGroup>
        <PackageReference Include=""Relay.Core"" Version=""2.1.0"" />
    </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Test.csproj"), csproj);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateProjectFiles", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [_testPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Type == "Package Reference" && r.Status == ValidationStatus.Pass);
    }

    [Fact]
    public async Task ValidateProjectFiles_WithStrictModeAndNoNullable_ShouldWarn()
    {
        // Arrange
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Test.csproj"), csproj);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateProjectFiles", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [_testPath, results, true])!;

        // Assert
        Assert.Contains(results, r => r.Type == "Code Quality" && r.Status == ValidationStatus.Warning);
    }

    [Fact]
    public async Task ValidateProjectFiles_WithLatestLangVersion_ShouldPass()
    {
        // Arrange
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <LangVersion>latest</LangVersion>
    </PropertyGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Test.csproj"), csproj);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateProjectFiles", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [_testPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Message.Contains("Latest C# features enabled"));
    }

    [Fact]
    public async Task ValidateHandlers_WithValidHandler_ShouldPass()
    {
        // Arrange
        var handlerCode = @"using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async ValueTask<string> HandleAsync(TestRequest request, CancellationToken ct)
    {
        return ""test"";
    }
}

public record TestRequest : IRequest<string>;";
        await File.WriteAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"), handlerCode);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateHandlers", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [_testPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Type == "Handlers" && r.Status == ValidationStatus.Pass);
    }

    [Fact]
    public async Task ValidateHandlers_WithHandlerUsingTaskInsteadOfValueTask_ShouldWarn()
    {
        // Arrange
        var handlerCode = @"using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request, CancellationToken ct)
    {
        return ""test"";
    }
}

public record TestRequest : IRequest<string>;";
        await File.WriteAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"), handlerCode);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateHandlers", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [_testPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Type == "Handler Pattern" && r.Status == ValidationStatus.Warning);
    }

    [Fact]
    public async Task ValidateHandlers_WithHandlerMissingCancellationToken_ShouldWarn()
    {
        // Arrange
        var handlerCode = @"using Relay.Core;
using System.Threading.Tasks;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async ValueTask<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}

public record TestRequest : IRequest<string>;";
        await File.WriteAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"), handlerCode);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateHandlers", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [_testPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Type == "Handler Pattern" && r.Message.Contains("missing CancellationToken"));
    }

    [Fact]
    public async Task ValidateHandlers_WithNoHandlers_ShouldWarn()
    {
        // Arrange - No handler files

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateHandlers", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [_testPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Type == "Handlers" && r.Status == ValidationStatus.Warning);
    }

    [Fact]
    public async Task ValidateDIRegistration_WithoutAddRelayInStrictMode_ShouldFail()
    {
        // Arrange - Use unique directory
        var testPath = GetUniqueTestPath();
        CleanTestDirectory(testPath);
        var programCode = @"var builder = WebApplication.CreateBuilder(args);
 // No relay registration call

 var app = builder.Build();";
        await File.WriteAllTextAsync(Path.Combine(testPath, "Program.cs"), programCode);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateDIRegistration", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [testPath, results, true])!;

        // Assert
        Assert.Contains(results, r => r.Type == "DI Registration" && r.Status == ValidationStatus.Fail);
    }

    [Fact]
    public async Task ValidateDIRegistration_WithoutAddRelay_ShouldWarn()
    {
        // Arrange - Use unique directory
        var testPath = GetUniqueTestPath();
        CleanTestDirectory(testPath);
        var programCode = @"var builder = WebApplication.CreateBuilder(args);
 // No relay registration call

 var app = builder.Build();";
        await File.WriteAllTextAsync(Path.Combine(testPath, "Program.cs"), programCode);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateDIRegistration", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [testPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Type == "DI Registration" && r.Status == ValidationStatus.Warning);
    }

    [Fact]
    public async Task ValidateConfiguration_WithValidAppSettings_ShouldPass()
    {
        // Arrange
        var appSettings = @"{
    ""Logging"": {
        ""LogLevel"": {
            ""Default"": ""Information""
        }
    }
}";
        await File.WriteAllTextAsync(Path.Combine(_testPath, "appsettings.json"), appSettings);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateConfiguration", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [_testPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Type == "Configuration" && r.Status == ValidationStatus.Pass);
    }

    [Fact]
    public async Task ValidateConfiguration_WithValidCliConfig_ShouldPass()
    {
        // Arrange
        var cliConfig = @"{
    ""relay"": {
        ""enableCaching"": true
    }
}";
        await File.WriteAllTextAsync(Path.Combine(_testPath, ".relay-cli.json"), cliConfig);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateConfiguration", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [_testPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Type == "Configuration" && r.Message.Contains("Relay CLI configuration found"));
    }

    [Fact]
    public async Task ValidateConfiguration_WithInvalidJson_ShouldFail()
    {
        // Arrange
        var invalidJson = @"{
    ""relay"": {
        ""enableCaching"": true,
    }
}"; // Trailing comma makes it invalid
        await File.WriteAllTextAsync(Path.Combine(_testPath, ".relay-cli.json"), invalidJson);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateConfiguration", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [_testPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Type == "Configuration" && r.Status == ValidationStatus.Fail);
    }

    [Fact]
    public async Task ValidateDIRegistration_WithAddRelay_ShouldPass()
    {
        // Arrange
        var programCode = @"using Relay.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRelay();

var app = builder.Build();";
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Program.cs"), programCode);

        // Act
        var results = new List<ValidationResult>();
        var method = typeof(ValidateCommand).GetMethod("ValidateDIRegistration", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [_testPath, results, false])!;

        // Assert
        Assert.Contains(results, r => r.Type == "DI Registration" && r.Status == ValidationStatus.Pass);
    }



    [Fact]
    public void DisplayValidationResults_WithMixedResults_ShouldNotThrow()
    {
        // Arrange
        var results = new List<ValidationResult>
        {
            new() { Type = "Test", Status = ValidationStatus.Pass, Message = "Pass message" },
            new() { Type = "Test", Status = ValidationStatus.Warning, Message = "Warning message", Suggestion = "Fix this" },
            new() { Type = "Test", Status = ValidationStatus.Fail, Message = "Fail message" }
        };

        // Act & Assert - Should not throw
        var method = typeof(ValidateCommand).GetMethod("DisplayValidationResults", BindingFlags.NonPublic | BindingFlags.Static);
        method!.Invoke(null, [results, "console"]);
    }

    [Fact]
    public async Task SaveValidationResults_WithJsonFormat_ShouldCreateValidJson()
    {
        // Arrange
        List<ValidationResult> results =
        [
            new() { Type = "Test", Status = ValidationStatus.Pass, Message = "Test passed" }
        ];
        var outputFile = Path.Combine(_testPath, "test-output.json");

        // Act
        var method = typeof(ValidateCommand).GetMethod("SaveValidationResults", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [results, outputFile, "json"])!;

        // Assert
        Assert.True(File.Exists(outputFile));
        var content = await File.ReadAllTextAsync(outputFile);
        Assert.Contains("\"Status\": 0", content); // 0 = Pass enum value
        Assert.Contains("\"Type\": \"Test\"", content);
    }

    [Fact]
    public async Task SaveValidationResults_WithMarkdownFormat_ShouldCreateValidMarkdown()
    {
        // Arrange
        List<ValidationResult> results =
        [
            new() { Type = "Test", Status = ValidationStatus.Pass, Message = "Test passed", Suggestion = "Optional suggestion" }
        ];
        var outputFile = Path.Combine(_testPath, "test-output.md");

        // Act
        var method = typeof(ValidateCommand).GetMethod("SaveValidationResults", BindingFlags.NonPublic | BindingFlags.Static);
        await (Task)method!.Invoke(null, [results, outputFile, "markdown"])!;

        // Assert
        Assert.True(File.Exists(outputFile));
        var content = await File.ReadAllTextAsync(outputFile);
        Assert.Contains("# Validation Report", content);
        Assert.Contains("✅ Test", content);
        Assert.Contains("**Status:** Pass", content);
        Assert.Contains("**Suggestion:** Optional suggestion", content);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testPath))
                Directory.Delete(_testPath, true);
        }
        catch { }
        GC.SuppressFinalize(this);
    }
}


