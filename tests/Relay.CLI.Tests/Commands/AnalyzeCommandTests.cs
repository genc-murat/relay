using System.CommandLine;
using Relay.CLI.Commands;
using Relay.CLI.Commands.Models;
using Relay.CLI.Commands.Models.Performance;
using Xunit;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.CLI.Tests.Commands;

public class AnalyzeCommandTests
{
    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        // Arrange & Act
        var command = AnalyzeCommand.Create();

        // Assert
        Assert.Equal("analyze", command.Name);
        Assert.Equal("Analyze your project for performance optimization opportunities", command.Description);
    }

    [Fact]
    public void Create_CommandHasRequiredOptions()
    {
        // Arrange & Act
        var command = AnalyzeCommand.Create();

        // Assert
        Assert.Contains(command.Options, o => o.Name == "path");
        Assert.Contains(command.Options, o => o.Name == "output");
        Assert.Contains(command.Options, o => o.Name == "format");
        Assert.Contains(command.Options, o => o.Name == "depth");
        Assert.Contains(command.Options, o => o.Name == "include-tests");
    }

    [Fact]
    public async Task ExecuteAnalyze_WithValidProjectPath_CompletesSuccessfully()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayAnalyzeTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a minimal .csproj file
            var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

            // Create a handler file
            var handlerContent = @"using Relay.Core;
public class TestHandler : IRequestHandler<TestRequest, string>
{
    public ValueTask<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}
