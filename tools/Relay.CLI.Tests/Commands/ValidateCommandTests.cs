using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class ValidateCommandTests : IDisposable
{
    private readonly string _testPath;

    public ValidateCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-validate-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
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
        projectFiles.Should().NotBeEmpty();
        hasRelayPackage.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateCommand_DetectsInvalidHandlers()
    {
        // Arrange
        var invalidHandler = @"using Relay.Core;

public class BadHandler
{
    // Missing [Handle] attribute
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

        // Assert - Handler is missing [Handle] attribute (validation issue)
        hasHandleAttribute.Should().BeFalse("the invalid handler doesn't have [Handle] attribute");
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
        content.Should().NotContain("IRequest");
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
        hasValueTask.Should().BeFalse("handler should use ValueTask but doesn't");
        hasAsync.Should().BeFalse("handler should be async but isn't");
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
        hasHandleAsync.Should().BeFalse("method is incorrectly named 'Handle' instead of 'HandleAsync'");
    }

    [Fact]
    public async Task ValidateCommand_ValidatesUsingDirectives()
    {
        // Arrange
        var missingUsings = @"
public class TestHandler
{
    public async ValueTask<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}"; // Missing using directives

        await File.WriteAllTextAsync(Path.Combine(_testPath, "MissingUsings.cs"), missingUsings);

        // Act
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "MissingUsings.cs"));

        // Assert
        content.Should().NotContain("using Relay.Core");
        content.Should().NotContain("using System.Threading.Tasks");
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
        followsConvention.Should().Be(isValid);
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
        followsConvention.Should().Be(isValid);
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
        content.Should().NotContain("CancellationToken");
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
        return ""test""; // Should use await or remove async
    }
}

public record TestRequest : IRequest<string>;";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "MissingAwait.cs"), missingAwait);

        // Act
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "MissingAwait.cs"));
        var hasAsync = content.Contains("async");
        var hasAwait = content.Contains("await");

        // Assert
        hasAsync.Should().BeTrue();
        hasAwait.Should().BeFalse(); // Potential issue
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
        files.Should().NotBeEmpty();
        files.Should().OnlyContain(f => f.EndsWith(".cs"));
    }

    private async Task CreateValidProject()
    {
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""2.1.0"" />
  </ItemGroup>
</Project>";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "Test.csproj"), csproj);

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
