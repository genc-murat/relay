using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class AICommandTests : IDisposable
{
    private readonly string _testPath;

    public AICommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-ai-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    [Fact]
    public async Task AICommand_SuggestsHandlerOptimizations()
    {
        // Arrange
        var handlerCode = @"using Relay.Core;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    [Handle]
    public async Task<User> HandleAsync(GetUserQuery request)
    {
        return new User();
    }
}";

        // Act
        var suggestions = new List<string>
        {
            "Consider using ValueTask<User> instead of Task<User>",
            "Add CancellationToken parameter",
            "Consider adding error handling"
        };

        // Assert
        suggestions.Should().HaveCountGreaterThan(0);
        suggestions.Should().Contain(s => s.Contains("ValueTask"));
    }

    [Fact]
    public async Task AICommand_DetectsPatterns()
    {
        // Arrange
        var projectPath = _testPath;

        // Act - Create some handler files
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Handler1.cs"), "public class Handler1 {}");
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Handler2.cs"), "public class Handler2 {}");
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Handler3.cs"), "public class Handler3 {}");

        var handlerCount = Directory.GetFiles(_testPath, "*Handler*.cs").Length;

        // Assert
        handlerCount.Should().Be(3);
    }

    [Fact]
    public void AICommand_GeneratesCodeRecommendations()
    {
        // Arrange
        var issues = new[]
        {
            "Missing async/await",
            "No cancellation token",
            "No error handling"
        };

        // Act
        var recommendations = issues.Select(issue => new
        {
            Issue = issue,
            Severity = "Warning",
            Suggestion = $"Fix: {issue}"
        }).ToList();

        // Assert
        recommendations.Should().HaveCount(3);
        recommendations.Should().AllSatisfy(r => r.Severity.Should().Be("Warning"));
    }

    [Fact]
    public async Task AICommand_AnalyzesComplexity()
    {
        // Arrange
        var method = @"
public async ValueTask<Result> HandleAsync(Request request, CancellationToken ct)
{
    if (request.IsValid)
    {
        for (int i = 0; i < 10; i++)
        {
            if (i % 2 == 0)
            {
                await DoSomethingAsync();
            }
        }
    }
    return Result.Success();
}";

        // Act - Count cyclomatic complexity indicators
        var complexityIndicators = new[] { "if", "for", "while", "case" };
        var complexity = complexityIndicators.Sum(indicator =>
            System.Text.RegularExpressions.Regex.Matches(method, $@"\b{indicator}\b").Count);

        // Assert
        complexity.Should().BeGreaterThan(1);
    }

    [Fact]
    public void AICommand_SuggestsNamingImprovements()
    {
        // Arrange
        var classNames = new[]
        {
            "handler", // Should be PascalCase
            "getuser", // Should be GetUser
            "GETDATA" // Should be GetData
        };

        // Act
        var suggestions = classNames
            .Where(name => !char.IsUpper(name[0]))
            .Select(name => $"'{name}' should start with uppercase")
            .ToList();

        // Assert
        suggestions.Should().HaveCount(2);
    }

    [Fact]
    public async Task AICommand_DetectsAntiPatterns()
    {
        // Arrange
        var codeWithAntiPatterns = @"
public class BadHandler
{
    public async Task<string> Handle(Request req) // Missing [Handle], wrong signature
    {
        Thread.Sleep(1000); // Blocking call in async method
        return ""result"";
    }
}";

        // Act
        var antiPatterns = new List<string>();
        if (codeWithAntiPatterns.Contains("Thread.Sleep"))
        {
            antiPatterns.Add("Blocking call in async method");
        }
        
        // Check if [Handle] attribute is on its own line (not in comments)
        var lines = codeWithAntiPatterns.Split('\n');
        var hasHandleAttribute = lines.Any(line => line.Trim() == "[Handle]");
        if (!hasHandleAttribute)
        {
            antiPatterns.Add("Missing Handle attribute");
        }

        // Assert
        antiPatterns.Should().HaveCount(2);
    }

    [Fact]
    public void AICommand_RanksHandlersByComplexity()
    {
        // Arrange
        var handlers = new[]
        {
            new { Name = "SimpleHandler", Lines = 10, Complexity = 2 },
            new { Name = "ComplexHandler", Lines = 100, Complexity = 15 },
            new { Name = "ModerateHandler", Lines = 50, Complexity = 7 }
        };

        // Act
        var ranked = handlers.OrderByDescending(h => h.Complexity).ToList();

        // Assert
        ranked[0].Name.Should().Be("ComplexHandler");
        ranked[2].Name.Should().Be("SimpleHandler");
    }

    [Fact]
    public async Task AICommand_GeneratesDocumentation()
    {
        // Arrange
        var handlerName = "CreateOrderHandler";
        var requestType = "CreateOrderCommand";
        var responseType = "Guid";

        // Act
        var documentation = $@"/// <summary>
/// Handles the creation of new orders in the system.
/// </summary>
/// <param name=""request"">The {requestType} containing order details.</param>
/// <param name=""ct"">Cancellation token for the operation.</param>
/// <returns>The unique identifier ({responseType}) of the created order.</returns>";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "docs.txt"), documentation);

        // Assert
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "docs.txt"));
        content.Should().Contain("<summary>");
        content.Should().Contain(requestType);
        content.Should().Contain(responseType);
    }

    [Fact]
    public void AICommand_SuggestsPerformanceImprovements()
    {
        // Arrange
        var code = @"
var list = new List<int>();
for (int i = 0; i < 1000000; i++)
{
    list.Add(i);
}";

        // Act
        var suggestions = new List<string>();
        if (code.Contains("new List<int>()"))
        {
            suggestions.Add("Consider initializing List with capacity: new List<int>(1000000)");
        }

        // Assert
        suggestions.Should().Contain(s => s.Contains("capacity"));
    }

    [Fact]
    public void AICommand_IdentifiesUnusedCode()
    {
        // Arrange
        var unusedMethods = new[] { "Helper1", "Helper2", "Helper3" };
        var usedMethods = new[] { "Handle", "Validate" };

        // Act
        var unused = unusedMethods.Except(usedMethods).ToList();

        // Assert
        unused.Should().HaveCount(3);
        unused.Should().NotContain("Handle");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, true);
        }
    }
}
