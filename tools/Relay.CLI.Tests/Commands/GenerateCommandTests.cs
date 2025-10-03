using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class GenerateCommandTests : IDisposable
{
    private readonly string _testPath;

    public GenerateCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-generate-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    [Fact]
    public async Task GenerateCommand_CreatesHandlerFile()
    {
        // Arrange
        var handlerName = "TestHandler";
        var handlerContent = GenerateHandlerContent(handlerName);

        // Act
        var filePath = Path.Combine(_testPath, $"{handlerName}.cs");
        await File.WriteAllTextAsync(filePath, handlerContent);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain(handlerName);
        content.Should().Contain("[Handle]");
    }

    [Fact]
    public async Task GenerateCommand_CreatesRequestFile()
    {
        // Arrange
        var requestName = "TestRequest";
        var requestContent = GenerateRequestContent(requestName);

        // Act
        var filePath = Path.Combine(_testPath, $"{requestName}.cs");
        await File.WriteAllTextAsync(filePath, requestContent);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain(requestName);
        content.Should().Contain("IRequest");
    }

    [Fact]
    public async Task GenerateCommand_CreatesCommandPattern()
    {
        // Arrange
        var commandName = "CreateUserCommand";
        var handlerName = "CreateUserCommandHandler";

        // Act
        await CreateCommandPattern(commandName, handlerName);

        // Assert
        File.Exists(Path.Combine(_testPath, $"{commandName}.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_testPath, $"{handlerName}.cs")).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateCommand_CreatesQueryPattern()
    {
        // Arrange
        var queryName = "GetUserQuery";
        var handlerName = "GetUserQueryHandler";

        // Act
        await CreateQueryPattern(queryName, handlerName);

        // Assert
        File.Exists(Path.Combine(_testPath, $"{queryName}.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_testPath, $"{handlerName}.cs")).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateCommand_IncludesProperUsings()
    {
        // Arrange & Act
        var handlerContent = GenerateHandlerContent("TestHandler");

        // Assert
        handlerContent.Should().Contain("using Relay.Core;");
        handlerContent.Should().Contain("using System.Threading;");
        handlerContent.Should().Contain("using System.Threading.Tasks;");
    }

    [Fact]
    public async Task GenerateCommand_UsesValueTaskReturnType()
    {
        // Arrange & Act
        var handlerContent = GenerateHandlerContent("TestHandler");

        // Assert
        handlerContent.Should().Contain("ValueTask");
        handlerContent.Should().NotContain("Task<");
    }

    [Fact]
    public async Task GenerateCommand_IncludesCancellationToken()
    {
        // Arrange & Act
        var handlerContent = GenerateHandlerContent("TestHandler");

        // Assert
        handlerContent.Should().Contain("CancellationToken");
    }

    [Fact]
    public async Task GenerateCommand_UsesHandleAttribute()
    {
        // Arrange & Act
        var handlerContent = GenerateHandlerContent("TestHandler");

        // Assert
        handlerContent.Should().Contain("[Handle]");
    }

    [Fact]
    public async Task GenerateCommand_FollowsNamingConventions()
    {
        // Arrange
        var names = new[]
        {
            "GetUserQuery",
            "CreateUserCommand",
            "UpdateUserCommand",
            "DeleteUserCommand"
        };

        // Act & Assert
        foreach (var name in names)
        {
            name.Should().MatchRegex("^[A-Z][a-zA-Z0-9]+(Query|Command)$");
        }
    }

    [Theory]
    [InlineData("GetUser", "Query", "GetUserQuery")]
    [InlineData("CreateUser", "Command", "CreateUserCommand")]
    [InlineData("UpdateUser", "Command", "UpdateUserCommand")]
    [InlineData("DeleteUser", "Command", "DeleteUserCommand")]
    public void GenerateCommand_AppendsCorrectSuffix(string baseName, string pattern, string expected)
    {
        // Act
        var result = $"{baseName}{pattern}";

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task GenerateCommand_CreatesRecordType()
    {
        // Arrange
        var requestName = "TestRequest";
        var requestContent = GenerateRequestContent(requestName, isRecord: true);

        // Act
        await File.WriteAllTextAsync(Path.Combine(_testPath, $"{requestName}.cs"), requestContent);
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, $"{requestName}.cs"));

        // Assert
        content.Should().Contain("public record");
    }

    [Fact]
    public async Task GenerateCommand_CreatesClassType()
    {
        // Arrange
        var requestName = "TestRequest";
        var requestContent = GenerateRequestContent(requestName, isRecord: false);

        // Act
        await File.WriteAllTextAsync(Path.Combine(_testPath, $"{requestName}.cs"), requestContent);
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, $"{requestName}.cs"));

        // Assert
        content.Should().Contain("public class");
    }

    [Fact]
    public async Task GenerateCommand_SupportsGenericResponseTypes()
    {
        // Arrange
        var handlerContent = GenerateHandlerContent("TestHandler", responseType: "TestResponse");

        // Act
        await File.WriteAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"), handlerContent);
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"));

        // Assert
        content.Should().Contain("IRequest<TestResponse>");
        content.Should().Contain("ValueTask<TestResponse>");
    }

    [Fact]
    public async Task GenerateCommand_SupportsVoidHandlers()
    {
        // Arrange
        var handlerContent = GenerateHandlerContent("TestHandler", responseType: null);

        // Act
        await File.WriteAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"), handlerContent);
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"));

        // Assert
        content.Should().Contain("IRequest");
        content.Should().Contain("ValueTask");
    }

    [Fact]
    public async Task GenerateCommand_CreatesWithNamespace()
    {
        // Arrange
        var namespaceName = "MyApp.Features.Users";
        var handlerContent = GenerateHandlerContent("TestHandler", namespaceName: namespaceName);

        // Act
        await File.WriteAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"), handlerContent);
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"));

        // Assert
        content.Should().Contain($"namespace {namespaceName}");
    }

    [Fact]
    public async Task GenerateCommand_SupportsFileOverwrite()
    {
        // Arrange
        var filePath = Path.Combine(_testPath, "TestHandler.cs");
        await File.WriteAllTextAsync(filePath, "old content");

        // Act
        var newContent = GenerateHandlerContent("TestHandler");
        await File.WriteAllTextAsync(filePath, newContent);
        var content = await File.ReadAllTextAsync(filePath);

        // Assert
        content.Should().NotContain("old content");
        content.Should().Contain("TestHandler");
    }

    private string GenerateHandlerContent(
        string handlerName,
        string? responseType = "string",
        string? namespaceName = null)
    {
        var requestName = handlerName.Replace("Handler", "Request");
        var returnType = responseType != null ? $"ValueTask<{responseType}>" : "ValueTask";
        var requestType = responseType != null ? $"IRequest<{responseType}>" : "IRequest";

        var namespaceStart = namespaceName != null ? $"namespace {namespaceName};\n\n" : "";

        return $@"{namespaceStart}using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class {handlerName} : IRequestHandler<{requestName}, {responseType ?? "Unit"}>
{{
    [Handle]
    public async {returnType} HandleAsync({requestName} request, CancellationToken ct)
    {{
        {(responseType != null ? $"return default({responseType});" : "await Task.CompletedTask;")}
    }}
}}

public record {requestName} : {requestType};";
    }

    private string GenerateRequestContent(string requestName, bool isRecord = true)
    {
        var typeKeyword = isRecord ? "record" : "class";
        return $@"using Relay.Core;

public {typeKeyword} {requestName} : IRequest<string>;";
    }

    private async Task CreateCommandPattern(string commandName, string handlerName)
    {
        var commandContent = $@"using Relay.Core;

public record {commandName}(string Data) : IRequest<bool>;";

        var handlerContent = $@"using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class {handlerName} : IRequestHandler<{commandName}, bool>
{{
    [Handle]
    public async ValueTask<bool> HandleAsync({commandName} command, CancellationToken ct)
    {{
        // Implementation
        return true;
    }}
}}";

        await File.WriteAllTextAsync(Path.Combine(_testPath, $"{commandName}.cs"), commandContent);
        await File.WriteAllTextAsync(Path.Combine(_testPath, $"{handlerName}.cs"), handlerContent);
    }

    private async Task CreateQueryPattern(string queryName, string handlerName)
    {
        var queryContent = $@"using Relay.Core;

public record {queryName}(int Id) : IRequest<string>;";

        var handlerContent = $@"using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class {handlerName} : IRequestHandler<{queryName}, string>
{{
    [Handle]
    public async ValueTask<string> HandleAsync({queryName} query, CancellationToken ct)
    {{
        // Implementation
        return ""result"";
    }}
}}";

        await File.WriteAllTextAsync(Path.Combine(_testPath, $"{queryName}.cs"), queryContent);
        await File.WriteAllTextAsync(Path.Combine(_testPath, $"{handlerName}.cs"), handlerContent);
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