public record TestRequest : IRequest<string>;";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "TestHandler.cs"), handlerContent);

            // Act
            var result = await AnalyzeCommand.ExecuteAnalyze(tempDir, null, "console", "full", false);

            // Assert
            Assert.Equal(0, result); // Success exit code
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ExecuteAnalyze_WithInvalidPath_ReturnsErrorCode()
    {
        // Arrange
        var invalidPath = "/nonexistent/path";

        // Act
        var result = await AnalyzeCommand.ExecuteAnalyze(invalidPath, null, "console", "full", false);

        // Assert
        Assert.Equal(1, result); // Error exit code for invalid path
    }

    [Fact]
    public async Task DiscoverProjectFiles_WithValidProject_FindsFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayDiscoverTest");
        Directory.CreateDirectory(tempDir);
        var analysis = new ProjectAnalysis { ProjectPath = tempDir, IncludeTests = true };

        try
        {
            // Create test files
            var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk""></Project>";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

            var csFile = "public class Test {}";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "Test.cs"), csFile);

            // Act
            await AnalyzeCommand.DiscoverProjectFiles(analysis, null, null);

            // Assert
            Assert.Single(analysis.ProjectFiles);
            Assert.Single(analysis.SourceFiles);
            Assert.Contains("test.csproj", analysis.ProjectFiles[0]);
            Assert.Contains("Test.cs", analysis.SourceFiles[0]);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task AnalyzeHandlers_WithValidHandlers_FindsHandlers()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayHandlersTest");
        Directory.CreateDirectory(tempDir);
        var analysis = new ProjectAnalysis { ProjectPath = tempDir };

        try
        {
            // Create handler file
            var handlerContent = @"using Relay.Core;
[Handle]
public class TestHandler : IRequestHandler<TestRequest, string>
{
    private readonly ILogger _logger;
    public TestHandler(ILogger logger) => _logger = logger;

    public async ValueTask<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(""Handling request"");
        return ""test"";
    }
}
public record TestRequest : IRequest<string>;";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "TestHandler.cs"), handlerContent);
            analysis.SourceFiles.Add(Path.Combine(tempDir, "TestHandler.cs"));

            // Act
            await AnalyzeCommand.AnalyzeHandlers(analysis, null, null);

            // Assert
            Assert.Single(analysis.Handlers);
            var handler = analysis.Handlers[0];
            Assert.Equal("TestHandler", handler.Name);
            Assert.True(handler.IsAsync);
            Assert.True(handler.HasDependencies);
            Assert.True(handler.UsesValueTask);
            Assert.True(handler.HasCancellationToken);
            Assert.True(handler.HasLogging);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task AnalyzeRequests_WithValidRequests_FindsRequests()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayRequestsTest");
        Directory.CreateDirectory(tempDir);
        var analysis = new ProjectAnalysis { ProjectPath = tempDir };

        try
        {
            // Create request file
            var requestContent = @"using Relay.Core;
using System.ComponentModel.DataAnnotations;

[Authorize]
[Cacheable]
public record UserQuery(string UserId) : IRequest<UserResponse>;

public record UserResponse(string Name);

public class CreateUserCommand : IRequest<int>
{
    [Required]
    public string Name { get; set; }
    public string Email { get; set; }
}";
            await File.WriteAllTextAsync(Path.Combine(tempDir, "UserRequests.cs"), requestContent);
            analysis.SourceFiles.Add(Path.Combine(tempDir, "UserRequests.cs"));

            // Act
            await AnalyzeCommand.AnalyzeRequests(analysis, null, null);

            // Assert
            Assert.Equal(2, analysis.Requests.Count);

            var query = analysis.Requests.First(r => r.Name == "UserQuery");
            Assert.True(query.IsRecord);
            Assert.True(query.HasResponse);
            Assert.True(query.HasAuthorization);
            Assert.True(query.HasCaching);
            Assert.Equal(1, query.ParameterCount);

            var command = analysis.Requests.First(r => r.Name == "CreateUserCommand");
            Assert.False(command.IsRecord);
            Assert.True(command.HasResponse);
            Assert.True(command.HasValidation);
            Assert.Equal(2, command.ParameterCount);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CheckPerformanceOpportunities_WithIssues_FindsPerformanceProblems()
    {
        // Arrange
        var analysis = new ProjectAnalysis();
        analysis.Handlers.Add(new HandlerInfo
        {
            Name = "TaskHandler",
            UsesValueTask = false,
            HasCancellationToken = false
        });
        analysis.Handlers.Add(new HandlerInfo
        {
            Name = "QueryHandler",
            UsesValueTask = true,
            HasCancellationToken = true
        });
        analysis.Requests.Add(new RequestInfo
        {
            Name = "UserQuery",
            HasCaching = false
        });

        // Act
        await AnalyzeCommand.CheckPerformanceOpportunities(analysis, null, null);

        // Assert
        Assert.Equal(3, analysis.PerformanceIssues.Count);
        Assert.Contains(analysis.PerformanceIssues, i => i.Type == "Task Usage");
        Assert.Contains(analysis.PerformanceIssues, i => i.Type == "Cancellation Support");
        Assert.Contains(analysis.PerformanceIssues, i => i.Type == "Caching Opportunity");
    }

    [Fact]
    public async Task CheckReliabilityPatterns_WithIssues_FindsReliabilityProblems()
    {
        // Arrange
        var analysis = new ProjectAnalysis();
        analysis.Handlers.Add(new HandlerInfo
        {
            Name = "NoLoggingHandler",
            HasLogging = false
        });
        analysis.Requests.Add(new RequestInfo
        {
            Name = "NoValidationRequest",
            HasValidation = false,
            HasAuthorization = false
        });

        // Act
        await AnalyzeCommand.CheckReliabilityPatterns(analysis, null, null);

        // Assert
        Assert.Equal(3, analysis.ReliabilityIssues.Count);
        Assert.Contains(analysis.ReliabilityIssues, i => i.Type == "Logging");
        Assert.Contains(analysis.ReliabilityIssues, i => i.Type == "Validation");
        Assert.Contains(analysis.ReliabilityIssues, i => i.Type == "Authorization");
    }

    [Fact]
    public async Task AnalyzeDependencies_WithRelayProject_SetsFlags()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayDepsTest");
        Directory.CreateDirectory(tempDir);
        var analysis = new ProjectAnalysis { ProjectPath = tempDir };

        try
        {
            // Create project file with dependencies
            var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""1.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging"" Version=""8.0.0"" />
    <PackageReference Include=""FluentValidation"" Version=""11.0.0"" />
    <PackageReference Include=""StackExchange.Redis"" Version=""2.0.0"" />
  </ItemGroup>
</Project>";
            var csprojPath = Path.Combine(tempDir, "test.csproj");
            await File.WriteAllTextAsync(csprojPath, csprojContent);
            analysis.ProjectFiles.Add(csprojPath);

            // Act
            await AnalyzeCommand.AnalyzeDependencies(analysis, null, null);

            // Assert
            Assert.True(analysis.HasRelayCore, "Should detect Relay.Core dependency");
            Assert.True(analysis.HasLogging, "Should detect logging dependency");
            Assert.True(analysis.HasValidation, "Should detect validation dependency");
            Assert.True(analysis.HasCaching, "Should detect caching dependency");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GenerateRecommendations_WithIssues_CreatesRecommendations()
    {
        // Arrange
        var analysis = new ProjectAnalysis();
        analysis.PerformanceIssues.Add(new PerformanceIssue { Type = "Task Usage", Severity = "Medium" });
        analysis.ReliabilityIssues.Add(new ReliabilityIssue { Type = "Logging", Severity = "Medium" });
        analysis.Handlers.AddRange(Enumerable.Repeat(new HandlerInfo(), 25)); // More than 20 handlers

        // Act
        await AnalyzeCommand.GenerateRecommendations(analysis, null, null);

        // Assert
        Assert.Equal(4, analysis.Recommendations.Count); // Performance, Reliability, Framework (because !HasRelayCore), Architecture
        Assert.Contains(analysis.Recommendations, r => r.Category == "Performance");
        Assert.Contains(analysis.Recommendations, r => r.Category == "Reliability");
        Assert.Contains(analysis.Recommendations, r => r.Category == "Framework");
        Assert.Contains(analysis.Recommendations, r => r.Category == "Architecture");
    }

    [Fact]
    public void CalculateOverallScore_WithPerfectProject_ReturnsHighScore()
    {
        // Arrange
        var analysis = new ProjectAnalysis
        {
            HasRelayCore = true,
            HasLogging = true,
            HasValidation = true,
            HasCaching = true
        };

        // Act
        var score = AnalyzeCommand.CalculateOverallScore(analysis);

        // Assert
        Assert.True(score >= 9.0);
    }

    [Fact]
    public void CalculateOverallScore_WithIssues_ReturnsLowerScore()
    {
        // Arrange
        var analysis = new ProjectAnalysis();
        analysis.PerformanceIssues.Add(new PerformanceIssue { Severity = "High" });
        analysis.PerformanceIssues.Add(new PerformanceIssue { Severity = "Medium" });
        analysis.ReliabilityIssues.Add(new ReliabilityIssue { Severity = "High" });

        // Act
        var score = AnalyzeCommand.CalculateOverallScore(analysis);

        // Assert
        Assert.True(score < 8.0);
    }

    [Fact]
    public async Task SaveAnalysisResults_WithJsonFormat_SavesJsonFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelaySaveTest");
        Directory.CreateDirectory(tempDir);
        var outputPath = Path.Combine(tempDir, "analysis.json");
        var analysis = new ProjectAnalysis
        {
            ProjectPath = "/test/path",
            AnalysisDepth = "full",
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Act
            await AnalyzeCommand.SaveAnalysisResults(analysis, outputPath, "json");

            // Assert
            Assert.True(File.Exists(outputPath));
            var content = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("ProjectPath", content);
            Assert.Contains("AnalysisDepth", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SaveAnalysisResults_WithMarkdownFormat_SavesMarkdownFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayMarkdownTest");
        Directory.CreateDirectory(tempDir);
        var outputPath = Path.Combine(tempDir, "analysis.md");
        var analysis = new ProjectAnalysis
        {
            ProjectPath = "/test/path",
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Act
            await AnalyzeCommand.SaveAnalysisResults(analysis, outputPath, "markdown");

            // Assert
            Assert.True(File.Exists(outputPath));
            var content = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("# 🔍 Relay Project Analysis Report", content);
            Assert.Contains("Overall Score:", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SaveAnalysisResults_WithHtmlFormat_SavesHtmlFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayHtmlTest");
        Directory.CreateDirectory(tempDir);
        var outputPath = Path.Combine(tempDir, "analysis.html");
        var analysis = new ProjectAnalysis
        {
            ProjectPath = "/test/path",
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Act
            await AnalyzeCommand.SaveAnalysisResults(analysis, outputPath, "html");

            // Assert
            Assert.True(File.Exists(outputPath));
            var content = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("Relay Project Analysis Report", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void IsHandler_WithHandlerClass_ReturnsTrue()
    {
        // Arrange
        var code = @"public class TestHandler : IRequestHandler<TestRequest, string> { }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var classDecl = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();

        // Act
        var result = AnalyzeCommand.IsHandler(classDecl, code);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHandler_WithHandleAttribute_ReturnsTrue()
    {
        // Arrange
        var code = @"[Handle] public class TestHandler { }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var classDecl = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();

        // Act
        var result = AnalyzeCommand.IsHandler(classDecl, code);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRequest_WithRequestRecord_ReturnsTrue()
    {
        // Arrange
        var code = @"public record TestRequest : IRequest<string> { }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var recordDecl = tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().First();

        // Act
        var result = AnalyzeCommand.IsRequest(recordDecl, code);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasAsyncMethods_WithAsyncMethod_ReturnsTrue()
    {
        // Arrange
        var code = @"public class Test { public async Task Method() { } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var classDecl = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();

        // Act
        var result = AnalyzeCommand.HasAsyncMethods(classDecl);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void UsesValueTask_WithValueTask_ReturnsTrue()
    {
        // Arrange
        var code = @"public class Test { public ValueTask Method() { return default; } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var classDecl = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();

        // Act
        var result = AnalyzeCommand.UsesValueTask(classDecl, code);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasLogging_WithLoggerField_ReturnsTrue()
    {
        // Arrange
        var code = @"public class Test { private readonly ILogger _logger; }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var classDecl = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();

        // Act
        var result = AnalyzeCommand.HasLogging(classDecl, code);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasValidationAttributes_WithRequiredAttribute_ReturnsTrue()
    {
        // Arrange
        var code = @"public record Test { [Required] public string Name { get; set; } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var recordDecl = tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().First();

        // Act
        var result = AnalyzeCommand.HasValidationAttributes(recordDecl, code);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasAuthorizationAttributes_WithAuthorizeAttribute_ReturnsTrue()
    {
        // Arrange
        var code = @"[Authorize] public record Test { }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var recordDecl = tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().First();

        // Act
        var result = AnalyzeCommand.HasAuthorizationAttributes(recordDecl, code);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetParameterCount_WithRecordParameters_ReturnsCorrectCount()
    {
        // Arrange
        var code = @"public record Test(string Name, int Age, bool Active) { }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var recordDecl = tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().First();

        // Act
        var result = AnalyzeCommand.GetParameterCount(recordDecl);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void GetParameterCount_WithClassProperties_ReturnsCorrectCount()
    {
        // Arrange
        var code = @"public class Test { public string Name { get; set; } public int Age { get; set; } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var classDecl = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();

        // Act
        var result = AnalyzeCommand.GetParameterCount(classDecl);

        // Assert
        Assert.Equal(2, result);
    }
}