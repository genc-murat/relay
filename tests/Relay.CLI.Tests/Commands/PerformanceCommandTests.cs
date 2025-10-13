using System.CommandLine;
using Relay.CLI.Commands;
using Relay.CLI.Commands.Models.Performance;
using Xunit;
using Xunit.Abstractions;

namespace Relay.CLI.Tests.Commands;

public class PerformanceCommandTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _tempDirectory;

    public PerformanceCommandTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void Create_ReturnsCommandWithCorrectNameAndDescription()
    {
        // Arrange & Act
        var command = PerformanceCommand.Create();

        // Assert
        Assert.Equal("performance", command.Name);
        Assert.Equal("Performance analysis and recommendations", command.Description);
    }

    [Fact]
    public void Create_HasAllRequiredOptions()
    {
        // Arrange & Act
        var command = PerformanceCommand.Create();

        // Assert
        var pathOption = command.Options.FirstOrDefault(o => o.Name == "path");
        var reportOption = command.Options.FirstOrDefault(o => o.Name == "report");
        var detailedOption = command.Options.FirstOrDefault(o => o.Name == "detailed");
        var outputOption = command.Options.FirstOrDefault(o => o.Name == "output");

        Assert.NotNull(pathOption);
        Assert.NotNull(reportOption);
        Assert.NotNull(detailedOption);
        Assert.NotNull(outputOption);
    }

    [Fact]
    public void GetPriorityOrder_ReturnsCorrectOrder()
    {
        // Act & Assert
        Assert.Equal(1, PerformanceCommand.GetPriorityOrder("High"));
        Assert.Equal(2, PerformanceCommand.GetPriorityOrder("Medium"));
        Assert.Equal(3, PerformanceCommand.GetPriorityOrder("Low"));
        Assert.Equal(4, PerformanceCommand.GetPriorityOrder("Unknown"));
    }

    [Fact]
    public async Task AnalyzeProjectStructure_WithValidProject_FindsProjectFiles()
    {
        // Arrange
        var analysis = new PerformanceAnalysis();
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Create a sample .csproj file
        var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <TieredPGO>true</TieredPGO>
    <Optimize>true</Optimize>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""1.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "TestProject.csproj"), csprojContent);

        // Act
        await PerformanceCommand.AnalyzeProjectStructure(projectPath, analysis);

        // Assert
        Assert.Equal(1, analysis.ProjectCount);
        Assert.True(analysis.HasRelay);
        Assert.True(analysis.HasPGO);
        Assert.True(analysis.HasOptimizations);
        Assert.True(analysis.ModernFramework);
    }

    [Fact]
    public async Task AnalyzeProjectStructure_WithOldFramework_SetsModernFrameworkFalse()
    {
        // Arrange
        var analysis = new PerformanceAnalysis();
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Create a sample .csproj file with old framework
        var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "TestProject.csproj"), csprojContent);

        // Act
        await PerformanceCommand.AnalyzeProjectStructure(projectPath, analysis);

        // Assert
        Assert.False(analysis.ModernFramework);
    }

    [Fact]
    public async Task AnalyzeAsyncPatterns_WithAsyncMethods_CountsCorrectly()
    {
        // Arrange
        var analysis = new PerformanceAnalysis();
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Create a sample C# file with async methods
        var csContent = @"
using System.Threading.Tasks;

public class TestHandler
{
    public async Task HandleAsync()
    {
        await Task.Delay(100);
    }

    public async ValueTask HandleValueAsync()
    {
        await Task.CompletedTask;
    }

    public async Task HandleWithCancellationAsync(System.Threading.CancellationToken token)
    {
        await Task.Delay(100, token).ConfigureAwait(false);
    }

    public void SyncMethod()
    {
        // Not async
    }
}";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "TestHandler.cs"), csContent);

        // Act
        await PerformanceCommand.AnalyzeAsyncPatterns(projectPath, analysis);

        // Assert
        Assert.Equal(3, analysis.AsyncMethodCount);
        Assert.Equal(1, analysis.ValueTaskCount);
        Assert.Equal(2, analysis.TaskCount);
        Assert.Equal(1, analysis.CancellationTokenCount);
        Assert.Equal(1, analysis.ConfigureAwaitCount);
    }

    [Fact]
    public async Task AnalyzeMemoryPatterns_WithMemoryIssues_CountsCorrectly()
    {
        // Arrange
        var analysis = new PerformanceAnalysis();
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Create a sample C# file with memory patterns
        var csContent = @"
using System.Text;

public record TestRecord(string Name, int Value);

public struct TestStruct
{
    public int Value;
}

public class TestClass
{
    public void BadStringConcat()
    {
        string result = """";
        for (int i = 0; i < 10; i++)
        {
            result += i.ToString(); // Bad practice
        }
    }

    public void GoodStringBuilder()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 10; i++)
        {
            sb.Append(i);
        }
    }

    public void LinqUsage()
    {
        var list = new List<int> { 1, 2, 3 };
        var result = list.Where(x => x > 1).Select(x => x * 2).ToList();
    }
}";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "TestClass.cs"), csContent);

        // Act
        await PerformanceCommand.AnalyzeMemoryPatterns(projectPath, analysis);

        // Assert
        Assert.Equal(1, analysis.RecordCount);
        Assert.Equal(1, analysis.StructCount);
        Assert.Equal(1, analysis.LinqUsageCount);
        Assert.Equal(1, analysis.StringBuilderCount);
        Assert.Equal(1, analysis.StringConcatInLoopCount);
    }

    [Fact]
    public async Task AnalyzeHandlerPerformance_WithHandlers_CountsCorrectly()
    {
        // Arrange
        var analysis = new PerformanceAnalysis();
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Create sample handler files
        var handlerContent = @"
using Relay.Core;

[Handle]
public class TestHandler : IRequestHandler<TestRequest, TestResponse>
{
    private readonly ICache _cache;

    public async Task<TestResponse> Handle(TestRequest request)
    {
        return await _cache.GetOrSetAsync(request.Id.ToString(), () => Task.FromResult(new TestResponse()));
    }
}

public class SimpleHandler : INotificationHandler<TestNotification>
{
    public async Task Handle(TestNotification notification)
    {
        await Task.CompletedTask;
    }
}";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "TestHandler.cs"), handlerContent);

        // Act
        await PerformanceCommand.AnalyzeHandlerPerformance(projectPath, analysis);

        // Assert
        Assert.Equal(2, analysis.HandlerCount);
        Assert.Equal(1, analysis.OptimizedHandlerCount);
        Assert.Equal(1, analysis.CachedHandlerCount);
    }

    [Fact]
    public async Task GenerateRecommendations_WithPoorPerformance_GeneratesRecommendations()
    {
        // Arrange
        var analysis = new PerformanceAnalysis
        {
            HasRelay = false,
            ModernFramework = false,
            HasPGO = false,
            HasOptimizations = false,
            TaskCount = 10,
            ValueTaskCount = 2,
            StringConcatInLoopCount = 3,
            HandlerCount = 5,
            OptimizedHandlerCount = 1,
            CachedHandlerCount = 0
        };

        // Act
        await PerformanceCommand.GenerateRecommendations(analysis);

        // Assert
        Assert.True(analysis.PerformanceScore < 100);
        Assert.NotEmpty(analysis.Recommendations);

        // Check specific recommendations
        var pgoRec = analysis.Recommendations.FirstOrDefault(r => r.Title.Contains("PGO"));
        var valueTaskRec = analysis.Recommendations.FirstOrDefault(r => r.Title.Contains("ValueTask"));
        var stringConcatRec = analysis.Recommendations.FirstOrDefault(r => r.Title.Contains("string concatenation"));
        var handleRec = analysis.Recommendations.FirstOrDefault(r => r.Title.Contains("[Handle]"));

        Assert.NotNull(pgoRec);
        Assert.NotNull(valueTaskRec);
        Assert.NotNull(stringConcatRec);
        Assert.NotNull(handleRec);

        Assert.Equal("High", pgoRec.Priority);
        Assert.Equal("High", stringConcatRec.Priority);
        Assert.Equal("Medium", valueTaskRec.Priority);
        Assert.Equal("Medium", handleRec.Priority);
    }

    [Fact]
    public async Task GenerateRecommendations_WithGoodPerformance_NoRecommendations()
    {
        // Arrange
        var analysis = new PerformanceAnalysis
        {
            HasRelay = true,
            ModernFramework = true,
            HasPGO = true,
            HasOptimizations = true,
            TaskCount = 2,
            ValueTaskCount = 10,
            StringConcatInLoopCount = 0,
            HandlerCount = 5,
            OptimizedHandlerCount = 5,
            CachedHandlerCount = 3
        };

        // Act
        await PerformanceCommand.GenerateRecommendations(analysis);

        // Assert
        Assert.True(analysis.PerformanceScore > 80);
        Assert.Empty(analysis.Recommendations);
    }

    [Fact]
    public async Task GeneratePerformanceReport_CreatesValidMarkdownReport()
    {
        // Arrange
        var analysis = new PerformanceAnalysis
        {
            ProjectCount = 2,
            HandlerCount = 5,
            OptimizedHandlerCount = 3,
            AsyncMethodCount = 10,
            ValueTaskCount = 7,
            TaskCount = 3,
            ModernFramework = true,
            HasPGO = true,
            PerformanceScore = 85
        };
        analysis.Recommendations.Add(new PerformanceRecommendation
        {
            Category = "Build Configuration",
            Priority = "High",
            Title = "Enable PGO",
            Description = "Add TieredPGO to improve performance",
            Impact = "10-20% improvement"
        });

        var reportPath = Path.Combine(_tempDirectory, "performance-report.md");

        // Act
        await PerformanceCommand.GeneratePerformanceReport(analysis, reportPath);

        // Assert
        Assert.True(File.Exists(reportPath));
        var content = await File.ReadAllTextAsync(reportPath);

        Assert.Contains("# Performance Analysis Report", content);
        Assert.Contains("Performance Score: 85/100", content);
        Assert.Contains("| Projects | 2 |", content);
        Assert.Contains("| Handlers | 5 |", content);
        Assert.Contains("## Recommendations", content);
        Assert.Contains("Enable PGO", content);
        Assert.Contains("*Generated by Relay CLI*", content);
    }

    [Fact]
    public async Task ExecutePerformance_WithValidProject_CompletesSuccessfully()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Create a minimal project structure
        var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "TestProject.csproj"), csprojContent);

        var csContent = @"
public class TestHandler
{
    public async System.Threading.Tasks.Task HandleAsync()
    {
        await System.Threading.Tasks.Task.CompletedTask;
    }
}";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "TestHandler.cs"), csContent);

        // Act & Assert - Should not throw
        await PerformanceCommand.ExecutePerformance(projectPath, false, false, null);
    }

    [Fact]
    public async Task ExecutePerformance_WithReportGeneration_CreatesReportFile()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);
        var reportPath = Path.Combine(_tempDirectory, "custom-report.md");

        // Create minimal project
        var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "TestProject.csproj"), csprojContent);

        // Act
        await PerformanceCommand.ExecutePerformance(projectPath, true, false, reportPath);

        // Assert
        Assert.True(File.Exists(reportPath));
    }

    [Fact]
    public async Task ExecutePerformance_WithDetailedFlag_IncludesDetailedMetrics()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Create project with detailed metrics
        var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "TestProject.csproj"), csprojContent);

        var csContent = @"
using System.Linq;

public record TestRecord(string Name);
public struct TestStruct { public int Value; }

public class TestClass
{
    public void TestLinq()
    {
        var list = new System.Collections.Generic.List<int> { 1, 2, 3 };
        var result = list.Where(x => x > 1).ToList();
    }
}";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "TestClass.cs"), csContent);

        // Act & Assert - Should not throw and complete successfully
        await PerformanceCommand.ExecutePerformance(projectPath, false, true, null);
    }

    [Fact]
    public async Task AnalyzeProjectStructure_WithEmptyDirectory_SetsZeroCounts()
    {
        // Arrange
        var analysis = new PerformanceAnalysis();
        var projectPath = Path.Combine(_tempDirectory, "EmptyProject");
        Directory.CreateDirectory(projectPath);

        // Act
        await PerformanceCommand.AnalyzeProjectStructure(projectPath, analysis);

        // Assert
        Assert.Equal(0, analysis.ProjectCount);
        Assert.False(analysis.HasRelay);
        Assert.False(analysis.HasPGO);
        Assert.False(analysis.HasOptimizations);
        Assert.False(analysis.ModernFramework);
    }

    [Fact]
    public async Task AnalyzeAsyncPatterns_WithNoAsyncMethods_SetsZeroCounts()
    {
        // Arrange
        var analysis = new PerformanceAnalysis();
        var projectPath = Path.Combine(_tempDirectory, "SyncProject");
        Directory.CreateDirectory(projectPath);

        // Create synchronous code only
        var csContent = @"
public class SyncClass
{
    public void SyncMethod()
    {
        // Synchronous method
    }
}";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "SyncClass.cs"), csContent);

        // Act
        await PerformanceCommand.AnalyzeAsyncPatterns(projectPath, analysis);

        // Assert
        Assert.Equal(0, analysis.AsyncMethodCount);
        Assert.Equal(0, analysis.ValueTaskCount);
        Assert.Equal(0, analysis.TaskCount);
        Assert.Equal(0, analysis.CancellationTokenCount);
        Assert.Equal(0, analysis.ConfigureAwaitCount);
    }

    [Fact]
    public async Task AnalyzeMemoryPatterns_WithGoodPatterns_SetsAppropriateCounts()
    {
        // Arrange
        var analysis = new PerformanceAnalysis();
        var projectPath = Path.Combine(_tempDirectory, "GoodMemoryProject");
        Directory.CreateDirectory(projectPath);

        // Create code with good memory patterns
        var csContent = @"
using System.Text;

public record GoodRecord(string Name, int Value);

public struct GoodStruct
{
    public int Value;
}

public class GoodClass
{
    public void GoodStringHandling()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 10; i++)
        {
            sb.Append(i.ToString());
        }
        var result = sb.ToString();
    }
}";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "GoodClass.cs"), csContent);

        // Act
        await PerformanceCommand.AnalyzeMemoryPatterns(projectPath, analysis);

        // Assert
        Assert.Equal(1, analysis.RecordCount);
        Assert.Equal(1, analysis.StructCount);
        Assert.Equal(1, analysis.StringBuilderCount);
        Assert.Equal(0, analysis.StringConcatInLoopCount);
    }
}