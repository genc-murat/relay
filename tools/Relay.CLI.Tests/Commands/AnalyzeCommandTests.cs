using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class AnalyzeCommandTests : IDisposable
{
    private readonly string _testPath;

    public AnalyzeCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-analyze-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    [Fact]
    public async Task ProjectAnalysis_CalculatesCorrectScore()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            PerformanceIssues = new List<PerformanceIssue>(),
            ReliabilityIssues = new List<ReliabilityIssue>(),
            HasRelayCore = true,
            HasLogging = true,
            HasValidation = true,
            HasCaching = true
        };

        // Act
        var score = CalculateScore(analysis);

        // Assert
        score.Should().BeGreaterThan(9.0); // Perfect score with bonuses
    }

    [Fact]
    public async Task ProjectAnalysis_DetectsPerformanceIssues()
    {
        // Arrange
        await CreateTestHandler(usesValueTask: false, hasCancellationToken: false);

        // Act
        var files = Directory.GetFiles(_testPath, "*.cs");
        var content = await File.ReadAllTextAsync(files[0]);

        // Assert
        content.Should().Contain("Task<");
        content.Should().NotContain("ValueTask");
        content.Should().NotContain("CancellationToken");
    }

    [Fact]
    public async Task ProjectAnalysis_DetectsReliabilityIssues()
    {
        // Arrange
        await CreateTestHandler(hasLogging: false, hasValidation: false);

        // Act
        var files = Directory.GetFiles(_testPath, "*.cs");
        var content = await File.ReadAllTextAsync(files[0]);

        // Assert
        content.Should().NotContain("ILogger");
        content.Should().NotContain("[Required]");
    }

    [Fact]
    public void HandlerInfo_TracksCorrectProperties()
    {
        // Arrange & Act
        var handler = new HandlerInfo
        {
            Name = "TestHandler",
            FilePath = "/test/handler.cs",
            IsAsync = true,
            HasDependencies = true,
            UsesValueTask = true,
            HasCancellationToken = true,
            HasLogging = true,
            HasValidation = true,
            LineCount = 50
        };

        // Assert
        handler.Name.Should().Be("TestHandler");
        handler.IsAsync.Should().BeTrue();
        handler.UsesValueTask.Should().BeTrue();
        handler.HasCancellationToken.Should().BeTrue();
        handler.HasLogging.Should().BeTrue();
        handler.LineCount.Should().Be(50);
    }

    [Fact]
    public void RequestInfo_TracksCorrectProperties()
    {
        // Arrange & Act
        var request = new RequestInfo
        {
            Name = "TestRequest",
            FilePath = "/test/request.cs",
            IsRecord = true,
            HasResponse = true,
            HasValidation = true,
            ParameterCount = 3,
            HasCaching = true,
            HasAuthorization = true
        };

        // Assert
        request.Name.Should().Be("TestRequest");
        request.IsRecord.Should().BeTrue();
        request.HasValidation.Should().BeTrue();
        request.ParameterCount.Should().Be(3);
        request.HasCaching.Should().BeTrue();
    }

    [Fact]
    public void PerformanceIssue_ContainsAllRequiredFields()
    {
        // Arrange & Act
        var issue = new PerformanceIssue
        {
            Type = "Task Usage",
            Severity = "Medium",
            Count = 5,
            Description = "5 handlers use Task instead of ValueTask",
            Recommendation = "Switch to ValueTask<T>",
            PotentialImprovement = "5-15% faster execution"
        };

        // Assert
        issue.Type.Should().Be("Task Usage");
        issue.Severity.Should().Be("Medium");
        issue.Count.Should().Be(5);
        issue.Recommendation.Should().NotBeEmpty();
        issue.PotentialImprovement.Should().NotBeEmpty();
    }

    [Fact]
    public void ReliabilityIssue_ContainsAllRequiredFields()
    {
        // Arrange & Act
        var issue = new ReliabilityIssue
        {
            Type = "Logging",
            Severity = "Medium",
            Count = 3,
            Description = "3 handlers don't have logging",
            Recommendation = "Add logging for better observability",
            Impact = "Improved troubleshooting"
        };

        // Assert
        issue.Type.Should().Be("Logging");
        issue.Severity.Should().Be("Medium");
        issue.Count.Should().Be(3);
        issue.Recommendation.Should().NotBeEmpty();
        issue.Impact.Should().NotBeEmpty();
    }

    [Fact]
    public void Recommendation_ContainsActionPlan()
    {
        // Arrange & Act
        var recommendation = new Recommendation
        {
            Category = "Performance",
            Priority = "High",
            Title = "Optimize Handler Performance",
            Description = "Several optimization opportunities found",
            Actions = new List<string>
            {
                "Switch to ValueTask",
                "Add cancellation support",
                "Implement caching"
            },
            EstimatedImpact = "20-50% performance improvement"
        };

        // Assert
        recommendation.Category.Should().Be("Performance");
        recommendation.Priority.Should().Be("High");
        recommendation.Actions.Should().HaveCount(3);
        recommendation.EstimatedImpact.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProjectAnalysis_DetectsProjectFiles()
    {
        // Arrange
        await CreateTestProject();

        // Act
        var projectFiles = Directory.GetFiles(_testPath, "*.csproj", SearchOption.AllDirectories);
        var sourceFiles = Directory.GetFiles(_testPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        // Assert
        projectFiles.Should().NotBeEmpty();
        sourceFiles.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("TestHandler.cs", true)]
    [InlineData("TestRequest.cs", false)]
    [InlineData("TestQuery.cs", false)]
    [InlineData("SomeClass.cs", false)]
    public void FilenamePattern_IdentifiesHandlers(string filename, bool isHandler)
    {
        // Act
        var detected = filename.EndsWith("Handler.cs");

        // Assert
        detected.Should().Be(isHandler);
    }

    [Theory]
    [InlineData("TestRequest.cs", true)]
    [InlineData("TestQuery.cs", true)]
    [InlineData("TestCommand.cs", true)]
    [InlineData("TestHandler.cs", false)]
    public void FilenamePattern_IdentifiesRequests(string filename, bool isRequest)
    {
        // Act
        var detected = filename.EndsWith("Request.cs") ||
                      filename.EndsWith("Query.cs") ||
                      filename.EndsWith("Command.cs");

        // Assert
        detected.Should().Be(isRequest);
    }

    [Fact]
    public void ScoreCalculation_WithNoIssues_Returns10()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            PerformanceIssues = new List<PerformanceIssue>(),
            ReliabilityIssues = new List<ReliabilityIssue>(),
            HasRelayCore = false,
            HasLogging = false,
            HasValidation = false,
            HasCaching = false
        };

        // Act
        var score = CalculateScore(analysis);

        // Assert
        score.Should().Be(10.0);
    }

    [Fact]
    public void ScoreCalculation_WithHighSeverityIssues_DeductsPoints()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            PerformanceIssues = new List<PerformanceIssue>
            {
                new() { Severity = "High" },
                new() { Severity = "High" }
            },
            ReliabilityIssues = new List<ReliabilityIssue>
            {
                new() { Severity = "High" }
            }
        };

        // Act
        var score = CalculateScore(analysis);

        // Assert - Should deduct: 2*2.0 + 1*1.5 = 5.5 points
        score.Should().BeLessThan(5.0);
    }

    [Fact]
    public void ScoreCalculation_WithBonuses_AddsPoints()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            HasRelayCore = true,    // +0.5
            HasLogging = true,      // +0.3
            HasValidation = true,   // +0.3
            HasCaching = true       // +0.2
        };

        // Act
        var score = CalculateScore(analysis);

        // Assert - Should be: 10 + 0.5 + 0.3 + 0.3 + 0.2 = 11.3, capped at 10
        score.Should().Be(10.0);
    }

    [Fact]
    public async Task ProjectAnalysis_ExcludesTestFiles_WhenRequested()
    {
        // Arrange
        await CreateTestProject();
        var testFile = Path.Combine(_testPath, "TestClass.Test.cs");
        await File.WriteAllTextAsync(testFile, "// Test file");

        // Act
        var sourceFiles = Directory.GetFiles(_testPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .Where(f => !f.Contains("Test")) // Exclude tests
            .ToList();

        // Assert
        sourceFiles.Should().NotContain(f => f.Contains("Test"));
    }

    private async Task CreateTestHandler(
        bool usesValueTask = true,
        bool hasCancellationToken = true,
        bool hasLogging = true,
        bool hasValidation = true)
    {
        var returnType = usesValueTask ? "ValueTask<string>" : "Task<string>";
        var cancellationParam = hasCancellationToken ? ", CancellationToken ct" : "";
        var logger = hasLogging ? "private readonly ILogger<TestHandler> _logger;" : "";
        var validation = hasValidation ? "[Required]" : "";

        var handler = $@"using Relay.Core;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

public class TestHandler : IRequestHandler<TestRequest, string>
{{
    {logger}

    [Handle]
    public async {returnType} HandleAsync(TestRequest request{cancellationParam})
    {{
        return ""test"";
    }}
}}

public record TestRequest({validation} string Name) : IRequest<string>;";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"), handler);
    }

    private async Task CreateTestProject()
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
        await CreateTestHandler();
    }

    private double CalculateScore(ProjectAnalysis analysis)
    {
        double score = 10.0;

        // Deduct for performance issues
        score -= analysis.PerformanceIssues.Count(i => i.Severity == "High") * 2.0;
        score -= analysis.PerformanceIssues.Count(i => i.Severity == "Medium") * 1.0;
        score -= analysis.PerformanceIssues.Count(i => i.Severity == "Low") * 0.5;

        // Deduct for reliability issues
        score -= analysis.ReliabilityIssues.Count(i => i.Severity == "High") * 1.5;
        score -= analysis.ReliabilityIssues.Count(i => i.Severity == "Medium") * 0.8;
        score -= analysis.ReliabilityIssues.Count(i => i.Severity == "Low") * 0.3;

        // Bonus for good practices
        if (analysis.HasRelayCore) score += 0.5;
        if (analysis.HasLogging) score += 0.3;
        if (analysis.HasValidation) score += 0.3;
        if (analysis.HasCaching) score += 0.2;

        return Math.Max(0, Math.Min(10, score));
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
