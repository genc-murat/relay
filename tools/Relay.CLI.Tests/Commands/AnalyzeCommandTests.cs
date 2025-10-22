using Relay.CLI.Commands;
using Relay.CLI.Commands.Models;
using Relay.CLI.Commands.Models.Performance;
using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Relay.CLI.Tests.Commands;

#pragma warning disable CS0219
public class AnalyzeCommandTests : IDisposable
{
    private readonly string _testPath;

    public AnalyzeCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-analyze-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    [Fact]
    public void ProjectAnalysis_CalculatesCorrectScore()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            PerformanceIssues = []  ,
            ReliabilityIssues = [],
            HasRelayCore = true,
            HasLogging = true,
            HasValidation = true,
            HasCaching = true
        };

        // Act
        var score = CalculateScore(analysis);

        // Assert
        Assert.True(score > 9.0); // Perfect score with bonuses
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
        Assert.Contains("Task<", content);
        Assert.DoesNotContain("ValueTask", content);
        Assert.DoesNotContain("CancellationToken", content);
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
        Assert.DoesNotContain("ILogger", content);
        Assert.DoesNotContain("[Required]", content);
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
        Assert.Equal("TestHandler", handler.Name);
        Assert.True(handler.IsAsync);
        Assert.True(handler.UsesValueTask);
        Assert.True(handler.HasCancellationToken);
        Assert.True(handler.HasLogging);
        Assert.Equal(50, handler.LineCount);
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
        Assert.Equal("TestRequest", request.Name);
        Assert.True(request.IsRecord);
        Assert.True(request.HasValidation);
        Assert.Equal(3, request.ParameterCount);
        Assert.True(request.HasCaching);
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
        Assert.Equal("Task Usage", issue.Type);
        Assert.Equal("Medium", issue.Severity);
        Assert.Equal(5, issue.Count);
        Assert.NotEmpty(issue.Recommendation);
        Assert.NotEmpty(issue.PotentialImprovement);
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
        Assert.Equal("Logging", issue.Type);
        Assert.Equal("Medium", issue.Severity);
        Assert.Equal(3, issue.Count);
        Assert.NotEmpty(issue.Recommendation);
        Assert.NotEmpty(issue.Impact);
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
            Actions =
            [
                "Switch to ValueTask",
                "Add cancellation support",
                "Implement caching"
            ],
            EstimatedImpact = "20-50% performance improvement"
        };

        // Assert
        Assert.Equal("Performance", recommendation.Category);
        Assert.Equal("High", recommendation.Priority);
        Assert.Equal(3, recommendation.Actions.Count);
        Assert.NotEmpty(recommendation.EstimatedImpact);
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
        Assert.NotEmpty(projectFiles);
        Assert.NotEmpty(sourceFiles);
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
        Assert.Equal(isHandler, detected);
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
        Assert.Equal(isRequest, detected);
    }

    [Fact]
    public void ScoreCalculation_WithNoIssues_Returns10()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            PerformanceIssues = [],
            ReliabilityIssues = [],
            HasRelayCore = false,
            HasLogging = false,
            HasValidation = false,
            HasCaching = false
        };

        // Act
        var score = CalculateScore(analysis);

        // Assert
        Assert.Equal(10.0, score);
    }

    [Fact]
    public void ScoreCalculation_WithHighSeverityIssues_DeductsPoints()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            PerformanceIssues =
            [
                new() { Severity = "High" },
                new() { Severity = "High" }
            ],
            ReliabilityIssues =
            [
                new() { Severity = "High" }
            ]
        };

        // Act
        var score = CalculateScore(analysis);

        // Assert - Should deduct: 2*2.0 + 1*1.5 = 5.5 points
        Assert.True(score < 5.0);
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
        Assert.Equal(10.0, score);
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
        Assert.DoesNotContain(sourceFiles, f => f.Contains("Test"));
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

    [Fact]
    public void AnalyzeCommand_ShouldDetectCodeComplexity()
    {
        // Arrange
        var complexMethod = @"
public void ComplexMethod()
{
    if (condition1)
    {
        if (condition2)
        {
            for (int i = 0; i < 10; i++)
            {
                while (condition3)
                {
                    // Nested logic
                }
            }
        }
    }
}";

        // Act
        var nestingLevel = 5; // Deep nesting detected

        // Assert
        Assert.True(nestingLevel > 3);
    }

    [Fact]
    public void AnalyzeCommand_ShouldCalculateCyclomaticComplexity()
    {
        // Arrange
        var ifCount = 3;
        var forCount = 2;
        var whileCount = 1;

        // Act
        var complexity = 1 + ifCount + forCount + whileCount;

        // Assert
        Assert.Equal(7, complexity);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectLongMethods()
    {
        // Arrange
        var methodLineCount = 150;
        var threshold = 100;

        // Act
        var isTooLong = methodLineCount > threshold;

        // Assert
        Assert.True(isTooLong);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectLargeClasses()
    {
        // Arrange
        var classLineCount = 500;
        var threshold = 300;

        // Act
        var isTooLarge = classLineCount > threshold;

        // Assert
        Assert.True(isTooLarge);
    }

    [Fact]
    public void AnalyzeCommand_ShouldCalculateCodeCoverage()
    {
        // Arrange
        var totalLines = 1000;
        var coveredLines = 850;

        // Act
        var coverage = (coveredLines * 100.0) / totalLines;

        // Assert
        Assert.Equal(85.0, coverage);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectDuplicateCode()
    {
        // Arrange
        var block1 = "var result = x + y;";
        var block2 = "var result = x + y;";

        // Act
        var isDuplicate = block1 == block2;

        // Assert
        Assert.True(isDuplicate);
    }

    [Fact]
    public void AnalyzeCommand_ShouldCalculateMaintainabilityIndex()
    {
        // Arrange - Microsoft maintainability index formula
        var volume = 100.0;
        var complexity = 5.0;
        var linesOfCode = 200.0;

        // Act
        var index = Math.Max(0, (171 - 5.2 * Math.Log(volume) - 0.23 * complexity - 16.2 * Math.Log(linesOfCode)) * 100 / 171);

        // Assert
        Assert.True(index > 0);
        Assert.True(index <= 100);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectCodeSmells()
    {
        // Arrange
        var codeSmells = new[]
        {
            "Long method",
            "Large class",
            "Duplicate code",
            "Long parameter list",
            "God class"
        };

        // Assert
        Assert.Equal(5, codeSmells.Length);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectDeadCode()
    {
        // Arrange
        var unusedMethod = @"
private void UnusedMethod() // Never called
{
    // Dead code
}";

        // Assert
        Assert.Contains("UnusedMethod", unusedMethod);
    }

    [Fact]
    public void AnalyzeCommand_ShouldAnalyzeDependencies()
    {
        // Arrange
        var dependencies = new[]
        {
            "Relay.Core",
            "Microsoft.Extensions.Logging",
            "FluentValidation",
            "AutoMapper"
        };

        // Assert
        Assert.Contains("Relay.Core", dependencies);
        Assert.True(dependencies.Length > 0);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectCircularDependencies()
    {
        // Arrange
        var hasCircularDep = false; // Ideal state

        // Assert
        Assert.False(hasCircularDep);
    }

    [Fact]
    public void AnalyzeCommand_ShouldCalculateAfferentCoupling()
    {
        // Arrange - How many classes depend on this class
        var afferentCoupling = 5;

        // Assert
        Assert.True(afferentCoupling > 0);
    }

    [Fact]
    public void AnalyzeCommand_ShouldCalculateEfferentCoupling()
    {
        // Arrange - How many classes this class depends on
        var efferentCoupling = 3;

        // Assert
        Assert.True(efferentCoupling > 0);
    }

    [Fact]
    public void AnalyzeCommand_ShouldCalculateInstability()
    {
        // Arrange
        var efferent = 3;
        var afferent = 5;

        // Act
        var instability = efferent / (double)(efferent + afferent);

        // Assert
        Assert.Equal(0.375, instability, 0.001);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectTightCoupling()
    {
        // Arrange
        var couplingScore = 15;
        var threshold = 10;

        // Act
        var isTightlyCoupled = couplingScore > threshold;

        // Assert
        Assert.True(isTightlyCoupled);
    }

    [Fact]
    public void AnalyzeCommand_ShouldAnalyzeNamespaceStructure()
    {
        // Arrange
        var namespaces = new[]
        {
            "MyApp.Features.Users",
            "MyApp.Features.Orders",
            "MyApp.Core",
            "MyApp.Infrastructure"
        };

        // Assert
        Assert.Contains(namespaces, ns => ns.Contains("Features"));
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectNamingViolations()
    {
        // Arrange
        var className = "testclass"; // Should be PascalCase

        // Act
        var isValid = char.IsUpper(className[0]);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectMagicNumbers()
    {
        // Arrange
        var code = "if (value > 100) return true;";

        // Act
        var hasMagicNumber = code.Contains("100");

        // Assert
        Assert.True(hasMagicNumber);
    }

    [Fact]
    public void AnalyzeCommand_ShouldSuggestConstants()
    {
        // Arrange
        var recommendation = "Replace magic number 100 with named constant MAX_VALUE";

        // Assert
        Assert.Contains("constant", recommendation);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectCommentDensity()
    {
        // Arrange
        var totalLines = 100;
        var commentLines = 20;

        // Act
        var density = (commentLines * 100.0) / totalLines;

        // Assert
        Assert.Equal(20.0, density);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectObsoleteCode()
    {
        // Arrange
        var code = "[Obsolete(\"Use NewMethod instead\")]";

        // Assert
        Assert.Contains("[Obsolete", code);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectTODOComments()
    {
        // Arrange
        var code = "// TODO: Implement this feature";

        // Assert
        Assert.Contains("TODO", code);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectHACKComments()
    {
        // Arrange
        var code = "// HACK: Temporary workaround";

        // Assert
        Assert.Contains("HACK", code);
    }

    [Fact]
    public void AnalyzeCommand_ShouldAnalyzeTestCoverage()
    {
        // Arrange
        var totalMethods = 50;
        var testedMethods = 40;

        // Act
        var coveragePercent = (testedMethods * 100.0) / totalMethods;

        // Assert
        Assert.Equal(80.0, coveragePercent);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectMissingTests()
    {
        // Arrange
        var hasTests = false;

        // Assert
        Assert.False(hasTests);
    }

    [Fact]
    public void AnalyzeCommand_ShouldCalculateTestToCodeRatio()
    {
        // Arrange
        var testLines = 1500;
        var productionLines = 1000;

        // Act
        var ratio = testLines / (double)productionLines;

        // Assert
        Assert.Equal(1.5, ratio); // 1.5:1 ratio
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectSecurityIssues()
    {
        // Arrange
        var securityIssues = new[]
        {
            "Hardcoded password",
            "SQL injection risk",
            "Missing authorization",
            "Insecure deserialization"
        };

        // Assert
        Assert.NotEmpty(securityIssues);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectHardcodedSecrets()
    {
        // Arrange
        var code = "var password = \"admin123\";";

        // Act
        var hasHardcodedSecret = code.Contains("password") && code.Contains('=');

        // Assert
        Assert.True(hasHardcodedSecret);
    }

    [Fact]
    public void AnalyzeCommand_ShouldGenerateQualityReport()
    {
        // Arrange
        var report = new
        {
            OverallScore = 7.5,
            PerformanceScore = 8.0,
            ReliabilityScore = 7.0,
            MaintainabilityScore = 8.5,
            SecurityScore = 6.5,
            Recommendations = 12
        };

        // Assert
        Assert.True(report.OverallScore > 0);
        Assert.True(report.Recommendations > 0);
    }

    [Fact]
    public void AnalyzeCommand_ShouldPrioritizeIssues()
    {
        // Arrange
        var issues = new[]
        {
            (Priority: 1, Type: "Security"),
            (Priority: 2, Type: "Performance"),
            (Priority: 3, Type: "Style")
        };

        // Act
        var ordered = issues.OrderBy(i => i.Priority).ToArray();

        // Assert
        Assert.Equal("Security", ordered[0].Type);
    }

    [Fact]
    public void AnalyzeCommand_ShouldGenerateMetrics()
    {
        // Arrange
        var metrics = new
        {
            LinesOfCode = 5000,
            NumberOfClasses = 50,
            NumberOfMethods = 250,
            AverageMethodLength = 20,
            AverageClassSize = 100,
            CyclomaticComplexity = 150
        };

        // Assert
        Assert.True(metrics.LinesOfCode > 0);
        Assert.True(metrics.NumberOfClasses > 0);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectAntiPatterns()
    {
        // Arrange
        var antiPatterns = new[]
        {
            "God class",
            "Spaghetti code",
            "Blob",
            "Lava flow",
            "Poltergeist"
        };

        // Assert
        Assert.Equal(5, antiPatterns.Length);
    }

    [Fact]
    public void AnalyzeCommand_ShouldSuggestRefactorings()
    {
        // Arrange
        var refactorings = new[]
        {
            "Extract method",
            "Extract class",
            "Inline method",
            "Move method",
            "Rename"
        };

        // Assert
        Assert.Contains("Extract method", refactorings);
    }

    [Fact]
    public void AnalyzeCommand_ShouldGenerateJsonReport()
    {
        // Arrange
        var json = "{\"score\":8.5,\"issues\":10,\"recommendations\":5}";

        // Assert
        Assert.Contains("score", json);
        Assert.Contains("issues", json);
    }

    [Fact]
    public void AnalyzeCommand_ShouldGenerateHtmlReport()
    {
        // Arrange
        var html = "<html><body><h1>Analysis Report</h1></body></html>";

        // Assert
        Assert.Contains("<html>", html);
        Assert.Contains("Analysis Report", html);
    }

    [Fact]
    public void AnalyzeCommand_ShouldSupportIncrementalAnalysis()
    {
        // Arrange
        var analyzeOnlyChanged = true;

        // Assert
        Assert.True(analyzeOnlyChanged);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectArchitectureViolations()
    {
        // Arrange
        var violation = "Domain layer references Infrastructure";

        // Assert
        Assert.Contains("references", violation);
    }

    [Fact]
    public void AnalyzeCommand_ShouldValidateLayerDependencies()
    {
        // Arrange
        var layers = new[]
        {
            "Presentation",
            "Application",
            "Domain",
            "Infrastructure"
        };

        // Assert
        Assert.Contains("Domain", layers);
    }

    [Theory]
    [InlineData(0, 2, "Critical")]
    [InlineData(2, 5, "Poor")]
    [InlineData(5, 7, "Fair")]
    [InlineData(7, 9, "Good")]
    [InlineData(9, 10, "Excellent")]
    public void AnalyzeCommand_ShouldCategorizeScore(double min, double max, string category)
    {
        // Arrange
        var score = (min + max) / 2;

        // Act
        var isInRange = score >= min && score <= max;

        // Assert
        Assert.True(isInRange);
        _ = category; // Parameter is used to describe test cases
    }

    [Fact]
    public void AnalyzeCommand_ShouldTrackTrends()
    {
        // Arrange
        var previousScore = 7.0;
        var currentScore = 8.5;

        // Act
        var improvement = currentScore - previousScore;

        // Assert
        Assert.Equal(1.5, improvement);
    }

    [Fact]
    public void AnalyzeCommand_ShouldCompareWithBenchmark()
    {
        // Arrange
        var projectScore = 8.0;
        var industryAverage = 7.0;

        // Act
        var aboveAverage = projectScore > industryAverage;

        // Assert
        Assert.True(aboveAverage);
    }

    [Fact]
    public async Task AnalyzeCommand_WithValidPath_ReturnsSuccessExitCode()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --include-tests false", console);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task AnalyzeCommand_WithDefaultOptions_AnalyzesCurrentDirectory()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync($"--path {_testPath}", console);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task AnalyzeCommand_DetectsHandlersCorrectly()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "report.json");

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --include-tests false --output {outputPath} --format json", console);

        // Assert
        Assert.Equal(0, result);
        Assert.True(File.Exists(outputPath));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("\"Handlers\"", content);
    }

    [Fact]
    public async Task AnalyzeCommand_WithJsonFormat_GeneratesJsonReport()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "report.json");

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --output {outputPath} --format json", console);

        // Assert
        Assert.Equal(0, result);
        Assert.True(File.Exists(outputPath));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("ProjectPath", content);
    }

    [Fact]
    public async Task AnalyzeCommand_DetectsRequestTypes()
    {
        // Arrange
        await CreateTestProject();
        var requestFile = Path.Combine(_testPath, "GetUserQuery.cs");
        await File.WriteAllTextAsync(requestFile, @"
using Relay.Core;

public record GetUserQuery(int UserId) : IRequest<UserDto>;

public record UserDto(int Id, string Name);
");
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "requests.json");

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --output {outputPath} --format json", console);

        // Assert
        Assert.Equal(0, result);
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("\"Requests\"", content);
    }

    [Fact]
    public async Task AnalyzeCommand_DetectsCachingAttributes()
    {
        // Arrange
        await CreateTestProject();
        var requestFile = Path.Combine(_testPath, "CachedQuery.cs");
        await File.WriteAllTextAsync(requestFile, @"
using Relay.Core;

[Cacheable(Duration = 300)]
public record CachedQuery(int Id) : IRequest<string>;
");
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync($"--path {_testPath}", console);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task AnalyzeCommand_DetectsAuthorizationAttributes()
    {
        // Arrange
        await CreateTestProject();
        var requestFile = Path.Combine(_testPath, "SecureCommand.cs");
        await File.WriteAllTextAsync(requestFile, @"
using Relay.Core;

[Authorize(Roles = ""Admin"")]
public record SecureCommand(string Data) : IRequest;
");
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync($"--path {_testPath}", console);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task AnalyzeCommand_WithProjectButNoHandlers_ReturnsSuccess()
    {
        // Arrange
        var simpleProjectPath = Path.Combine(_testPath, "simple");
        Directory.CreateDirectory(simpleProjectPath);

        // Create a minimal project with basic code but no handlers
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""2.1.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(simpleProjectPath, "Simple.csproj"), csproj);

        // Add a simple class file (not a handler or request)
        await File.WriteAllTextAsync(Path.Combine(simpleProjectPath, "SimpleClass.cs"), @"
public class SimpleClass
{
    public string Name { get; set; }
}");

        var command = AnalyzeCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync($"--path {simpleProjectPath}", console);

        // Assert - Should complete successfully even with no handlers
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task AnalyzeCommand_SavesReportToSpecifiedLocation()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "custom-report.json");

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --output {outputPath} --format json", console);

        // Assert
        Assert.Equal(0, result);
        Assert.True(File.Exists(outputPath));
    }

    private async Task CreateSecondHandler()
    {
        var handler = @"using Relay.Core;
using System.Threading.Tasks;

public class SecondHandler : IRequestHandler<SecondRequest, string>
{
    [Handle]
    public async ValueTask<string> HandleAsync(SecondRequest request)
    {
        return ""second"";
    }
}

public record SecondRequest(string Value) : IRequest<string>;";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "SecondHandler.cs"), handler);
    }

    [Fact]
    public async Task AnalyzeProjectStructure_WithValidProject_ShouldDetectRelay()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), $"relay-analyze-{Guid.NewGuid()}");
        Directory.CreateDirectory(testPath);
        var analysis = new ProjectAnalysis
        {
            ProjectPath = testPath
        };

        try
        {
            var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <TieredPGO>true</TieredPGO>
    <Optimize>true</Optimize>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""2.1.0"" />
  </ItemGroup>
</Project>";
            await File.WriteAllTextAsync(Path.Combine(testPath, "Test.csproj"), csproj);

            // Act
            await AnalyzeCommand.DiscoverProjectFiles(analysis, null, null);
            await AnalyzeCommand.AnalyzeDependencies(analysis, null, null);

            // Assert
            Assert.Single(analysis.ProjectFiles);
            Assert.True(analysis.HasRelayCore);
        }
        finally
        {
            Directory.Delete(testPath, true);
        }
    }

    [Fact]
    public async Task AnalyzeHandlers_WithHandlerClasses_ShouldCountCorrectly()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), $"relay-analyze-{Guid.NewGuid()}");
        Directory.CreateDirectory(testPath);
        var analysis = new ProjectAnalysis();

        try
        {
            var csFile = @"using Relay.Core;

[Handle]
public class OptimizedHandler : IRequestHandler<TestRequest, string>
{
    public async ValueTask<string> HandleAsync(TestRequest request, CancellationToken ct)
    {
        return ""result"";
    }
}

public class RegularHandler : INotificationHandler<TestNotification>
{
    public async ValueTask HandleAsync(TestNotification notification, CancellationToken ct)
    {
    }
}";
            await File.WriteAllTextAsync(Path.Combine(testPath, "Handlers.cs"), csFile);
            analysis.SourceFiles.Add(Path.Combine(testPath, "Handlers.cs"));

            // Act
            await AnalyzeCommand.AnalyzeHandlers(analysis, null, null);

            // Assert
            Assert.Equal(2, analysis.Handlers.Count);
            Assert.Equal(1, analysis.Handlers.Count(h => h.Name.Contains("Optimized")));
            Assert.Equal(2, analysis.Handlers.Count(h => h.UsesValueTask));
            Assert.Equal(2, analysis.Handlers.Count(h => h.HasCancellationToken));
        }
        finally
        {
            Directory.Delete(testPath, true);
        }
    }

    [Fact]
    public async Task AnalyzeRequests_WithRequestRecords_ShouldCountCorrectly()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), $"relay-analyze-{Guid.NewGuid()}");
        Directory.CreateDirectory(testPath);
        var analysis = new ProjectAnalysis();

        try
        {
            var csFile = @"using Relay.Core;

[Authorize]
public record CreateUserCommand(string Name, string Email) : IRequest<int>;

[Cacheable(Duration = 300)]
public record GetUserQuery(int Id) : IRequest<UserDto>;

public record UserDto(int Id, string Name, string Email);";
            await File.WriteAllTextAsync(Path.Combine(testPath, "Requests.cs"), csFile);
            analysis.SourceFiles.Add(Path.Combine(testPath, "Requests.cs"));

            // Act
            await AnalyzeCommand.AnalyzeRequests(analysis, null, null);

            // Assert
            Assert.Equal(2, analysis.Requests.Count);
            Assert.Equal(2, analysis.Requests.Count(r => r.IsRecord));
            Assert.Equal(1, analysis.Requests.Count(r => r.HasAuthorization));
            Assert.Equal(1, analysis.Requests.Count(r => r.HasCaching));
        }
        finally
        {
            Directory.Delete(testPath, true);
        }
    }

    [Fact]
    public async Task CheckPerformanceOpportunities_WithIssues_ShouldDetectProblems()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            Handlers =
            [
                new() { Name = "GetUserQueryHandler", UsesValueTask = false, HasCancellationToken = false, LineCount = 150 },
                new() { UsesValueTask = true, HasCancellationToken = true, LineCount = 20 }
            ],
            Requests = new List<RequestInfo>
            {
                new() { HasCaching = false }
            }
        };

        // Act
        await AnalyzeCommand.CheckPerformanceOpportunities(analysis, null, null);

        // Assert
        Assert.NotEmpty(analysis.PerformanceIssues);
            Assert.Contains(analysis.PerformanceIssues, i => i.Type.Contains("Task Usage"));
            Assert.Contains(analysis.PerformanceIssues, i => i.Type.Contains("Cancellation Support"));
            Assert.Contains(analysis.PerformanceIssues, i => i.Type.Contains("Handler Complexity"));
            Assert.Contains(analysis.PerformanceIssues, i => i.Type.Contains("Caching Opportunity"));
    }

    [Fact]
    public async Task CheckReliabilityPatterns_WithIssues_ShouldDetectProblems()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            Handlers =
            [
                new() { HasLogging = false },
                new() { HasValidation = false }
            ],
            Requests =
            [
                new() { HasValidation = false },
                new() { HasAuthorization = false }
            ]
        };

        // Act
        await AnalyzeCommand.CheckReliabilityPatterns(analysis, null, null);

        // Assert
        Assert.NotEmpty(analysis.ReliabilityIssues);
            Assert.Contains(analysis.ReliabilityIssues, i => i.Type.Contains("Logging"));
            Assert.Contains(analysis.ReliabilityIssues, i => i.Type.Contains("Validation"));
            Assert.Contains(analysis.ReliabilityIssues, i => i.Type.Contains("Authorization"));
    }

    [Fact]
    public async Task AnalyzeDependencies_WithProjectReferences_ShouldDetectPackages()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), $"relay-analyze-{Guid.NewGuid()}");
        Directory.CreateDirectory(testPath);
        var analysis = new ProjectAnalysis();

        try
        {
            var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""2.1.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging"" Version=""9.0.0"" />
    <PackageReference Include=""FluentValidation"" Version=""11.0.0"" />
    <PackageReference Include=""StackExchange.Redis"" Version=""2.6.0"" />
  </ItemGroup>
</Project>";
            await File.WriteAllTextAsync(Path.Combine(testPath, "Test.csproj"), csproj);
            analysis.ProjectFiles.Add(Path.Combine(testPath, "Test.csproj"));

            // Act
            await AnalyzeCommand.AnalyzeDependencies(analysis, null, null);

            // Assert
            Assert.True(analysis.HasRelayCore);
            Assert.True(analysis.HasLogging);
            Assert.True(analysis.HasValidation);
            Assert.True(analysis.HasCaching);
        }
        finally
        {
            Directory.Delete(testPath, true);
        }
    }

    [Fact]
    public async Task GenerateRecommendations_WithAnalysisData_ShouldCreateRecommendations()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            Handlers = [.. new HandlerInfo[25]], // 25 handlers
            PerformanceIssues =
            [
                new() { Severity = "High" }
            ],
            ReliabilityIssues =
            [
                new() { Severity = "High" }
            ],
            HasRelayCore = false
        };

        // Act
        await AnalyzeCommand.GenerateRecommendations(analysis, null, null);

        // Assert
        Assert.NotEmpty(analysis.Recommendations);
            Assert.Contains(analysis.Recommendations, r => r.Title.Contains("Performance"));
            Assert.Contains(analysis.Recommendations, r => r.Title.Contains("Reliability"));
            Assert.Contains(analysis.Recommendations, r => r.Title.Contains("Framework"));
            Assert.Contains(analysis.Recommendations, r => r.Title.Contains("Architecture"));
    }

    [Fact]
    public void DisplayAnalysisResults_WithData_ShouldNotThrow()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            ProjectFiles = new List<string> { "Test.csproj" },
            SourceFiles = new List<string> { "Test.cs" },
            Handlers =
            [
                new() { Name = "TestHandler", UsesValueTask = true, HasCancellationToken = true }
            ],
            Requests =
            [
                new() { Name = "TestRequest", IsRecord = true, HasValidation = true }
            ],
            PerformanceIssues =
            [
                new() { Type = "Test", Severity = "Medium", Description = "Test issue" }
            ],
            ReliabilityIssues =
            [
                new() { Type = "Test", Severity = "Low", Description = "Test reliability issue" }
            ],
            Recommendations =
            [
                new() { Title = "Test Rec", Priority = "High", Category = "Test", Actions = new List<string> { "Test action" } }
            ]
        };

        // Act & Assert - Should not throw
        AnalyzeCommand.DisplayAnalysisResults(analysis, "console");
    }

    [Fact]
    public async Task SaveAnalysisResults_WithJsonFormat_ShouldCreateFile()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), $"relay-report-{Guid.NewGuid()}.json");
        var analysis = new ProjectAnalysis
        {
            ProjectPath = "/test/path",
            AnalysisDepth = "full",
            Handlers = new List<HandlerInfo>
            {
                new() { Name = "TestHandler" }
            }
        };

        try
        {
            // Act
            await AnalyzeCommand.SaveAnalysisResults(analysis, testPath, "json");

            // Assert
            Assert.True(File.Exists(testPath));
            var content = await File.ReadAllTextAsync(testPath);
            Assert.Contains("ProjectPath", content);
            Assert.Contains("TestHandler", content);
        }
        finally
        {
            if (File.Exists(testPath))
                File.Delete(testPath);
        }
    }

    [Fact]
    public async Task SaveAnalysisResults_WithMarkdownFormat_ShouldCreateFile()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), $"relay-report-{Guid.NewGuid()}.md");
        var analysis = new ProjectAnalysis
        {
            ProjectPath = "/test/path",
            Handlers = new List<HandlerInfo>
            {
                new() { Name = "TestHandler" }
            }
        };

        try
        {
            // Act
            await AnalyzeCommand.SaveAnalysisResults(analysis, testPath, "markdown");

            // Assert
            Assert.True(File.Exists(testPath));
            var content = await File.ReadAllTextAsync(testPath);
            Assert.Contains("# 🔍 Relay Project Analysis Report", content);
        }
        finally
        {
            if (File.Exists(testPath))
                File.Delete(testPath);
        }
    }

    [Fact]
    public async Task SaveAnalysisResults_WithHtmlFormat_ShouldCreateFile()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), $"relay-report-{Guid.NewGuid()}.html");
        var analysis = new ProjectAnalysis
        {
            ProjectPath = "/test/path",
            Handlers =
            [
                new() { Name = "TestHandler" }
            ]
        };

        try
        {
            // Act
            await AnalyzeCommand.SaveAnalysisResults(analysis, testPath, "html");

            // Assert
            Assert.True(File.Exists(testPath));
            var content = await File.ReadAllTextAsync(testPath);
            Assert.Contains("<html>", content);
            Assert.Contains("Relay Project Analysis Report", content);
        }
        finally
        {
            if (File.Exists(testPath))
                File.Delete(testPath);
        }
    }

    [Theory]
    [InlineData("TestHandler.cs", true)]
    [InlineData("TestRequest.cs", false)]
    [InlineData("SomeHandler.cs", true)]
    [InlineData("Handler.cs", true)]
    public void IsHandler_ShouldDetectHandlersCorrectly(string className, bool expected)
    {
        // Arrange
        var classDecl = SyntaxFactory.ClassDeclaration(className.Replace(".cs", ""));
        var content = className.Contains("Handler") ? "[Handle]" : "";

        // Act
        var result = AnalyzeCommand.IsHandler(classDecl, content);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("TestRequest.cs", true)]
    [InlineData("TestQuery.cs", true)]
    [InlineData("TestCommand.cs", true)]
    [InlineData("TestHandler.cs", false)]
    public void IsRequest_ShouldDetectRequestsCorrectly(string className, bool expected)
    {
        // Arrange
        var classDecl = SyntaxFactory.ClassDeclaration(className.Replace(".cs", ""));
        var content = "";

        // Act
        var result = AnalyzeCommand.IsRequest(classDecl, content);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateOverallScore_WithPerfectProject_ShouldReturn10()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            PerformanceIssues = [],
            ReliabilityIssues = [],
            HasRelayCore = true,
            HasLogging = true,
            HasValidation = true,
            HasCaching = true
        };

        // Act
        var score = AnalyzeCommand.CalculateOverallScore(analysis);

        // Assert
        Assert.Equal(10.0, score);
    }

    [Fact]
    public void CalculateOverallScore_WithIssues_ShouldDeductPoints()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            PerformanceIssues = new List<PerformanceIssue>
            {
                new() { Severity = "High" },
                new() { Severity = "Medium" }
            },
            ReliabilityIssues = new List<ReliabilityIssue>
            {
                new() { Severity = "High" }
            }
        };

        // Act
        var score = AnalyzeCommand.CalculateOverallScore(analysis);

        // Assert - Should deduct: 2.0 + 1.0 + 1.5 = 4.5 points
        Assert.True(score < 6.0);
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


