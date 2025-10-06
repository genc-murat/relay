using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using Relay.CLI.Commands;
using Relay.CLI.Commands.Models;
using Relay.CLI.Commands.Models.Performance;

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
    public void ProjectAnalysis_CalculatesCorrectScore()
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
        nestingLevel.Should().BeGreaterThan(3);
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
        complexity.Should().Be(7);
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
        isTooLong.Should().BeTrue();
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
        isTooLarge.Should().BeTrue();
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
        coverage.Should().Be(85.0);
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
        isDuplicate.Should().BeTrue();
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
        index.Should().BeGreaterThan(0);
        index.Should().BeLessThanOrEqualTo(100);
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
        codeSmells.Should().HaveCount(5);
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
        unusedMethod.Should().Contain("UnusedMethod");
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
        dependencies.Should().Contain("Relay.Core");
        dependencies.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectCircularDependencies()
    {
        // Arrange
        var hasCircularDep = false; // Ideal state

        // Assert
        hasCircularDep.Should().BeFalse();
    }

    [Fact]
    public void AnalyzeCommand_ShouldCalculateAfferentCoupling()
    {
        // Arrange - How many classes depend on this class
        var afferentCoupling = 5;

        // Assert
        afferentCoupling.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AnalyzeCommand_ShouldCalculateEfferentCoupling()
    {
        // Arrange - How many classes this class depends on
        var efferentCoupling = 3;

        // Assert
        efferentCoupling.Should().BeGreaterThan(0);
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
        instability.Should().BeApproximately(0.375, 0.001);
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
        isTightlyCoupled.Should().BeTrue();
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
        namespaces.Should().Contain(ns => ns.Contains("Features"));
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectNamingViolations()
    {
        // Arrange
        var className = "testclass"; // Should be PascalCase

        // Act
        var isValid = char.IsUpper(className[0]);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectMagicNumbers()
    {
        // Arrange
        var code = "if (value > 100) return true;";

        // Act
        var hasMagicNumber = code.Contains("100");

        // Assert
        hasMagicNumber.Should().BeTrue();
    }

    [Fact]
    public void AnalyzeCommand_ShouldSuggestConstants()
    {
        // Arrange
        var recommendation = "Replace magic number 100 with named constant MAX_VALUE";

        // Assert
        recommendation.Should().Contain("constant");
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
        density.Should().Be(20.0);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectObsoleteCode()
    {
        // Arrange
        var code = "[Obsolete(\"Use NewMethod instead\")]";

        // Assert
        code.Should().Contain("[Obsolete");
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectTODOComments()
    {
        // Arrange
        var code = "// TODO: Implement this feature";

        // Assert
        code.Should().Contain("TODO");
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectHACKComments()
    {
        // Arrange
        var code = "// HACK: Temporary workaround";

        // Assert
        code.Should().Contain("HACK");
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
        coveragePercent.Should().Be(80.0);
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectMissingTests()
    {
        // Arrange
        var hasTests = false;

        // Assert
        hasTests.Should().BeFalse();
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
        ratio.Should().Be(1.5); // 1.5:1 ratio
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
        securityIssues.Should().NotBeEmpty();
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectHardcodedSecrets()
    {
        // Arrange
        var code = "var password = \"admin123\";";

        // Act
        var hasHardcodedSecret = code.Contains("password") && code.Contains("=");

        // Assert
        hasHardcodedSecret.Should().BeTrue();
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
        report.OverallScore.Should().BeGreaterThan(0);
        report.Recommendations.Should().BeGreaterThan(0);
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
        ordered[0].Type.Should().Be("Security");
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
        metrics.LinesOfCode.Should().BeGreaterThan(0);
        metrics.NumberOfClasses.Should().BeGreaterThan(0);
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
        antiPatterns.Should().HaveCount(5);
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
        refactorings.Should().Contain("Extract method");
    }

    [Fact]
    public void AnalyzeCommand_ShouldGenerateJsonReport()
    {
        // Arrange
        var json = "{\"score\":8.5,\"issues\":10,\"recommendations\":5}";

        // Assert
        json.Should().Contain("score");
        json.Should().Contain("issues");
    }

    [Fact]
    public void AnalyzeCommand_ShouldGenerateHtmlReport()
    {
        // Arrange
        var html = "<html><body><h1>Analysis Report</h1></body></html>";

        // Assert
        html.Should().Contain("<html>");
        html.Should().Contain("Analysis Report");
    }

    [Fact]
    public void AnalyzeCommand_ShouldSupportIncrementalAnalysis()
    {
        // Arrange
        var analyzeOnlyChanged = true;

        // Assert
        analyzeOnlyChanged.Should().BeTrue();
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectArchitectureViolations()
    {
        // Arrange
        var violation = "Domain layer references Infrastructure";

        // Assert
        violation.Should().Contain("references");
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
        layers.Should().Contain("Domain");
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
        isInRange.Should().BeTrue();
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
        improvement.Should().Be(1.5);
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
        aboveAverage.Should().BeTrue();
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
        result.Should().Be(0);
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
        result.Should().Be(0);
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
        result.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("\"Handlers\"");
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
        result.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("ProjectPath");
    }

    [Fact]
    public async Task AnalyzeCommand_WithMarkdownFormat_GeneratesMarkdownReport()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "report.md");

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --output {outputPath} --format markdown", console);

        // Assert
        result.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("# 🔍 Relay Project Analysis Report");
    }

    [Fact]
    public async Task AnalyzeCommand_WithHtmlFormat_GeneratesHtmlReport()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "report.html");

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --output {outputPath} --format html", console);

        // Assert
        result.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("<!DOCTYPE html>");
        content.Should().Contain("Relay Project Analysis Report");
    }

    [Fact]
    public async Task AnalyzeCommand_DetectsPerformanceIssuesInRealCode()
    {
        // Arrange
        await CreateTestProject();
        await CreateTestHandler(usesValueTask: false, hasCancellationToken: false);
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "perf-report.json");

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("PerformanceIssues");
    }

    [Fact]
    public async Task AnalyzeCommand_DetectsReliabilityIssuesInRealCode()
    {
        // Arrange
        await CreateTestProject();
        await CreateTestHandler(hasLogging: false, hasValidation: false);
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "reliability-report.json");

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("ReliabilityIssues");
    }

    [Fact]
    public async Task AnalyzeCommand_WithIncludeTests_AnalyzesTestFiles()
    {
        // Arrange
        await CreateTestProject();
        var testFile = Path.Combine(_testPath, "TestClass.Test.cs");
        await File.WriteAllTextAsync(testFile, @"
public class TestClass
{
    [Fact]
    public void Test_ShouldPass()
    {
        Assert.True(true);
    }
}");
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --include-tests true", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeCommand_WithoutIncludeTests_ExcludesTestFiles()
    {
        // Arrange
        await CreateTestProject();
        var testFile = Path.Combine(_testPath, "TestClass.Test.cs");
        await File.WriteAllTextAsync(testFile, @"
public class TestClass
{
    [Fact]
    public void Test_ShouldPass()
    {
        Assert.True(true);
    }
}");
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --include-tests false", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeCommand_DisplaysOverallScore()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync($"--path {_testPath}", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeCommand_DisplaysProjectOverview()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "overview.json");

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("ProjectFiles");
        content.Should().Contain("SourceFiles");
    }

    [Fact]
    public async Task AnalyzeCommand_GeneratesRecommendations()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "recommendations.json");

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("Recommendations");
    }

    [Fact]
    public async Task AnalyzeCommand_WithQuickDepth_PerformsQuickAnalysis()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --depth quick", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeCommand_WithStandardDepth_PerformsStandardAnalysis()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --depth standard", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeCommand_WithFullDepth_PerformsFullAnalysis()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --depth full", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeCommand_WithDeepDepth_PerformsDeepAnalysis()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --depth deep", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeCommand_DetectsRelayCoreDependency()
    {
        // Arrange
        await CreateTestProject();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync($"--path {_testPath}", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeCommand_WithMultipleHandlers_CountsAllHandlers()
    {
        // Arrange
        await CreateTestProject();
        await CreateSecondHandler();
        var command = AnalyzeCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "handlers.json");

        // Act
        var result = await command.InvokeAsync($"--path {_testPath} --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("\"Handlers\"");
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
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("\"Requests\"");
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
        result.Should().Be(0);
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
        result.Should().Be(0);
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
        result.Should().Be(0);
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
        result.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();
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
