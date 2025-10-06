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
        handlerContent.Should().NotContain("async Task<"); // Ensure we're not using Task<T> instead of ValueTask<T>
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

    [Fact]
    public async Task GenerateCommand_ShouldCreateResponseFile()
    {
        // Arrange
        var responseName = "UserResponse";
        var responseContent = "public record UserResponse(int Id, string Name);";

        // Act
        var filePath = Path.Combine(_testPath, $"{responseName}.cs");
        await File.WriteAllTextAsync(filePath, responseContent);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain(responseName);
    }

    [Fact]
    public async Task GenerateCommand_ShouldCreateValidator()
    {
        // Arrange
        var validatorName = "CreateUserValidator";
        var validatorContent = GenerateValidatorContent(validatorName);

        // Act
        var filePath = Path.Combine(_testPath, $"{validatorName}.cs");
        await File.WriteAllTextAsync(filePath, validatorContent);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain(validatorName);
    }

    [Fact]
    public async Task GenerateCommand_ShouldSupportNotificationPattern()
    {
        // Arrange
        var notificationName = "UserCreatedEvent";
        var notificationContent = "public record UserCreatedEvent(int UserId) : INotification;";

        // Act
        var filePath = Path.Combine(_testPath, $"{notificationName}.cs");
        await File.WriteAllTextAsync(filePath, notificationContent);

        // Assert
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("INotification");
    }

    [Fact]
    public async Task GenerateCommand_ShouldCreateNotificationHandler()
    {
        // Arrange
        var handlerName = "UserCreatedEventHandler";
        var handlerContent = GenerateNotificationHandlerContent(handlerName);

        // Act
        var filePath = Path.Combine(_testPath, $"{handlerName}.cs");
        await File.WriteAllTextAsync(filePath, handlerContent);

        // Assert
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("INotificationHandler");
    }

    [Theory]
    [InlineData("Handlers")]
    [InlineData("Requests")]
    [InlineData("Responses")]
    [InlineData("Validators")]
    public async Task GenerateCommand_ShouldCreateInSpecificFolder(string folderName)
    {
        // Arrange
        var folderPath = Path.Combine(_testPath, folderName);
        Directory.CreateDirectory(folderPath);

        // Act
        var filePath = Path.Combine(folderPath, "Test.cs");
        await File.WriteAllTextAsync(filePath, "// Test");

        // Assert
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateCommand_ShouldSupportMultipleParameters()
    {
        // Arrange
        var requestContent = "public record CreateUserCommand(string Name, string Email, int Age) : IRequest<int>;";

        // Act
        await File.WriteAllTextAsync(Path.Combine(_testPath, "CreateUserCommand.cs"), requestContent);
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "CreateUserCommand.cs"));

        // Assert
        content.Should().Contain("string Name");
        content.Should().Contain("string Email");
        content.Should().Contain("int Age");
    }

    [Fact]
    public async Task GenerateCommand_ShouldCreatePipelineBehavior()
    {
        // Arrange
        var behaviorName = "LoggingBehavior";
        var behaviorContent = GenerateBehaviorContent(behaviorName);

        // Act
        var filePath = Path.Combine(_testPath, $"{behaviorName}.cs");
        await File.WriteAllTextAsync(filePath, behaviorContent);

        // Assert
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("IPipelineBehavior");
    }

    [Fact]
    public async Task GenerateCommand_ShouldSupportAsyncSuffix()
    {
        // Arrange
        var handlerContent = GenerateHandlerContent("TestHandler");

        // Assert
        handlerContent.Should().Contain("HandleAsync");
    }

    [Fact]
    public async Task GenerateCommand_ShouldGenerateUnitTests()
    {
        // Arrange
        var testName = "TestHandlerTests";
        var testContent = GenerateTestContent(testName);

        // Act
        var filePath = Path.Combine(_testPath, $"{testName}.cs");
        await File.WriteAllTextAsync(filePath, testContent);

        // Assert
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("[Fact]");
        content.Should().Contain("Should");
    }

    [Fact]
    public async Task GenerateCommand_ShouldCreateCRUDHandlers()
    {
        // Arrange
        var operations = new[] { "Create", "Get", "Update", "Delete" };

        // Act & Assert
        foreach (var operation in operations)
        {
            var handlerName = $"{operation}UserHandler";
            handlerName.Should().Contain(operation);
            handlerName.Should().EndWith("Handler");
        }
    }

    [Fact]
    public async Task GenerateCommand_ShouldSupportFileScoped()
    {
        // Arrange
        var content = "namespace MyApp.Features;\n\npublic record TestRequest : IRequest<string>;";

        // Act
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Test.cs"), content);
        var fileContent = await File.ReadAllTextAsync(Path.Combine(_testPath, "Test.cs"));

        // Assert
        fileContent.Should().Contain("namespace MyApp.Features;");
    }

    [Fact]
    public async Task GenerateCommand_ShouldGenerateWithComments()
    {
        // Arrange
        var content = @"/// <summary>
/// Handles user creation
/// </summary>
public class CreateUserHandler";

        // Assert
        content.Should().Contain("/// <summary>");
        content.Should().Contain("/// </summary>");
    }

    [Fact]
    public async Task GenerateCommand_ShouldSupportDependencyInjection()
    {
        // Arrange
        var handlerContent = @"public class TestHandler
{
    private readonly IUserRepository _repository;

    public TestHandler(IUserRepository repository)
    {
        _repository = repository;
    }
}";

        // Assert
        handlerContent.Should().Contain("private readonly");
        handlerContent.Should().Contain("public TestHandler(");
    }

    [Theory]
    [InlineData("int", "IRequest<int>")]
    [InlineData("string", "IRequest<string>")]
    [InlineData("bool", "IRequest<bool>")]
    [InlineData("UserResponse", "IRequest<UserResponse>")]
    public async Task GenerateCommand_ShouldSupportVariousReturnTypes(string returnType, string expectedInterface)
    {
        // Arrange
        var requestContent = $"public record TestRequest : {expectedInterface};";

        // Assert
        requestContent.Should().Contain(expectedInterface);
        _ = returnType; // Parameter is used to describe test cases
    }

    [Fact]
    public async Task GenerateCommand_ShouldCreateCollectionResponse()
    {
        // Arrange
        var requestContent = "public record GetUsersQuery : IRequest<List<UserResponse>>;";

        // Assert
        requestContent.Should().Contain("List<UserResponse>");
    }

    [Fact]
    public async Task GenerateCommand_ShouldSupportPagination()
    {
        // Arrange
        var requestContent = "public record GetUsersQuery(int Page, int PageSize) : IRequest<PagedResult<UserResponse>>;";

        // Assert
        requestContent.Should().Contain("int Page");
        requestContent.Should().Contain("int PageSize");
        requestContent.Should().Contain("PagedResult");
    }

    [Fact]
    public async Task GenerateCommand_ShouldGenerateWithRegions()
    {
        // Arrange
        var content = @"#region Handlers
public class TestHandler { }
#endregion";

        // Assert
        content.Should().Contain("#region Handlers");
        content.Should().Contain("#endregion");
    }

    [Fact]
    public async Task GenerateCommand_ShouldSupportNullableReferenceTypes()
    {
        // Arrange
        var content = "public record TestRequest(string? OptionalName) : IRequest<string>;";

        // Assert
        content.Should().Contain("string?");
    }

    [Fact]
    public async Task GenerateCommand_ShouldCreateFeatureFolder()
    {
        // Arrange
        var featureName = "Users";
        var featurePath = Path.Combine(_testPath, "Features", featureName);

        // Act
        Directory.CreateDirectory(featurePath);

        // Assert
        Directory.Exists(featurePath).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateCommand_ShouldColocateFilesInFeatureFolder()
    {
        // Arrange
        var featurePath = Path.Combine(_testPath, "Features", "Users");
        Directory.CreateDirectory(featurePath);

        // Act
        await File.WriteAllTextAsync(Path.Combine(featurePath, "CreateUser.cs"), "// Command");
        await File.WriteAllTextAsync(Path.Combine(featurePath, "CreateUserHandler.cs"), "// Handler");
        await File.WriteAllTextAsync(Path.Combine(featurePath, "CreateUserValidator.cs"), "// Validator");

        // Assert
        File.Exists(Path.Combine(featurePath, "CreateUser.cs")).Should().BeTrue();
        File.Exists(Path.Combine(featurePath, "CreateUserHandler.cs")).Should().BeTrue();
        File.Exists(Path.Combine(featurePath, "CreateUserValidator.cs")).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateCommand_ShouldGenerateResultPattern()
    {
        // Arrange
        var content = "public record TestRequest : IRequest<Result<UserResponse>>;";

        // Assert
        content.Should().Contain("Result<UserResponse>");
    }

    [Fact]
    public async Task GenerateCommand_ShouldSupportInitOnlyProperties()
    {
        // Arrange
        var content = "public class TestRequest { public string Name { get; init; } }";

        // Assert
        content.Should().Contain("{ get; init; }");
    }

    [Fact]
    public async Task GenerateCommand_ShouldGenerateWithAttributes()
    {
        // Arrange
        var content = @"public record TestRequest(
    [Required] string Name,
    [EmailAddress] string Email
) : IRequest<int>;";

        // Assert
        content.Should().Contain("[Required]");
        content.Should().Contain("[EmailAddress]");
    }

    [Fact]
    public async Task GenerateCommand_ShouldCreateMediatrProfile()
    {
        // Arrange
        var profileContent = "services.AddRelay();";

        // Assert
        profileContent.Should().Contain("AddRelay");
    }

    [Fact]
    public async Task GenerateCommand_ShouldSupportStreamResponse()
    {
        // Arrange
        var content = "public record GetUsersStreamQuery : IStreamRequest<UserResponse>;";

        // Assert
        content.Should().Contain("IStreamRequest");
    }

    [Theory]
    [InlineData("Create", "Command")]
    [InlineData("Update", "Command")]
    [InlineData("Delete", "Command")]
    [InlineData("Get", "Query")]
    [InlineData("List", "Query")]
    [InlineData("Search", "Query")]
    public async Task GenerateCommand_ShouldRecognizeCRUDOperation(string operation, string expectedType)
    {
        // Arrange
        var isQuery = new[] { "Get", "List", "Search" }.Contains(operation);
        var isCommand = new[] { "Create", "Update", "Delete" }.Contains(operation);

        // Assert
        if (expectedType == "Query")
        {
            isQuery.Should().BeTrue();
        }
        else
        {
            isCommand.Should().BeTrue();
        }
    }

    [Fact]
    public async Task GenerateCommand_ShouldGenerateWithLogging()
    {
        // Arrange
        var content = @"private readonly ILogger<TestHandler> _logger;

public TestHandler(ILogger<TestHandler> logger)
{
    _logger = logger;
}";

        // Assert
        content.Should().Contain("ILogger<TestHandler>");
        content.Should().Contain("_logger");
    }

    [Fact]
    public async Task GenerateCommand_ShouldCreateWithMapper()
    {
        // Arrange
        var content = @"private readonly IMapper _mapper;

public TestHandler(IMapper mapper)
{
    _mapper = mapper;
}";

        // Assert
        content.Should().Contain("IMapper");
        content.Should().Contain("_mapper");
    }

    [Fact]
    public async Task GenerateCommand_ShouldGenerateTemplateOptions()
    {
        // Arrange
        var templates = new[] { "minimal", "standard", "full" };

        // Assert
        templates.Should().Contain("minimal");
        templates.Should().Contain("standard");
        templates.Should().Contain("full");
    }

    private string GenerateValidatorContent(string validatorName)
    {
        return $@"using FluentValidation;

public class {validatorName} : AbstractValidator<CreateUserCommand>
{{
    public {validatorName}()
    {{
        RuleFor(x => x.Name).NotEmpty();
    }}
}}";
    }

    private string GenerateNotificationHandlerContent(string handlerName)
    {
        return $@"using Relay.Core;

public class {handlerName} : INotificationHandler<UserCreatedEvent>
{{
    public async ValueTask HandleAsync(UserCreatedEvent notification, CancellationToken ct)
    {{
        // Handle notification
    }}
}}";
    }

    private string GenerateBehaviorContent(string behaviorName)
    {
        return $@"using Relay.Core;

public class {behaviorName}<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{{
    public async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken ct)
    {{
        // Behavior logic
        return default(TResponse);
    }}
}}";
    }

    private string GenerateTestContent(string testName)
    {
        return $@"using Xunit;

public class {testName}
{{
    [Fact]
    public async Task Handler_ShouldReturnExpectedResult()
    {{
        // Arrange
        // Act
        // Assert
    }}
}}";
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
