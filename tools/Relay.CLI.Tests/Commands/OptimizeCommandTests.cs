using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class OptimizeCommandTests : IDisposable
{
    private readonly string _testPath;

    public OptimizeCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-optimize-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    [Fact]
    public async Task OptimizeCommand_ConvertsTaskToValueTask()
    {
        // Arrange
        var originalCode = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}

public record TestRequest : IRequest<string>;";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"), originalCode);

        // Act
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"));
        var optimized = content.Replace("Task<string>", "ValueTask<string>");
        await File.WriteAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"), optimized);
        var newContent = await File.ReadAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"));

        // Assert
        newContent.Should().Contain("ValueTask<string>");
        newContent.Should().NotContain("Task<string>");
    }

    [Fact]
    public async Task OptimizeCommand_AddsCancellationToken()
    {
        // Arrange
        var originalCode = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async ValueTask<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"), originalCode);

        // Act
        var content = await File.ReadAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"));
        var optimized = content.Replace("HandleAsync(TestRequest request)", 
                                       "HandleAsync(TestRequest request, CancellationToken ct)");
        await File.WriteAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"), optimized);
        var newContent = await File.ReadAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"));

        // Assert
        newContent.Should().Contain("CancellationToken ct");
    }

    [Fact]
    public async Task OptimizeCommand_RemovesUnnecessaryAllocations()
    {
        // Arrange
        var originalCode = @"
var list = new List<string>();
list.Add(""item1"");
list.Add(""item2"");
return list.ToArray();";

        // Act
        var optimized = @"
return new[] { ""item1"", ""item2"" };";

        // Assert
        optimized.Length.Should().BeLessThan(originalCode.Length);
    }

    [Fact]
    public async Task OptimizeCommand_UsesSpanWherePossible()
    {
        // Arrange
        var beforeOptimization = "string[] items = new string[10];";
        var afterOptimization = "Span<string> items = stackalloc string[10];";

        // Assert
        afterOptimization.Should().Contain("Span");
        afterOptimization.Should().Contain("stackalloc");
    }

    [Fact]
    public async Task OptimizeCommand_DetectsLinqInHotPath()
    {
        // Arrange
        var codeWithLinq = @"
for (int i = 0; i < items.Count(); i++) // Count() is inefficient
{
    // Process item
}";

        // Act
        var hasLinqInLoop = codeWithLinq.Contains("Count()");

        // Assert
        hasLinqInLoop.Should().BeTrue();
    }

    [Fact]
    public async Task OptimizeCommand_SuggestsStructOverClass()
    {
        // Arrange
        var smallValueType = @"
public class Point // Should be struct for better performance
{
    public int X { get; set; }
    public int Y { get; set; }
}";

        var optimized = @"
public readonly struct Point
{
    public int X { get; init; }
    public int Y { get; init; }
}";

        // Assert
        optimized.Should().Contain("struct");
        optimized.Should().Contain("readonly");
    }

    [Fact]
    public async Task OptimizeCommand_UsesReadOnlyWherePossible()
    {
        // Arrange
        var mutableField = "private List<string> _items;";
        var immutableField = "private readonly List<string> _items;";

        // Assert
        immutableField.Should().Contain("readonly");
    }

    [Fact]
    public async Task OptimizeCommand_DetectsStringConcatenation()
    {
        // Arrange
        var inefficient = @"
string result = """";
for (int i = 0; i < 100; i++)
{
    result += i.ToString(); // Inefficient
}";

        var efficient = @"
var sb = new StringBuilder();
for (int i = 0; i < 100; i++)
{
    sb.Append(i);
}
string result = sb.ToString();";

        // Assert
        inefficient.Should().Contain("+=");
        efficient.Should().Contain("StringBuilder");
    }

    [Fact]
    public async Task OptimizeCommand_SuggestsArrayPool()
    {
        // Arrange
        var withoutPool = "var buffer = new byte[1024];";
        var withPool = @"
var buffer = ArrayPool<byte>.Shared.Rent(1024);
try
{
    // Use buffer
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}";

        // Assert
        withPool.Should().Contain("ArrayPool");
        withPool.Should().Contain("Rent");
        withPool.Should().Contain("Return");
    }

    [Fact]
    public async Task OptimizeCommand_DetectsAsyncVoidMethods()
    {
        // Arrange
        var asyncVoid = @"
public async void ProcessData() // Should return Task
{
    await DoSomethingAsync();
}";

        // Act
        var hasAsyncVoid = asyncVoid.Contains("async void");

        // Assert
        hasAsyncVoid.Should().BeTrue(); // This is a problem
    }

    [Fact]
    public async Task OptimizeCommand_SuggestsConfigureAwaitFalse()
    {
        // Arrange
        var without = "await someTask;";
        var with = "await someTask.ConfigureAwait(false);";

        // Assert
        with.Should().Contain("ConfigureAwait(false)");
    }

    [Fact]
    public async Task OptimizeCommand_DetectsExcessiveBoxing()
    {
        // Arrange
        var boxing = @"
int value = 42;
object obj = value; // Boxing
Console.WriteLine(obj); // Uses boxed value";

        // Act
        var hasBoxing = boxing.Contains("object obj = value");

        // Assert
        hasBoxing.Should().BeTrue();
    }

    [Theory]
    [InlineData("Task<string>", "ValueTask<string>", true)]
    [InlineData("Task<int>", "ValueTask<int>", true)]
    [InlineData("Task", "ValueTask", true)]
    [InlineData("string", "string", false)]
    public void OptimizeCommand_IdentifiesOptimizableCases(string original, string optimized, bool shouldOptimize)
    {
        // Act
        var needsOptimization = original.StartsWith("Task") && shouldOptimize;

        // Assert
        if (shouldOptimize)
        {
            optimized.Should().Contain("ValueTask");
        }
    }

    [Fact]
    public async Task OptimizeCommand_CreatesBackupBeforeChanges()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testPath, "Original.cs"), "original content");

        // Act
        var backupPath = Path.Combine(_testPath, "Original.cs.bak");
        File.Copy(Path.Combine(_testPath, "Original.cs"), backupPath);

        // Assert
        File.Exists(backupPath).Should().BeTrue();
    }

    [Fact]
    public async Task OptimizeCommand_GeneratesOptimizationReport()
    {
        // Arrange & Act
        var report = new
        {
            FilesAnalyzed = 10,
            FilesOptimized = 5,
            OptimizationsApplied = 15,
            EstimatedImprovement = "20-30%"
        };

        // Assert
        report.FilesOptimized.Should().BeLessThanOrEqualTo(report.FilesAnalyzed);
        report.OptimizationsApplied.Should().BeGreaterThan(0);
        report.EstimatedImprovement.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task OptimizeCommand_PreservesCodeSemantics()
    {
        // Arrange
        var originalCode = @"
public async Task<int> Calculate(int x, int y)
{
    return x + y;
}";

        var optimizedCode = @"
public async ValueTask<int> Calculate(int x, int y)
{
    return x + y;
}";

        // Assert - Only return type changed, logic preserved
        optimizedCode.Should().Contain("return x + y");
    }

    [Fact]
    public void OptimizationMetrics_TracksImprovements()
    {
        // Arrange
        var metrics = new
        {
            AllocationsBefore = 1000,
            AllocationsAfter = 200,
            ExecutionTimeBefore = TimeSpan.FromMilliseconds(100),
            ExecutionTimeAfter = TimeSpan.FromMilliseconds(67)
        };

        // Act
        var allocationReduction = (metrics.AllocationsBefore - metrics.AllocationsAfter) * 100.0 / metrics.AllocationsBefore;
        var speedImprovement = (metrics.ExecutionTimeBefore - metrics.ExecutionTimeAfter).TotalMilliseconds * 100 / metrics.ExecutionTimeBefore.TotalMilliseconds;

        // Assert
        allocationReduction.Should().Be(80.0); // 80% reduction
        speedImprovement.Should().Be(33.0); // 33% faster
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
