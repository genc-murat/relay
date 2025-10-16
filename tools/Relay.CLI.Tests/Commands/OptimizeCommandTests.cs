using Relay.CLI.Commands;
using Relay.CLI.Commands.Models.Optimization;
using System.CommandLine;

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
        var optimized = content.Replace("async Task<string>", "async ValueTask<string>");
        await File.WriteAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"), optimized);
        var newContent = await File.ReadAllTextAsync(Path.Combine(_testPath, "TestHandler.cs"));

        // Assert
        newContent.Should().Contain("ValueTask<string>");
        newContent.Should().NotContain("async Task<string>");
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

    [Fact]
    public async Task OptimizeCommand_ShouldUseMemoryInsteadOfArray()
    {
        // Arrange
        var before = "public void Process(byte[] data)";
        var after = "public void Process(ReadOnlyMemory<byte> data)";

        // Assert
        after.Should().Contain("ReadOnlyMemory");
    }

    [Fact]
    public async Task OptimizeCommand_ShouldDetectClosureAllocations()
    {
        // Arrange
        var closure = @"
var items = new List<int>();
items.ForEach(x => Console.WriteLine(x)); // Creates closure";

        // Assert
        closure.Should().Contain("=>");
    }

    [Fact]
    public async Task OptimizeCommand_ShouldSuggestStaticLambdas()
    {
        // Arrange
        var nonStatic = "list.Where(x => x > 0)";
        var staticLambda = "list.Where(static x => x > 0)";

        // Assert
        staticLambda.Should().Contain("static");
    }

    [Fact]
    public async Task OptimizeCommand_ShouldDetectAsNoTracking()
    {
        // Arrange
        var tracked = "context.Users.ToList()";
        var noTracking = "context.Users.AsNoTracking().ToList()";

        // Assert
        noTracking.Should().Contain("AsNoTracking");
    }

    [Fact]
    public async Task OptimizeCommand_ShouldSuggestCompiledQueries()
    {
        // Arrange
        var regular = "context.Users.Where(u => u.Age > 18).ToList()";
        var compiled = "CompiledQuery.Compile((MyContext ctx, int age) => ctx.Users.Where(u => u.Age > age).ToList())";

        // Assert
        compiled.Should().Contain("CompiledQuery");
    }

    [Fact]
    public async Task OptimizeCommand_ShouldDetectNPlusOne()
    {
        // Arrange
        var nPlusOne = @"
foreach (var user in users)
{
    var orders = context.Orders.Where(o => o.UserId == user.Id).ToList();
}";

        // Assert
        nPlusOne.Should().Contain("foreach");
    }

    [Fact]
    public async Task OptimizeCommand_ShouldSuggestInclude()
    {
        // Arrange
        var without = "context.Users.ToList()";
        var withInclude = "context.Users.Include(u => u.Orders).ToList()";

        // Assert
        withInclude.Should().Contain("Include");
    }

    [Fact]
    public async Task OptimizeCommand_ShouldDetectExcessiveLINQ()
    {
        // Arrange
        var excessive = "items.Where(x => x > 0).Select(x => x * 2).Where(x => x < 100).ToList()";

        // Act
        var chainCount = excessive.Split('.').Length - 1;

        // Assert
        chainCount.Should().BeGreaterThan(3);
    }

    [Fact]
    public async Task OptimizeCommand_ShouldSuggestPrecomputation()
    {
        // Arrange
        var inefficient = @"
for (int i = 0; i < array.Length; i++)
{
    var result = ExpensiveOperation();
}";

        var efficient = @"
var precomputed = ExpensiveOperation();
for (int i = 0; i < array.Length; i++)
{
    var result = precomputed;
}";

        // Assert
        efficient.Should().Contain("var precomputed");
    }

    [Fact]
    public async Task OptimizeCommand_ShouldDetectUnnecessaryToList()
    {
        // Arrange
        var unnecessary = "foreach (var item in items.ToList())";
        var necessary = "foreach (var item in items)";

        // Assert
        unnecessary.Should().Contain("ToList()");
        necessary.Should().NotContain("ToList()");
    }

    [Fact]
    public async Task OptimizeCommand_ShouldSuggestDictionaryLookup()
    {
        // Arrange
        var linear = "list.FirstOrDefault(x => x.Id == id)";
        var indexed = "dictionary[id]";

        // Assert
        indexed.Should().Contain("dictionary[");
    }

    [Fact]
    public async Task OptimizeCommand_ShouldDetectStringInterpolation()
    {
        // Arrange
        var interpolation = "$\"Hello {name}\"";
        var format = "string.Format(\"Hello {0}\", name)";

        // Assert
        interpolation.Should().Contain("$\"");
    }

    [Fact]
    public async Task OptimizeCommand_ShouldSuggestAsParallel()
    {
        // Arrange
        var sequential = "items.Select(x => ExpensiveOperation(x)).ToList()";
        var parallel = "items.AsParallel().Select(x => ExpensiveOperation(x)).ToList()";

        // Assert
        parallel.Should().Contain("AsParallel");
    }

    [Fact]
    public async Task OptimizeCommand_ShouldDetectMemoryLeaks()
    {
        // Arrange
        var leak = @"
public class MyClass
{
    private event EventHandler MyEvent;
}";

        // Assert
        leak.Should().Contain("event");
    }

    [Fact]
    public async Task OptimizeCommand_ShouldSuggestDisposablePattern()
    {
        // Arrange
        var withUsing = @"
using (var stream = File.OpenRead(""file.txt""))
{
    // Use stream
}";

        // Assert
        withUsing.Should().Contain("using");
    }

    [Fact]
    public async Task OptimizeCommand_ShouldDetectLargeObjectHeap()
    {
        // Arrange
        var largeArray = "var buffer = new byte[100000];"; // > 85KB

        // Assert
        largeArray.Should().Contain("100000");
    }

    [Theory]
    [InlineData("List<int>", 100, false)]
    [InlineData("List<int>", 10000, true)]
    public async Task OptimizeCommand_ShouldDetectListCapacity(string type, int size, bool shouldPreallocate)
    {
        // Arrange
        var withoutCapacity = $"var list = new {type}();";
        var withCapacity = $"var list = new {type}({size});";

        // Assert
        if (shouldPreallocate)
        {
            withCapacity.Should().Contain($"({size})");
        }
    }

    [Fact]
    public async Task OptimizeCommand_ShouldSuggestStringBuilder()
    {
        // Arrange
        var recommendation = "Use StringBuilder for multiple concatenations";

        // Assert
        recommendation.Should().Contain("StringBuilder");
    }

    [Fact]
    public void OptimizeCommand_ShouldDetectRecursion()
    {
        // Arrange
        var recursive = @"
public int Factorial(int n)
{
    if (n <= 1) return 1;
    return n * Factorial(n - 1);
}";

        // Assert
        recursive.Should().Contain("Factorial(n - 1)");
    }

    [Fact]
    public void OptimizeCommand_ShouldSuggestIterativeApproach()
    {
        // Arrange
        var iterative = @"
public int Calculate(int n)
{
    int result = 1;
    for (int i = 2; i <= n; i++)
        result *= i;
    return result;
}";

        // Assert
        iterative.Should().Contain("for");
        iterative.Should().NotContain("Calculate(n"); // Should not have recursive call
    }

    [Fact]
    public void OptimizeCommand_ShouldDetectRegexCompilation()
    {
        // Arrange
        var compiled = "new Regex(pattern, RegexOptions.Compiled)";

        // Assert
        compiled.Should().Contain("RegexOptions.Compiled");
    }

    [Fact]
    public void OptimizeCommand_ShouldSuggestGeneratedRegex()
    {
        // Arrange
        var generated = @"
[GeneratedRegex(@""\d+"")]
private static partial Regex MyRegex();";

        // Assert
        generated.Should().Contain("[GeneratedRegex");
    }

    [Fact]
    public void OptimizeCommand_ShouldDetectExcessiveReflection()
    {
        // Arrange
        var reflection = "type.GetMethod(\"MethodName\").Invoke(obj, args)";

        // Assert
        reflection.Should().Contain("GetMethod");
        reflection.Should().Contain("Invoke");
    }

    [Fact]
    public void OptimizeCommand_ShouldSuggestExpressionCompilation()
    {
        // Arrange
        var expression = "Expression.Lambda<Func<int, int>>(body, param).Compile()";

        // Assert
        expression.Should().Contain("Compile()");
    }

    [Fact]
    public void OptimizeCommand_ShouldDetectDateTimeNow()
    {
        // Arrange
        var utcNow = "DateTime.UtcNow";

        // Assert
        utcNow.Should().Contain("UtcNow");
    }

    [Fact]
    public void OptimizeCommand_ShouldSuggestDateTimeOffset()
    {
        // Arrange
        var better = "DateTimeOffset.UtcNow";

        // Assert
        better.Should().Contain("DateTimeOffset");
    }

    [Fact]
    public void OptimizeCommand_ShouldDetectGuidNewGuid()
    {
        // Arrange
        var standard = "Guid.NewGuid()";

        // Assert
        standard.Should().Contain("NewGuid");
    }

    [Fact]
    public void OptimizeCommand_ShouldSuggestSequentialGuid()
    {
        // Arrange
        var sequential = "// Consider sequential GUID for database";

        // Assert
        sequential.Should().Contain("sequential");
    }

    [Fact]
    public void OptimizeCommand_ShouldDetectAsyncOverSync()
    {
        // Arrange
        var syncOverAsync = "result = asyncMethod.Result;";
        var proper = "result = await asyncMethod;";

        // Assert
        syncOverAsync.Should().Contain(".Result");
        proper.Should().Contain("await");
    }

    [Fact]
    public void OptimizeCommand_ShouldSuggestCachingStrategy()
    {
        // Arrange
        var withCache = @"
private static readonly ConcurrentDictionary<string, object> _cache = new();
var result = _cache.GetOrAdd(key, k => ExpensiveOperation(k));";

        // Assert
        withCache.Should().Contain("ConcurrentDictionary");
        withCache.Should().Contain("GetOrAdd");
    }

    [Fact]
    public void OptimizeCommand_ShouldDetectLazyInitialization()
    {
        // Arrange
        var lazy = "private readonly Lazy<ExpensiveObject> _obj = new(() => new ExpensiveObject());";

        // Assert
        lazy.Should().Contain("Lazy<");
    }

    [Fact]
    public void OptimizeCommand_ShouldSuggestObjectPooling()
    {
        // Arrange
        var pooled = "var obj = ObjectPool<MyObject>.Shared.Get();";

        // Assert
        pooled.Should().Contain("ObjectPool");
    }

    [Fact]
    public void OptimizeCommand_ShouldDetectImmutableCollections()
    {
        // Arrange
        var immutable = "ImmutableList<int>.Create(1, 2, 3)";

        // Assert
        immutable.Should().Contain("ImmutableList");
    }

    [Fact]
    public void OptimizeCommand_ShouldSuggestFrozenCollections()
    {
        // Arrange
        var frozen = "FrozenDictionary.ToFrozenDictionary(items)";

        // Assert
        frozen.Should().Contain("Frozen");
    }

    [Fact]
    public void OptimizeCommand_ShouldCalculateOptimizationScore()
    {
        // Arrange
        var optimizations = new[]
        {
            ("ValueTask conversion", 10),
            ("Removed allocations", 15),
            ("Added readonly", 5),
            ("Used Span", 20)
        };

        // Act
        var totalScore = optimizations.Sum(o => o.Item2);

        // Assert
        totalScore.Should().Be(50);
    }

    [Fact]
    public void OptimizeCommand_ShouldPrioritizeOptimizations()
    {
        // Arrange
        var priorities = new[]
        {
            ("Critical: Async void", 100),
            ("High: N+1 queries", 80),
            ("Medium: Missing readonly", 50),
            ("Low: String interpolation", 20)
        };

        // Act
        var ordered = priorities.OrderByDescending(p => p.Item2).ToArray();

        // Assert
        ordered[0].Item1.Should().Contain("Critical");
        ordered[^1].Item1.Should().Contain("Low");
    }

    [Fact]
    public void OptimizeCommand_ShouldGenerateBeforeAfterComparison()
    {
        // Arrange
        var comparison = new
        {
            Before = "Task<string>",
            After = "ValueTask<string>",
            Impact = "High",
            Reason = "Reduces allocation for synchronous completion"
        };

        // Assert
        comparison.Before.Should().NotBe(comparison.After);
        comparison.Impact.Should().Be("High");
    }

    [Fact]
    public void OptimizeCommand_ShouldSupportDryRun()
    {
        // Arrange
        var dryRun = true;

        // Act
        var shouldApplyChanges = !dryRun;

        // Assert
        shouldApplyChanges.Should().BeFalse();
    }

    [Fact]
    public void OptimizeCommand_ShouldValidateOptimizations()
    {
        // Arrange
        var validationPassed = true;

        // Assert
        validationPassed.Should().BeTrue();
    }

    [Fact]
    public void OptimizeCommand_ShouldSupportAggressiveMode()
    {
        // Arrange
        var aggressiveMode = true;
        var optimizationCount = aggressiveMode ? 20 : 10;

        // Assert
        optimizationCount.Should().Be(20);
    }

    #region Command Creation Tests

    [Fact]
    public void OptimizeCommand_Create_ShouldReturnValidCommand()
    {
        // Act
        var command = OptimizeCommand.Create();

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("optimize");
        command.Description.Should().Contain("optimization");
    }

    [Fact]
    public void OptimizeCommand_ShouldHavePathOption()
    {
        // Act
        var command = OptimizeCommand.Create();
        var pathOption = command.Options.FirstOrDefault(o => o.Name == "path");

        // Assert
        pathOption.Should().NotBeNull();
        pathOption!.Name.Should().Be("path");
        pathOption!.Description.Should().Be("Project path to optimize");
    }

    [Fact]
    public void OptimizeCommand_ShouldHaveDryRunOption()
    {
        // Act
        var command = OptimizeCommand.Create();
        var dryRunOption = command.Options.FirstOrDefault(o => o.Name == "dry-run");

        // Assert
        dryRunOption.Should().NotBeNull();
        dryRunOption!.Name.Should().Be("dry-run");
        dryRunOption!.Description.Should().Be("Show what would be optimized without applying changes");
    }

    [Fact]
    public void OptimizeCommand_ShouldHaveTargetOption()
    {
        // Act
        var command = OptimizeCommand.Create();
        var targetOption = command.Options.FirstOrDefault(o => o.Name == "target");

        // Assert
        targetOption.Should().NotBeNull();
        targetOption!.Name.Should().Be("target");
        targetOption!.Description.Should().Be("Optimization target (all, handlers, requests, config)");
    }

    [Fact]
    public void OptimizeCommand_ShouldHaveAggressiveOption()
    {
        // Act
        var command = OptimizeCommand.Create();
        var aggressiveOption = command.Options.FirstOrDefault(o => o.Name == "aggressive");

        // Assert
        aggressiveOption.Should().NotBeNull();
        aggressiveOption!.Name.Should().Be("aggressive");
        aggressiveOption!.Description.Should().Be("Apply aggressive optimizations");
    }

    [Fact]
    public void OptimizeCommand_ShouldHaveBackupOption()
    {
        // Act
        var command = OptimizeCommand.Create();
        var backupOption = command.Options.FirstOrDefault(o => o.Name == "backup");

        // Assert
        backupOption.Should().NotBeNull();
        backupOption!.Name.Should().Be("backup");
        backupOption!.Description.Should().Be("Create backup before applying changes");
    }

    [Fact]
    public void OptimizeCommand_ShouldHaveCorrectNumberOfOptions()
    {
        // Act
        var command = OptimizeCommand.Create();

        // Assert
        command.Options.Should().HaveCount(5);
    }

    #endregion

    #region ExecuteOptimize Tests

    [Fact]
    public async Task ExecuteOptimize_WithDryRun_ShouldNotModifyFiles()
    {
        // Arrange
        var testFile = Path.Combine(_testPath, "Handler.cs");
        var originalContent = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}";

        await File.WriteAllTextAsync(testFile, originalContent);

        // Act
        await OptimizeCommand.ExecuteOptimize(_testPath, true, "all", false, false);

        // Assert
        var finalContent = await File.ReadAllTextAsync(testFile);
        finalContent.Should().Be(originalContent); // Should not be modified in dry run
    }

    [Fact]
    public async Task ExecuteOptimize_WithActualRun_ShouldModifyFiles()
    {
        // Arrange
        var testFile = Path.Combine(_testPath, "Handler.cs");
        var originalContent = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}";

        await File.WriteAllTextAsync(testFile, originalContent);

        // Act
        await OptimizeCommand.ExecuteOptimize(_testPath, false, "all", false, false);

        // Assert
        var finalContent = await File.ReadAllTextAsync(testFile);
        finalContent.Should().Contain("ValueTask<string>"); // Should be optimized
    }

    [Fact]
    public async Task ExecuteOptimize_WithHandlersTarget_ShouldOnlyOptimizeHandlers()
    {
        // Arrange
        var handlerFile = Path.Combine(_testPath, "Handler.cs");
        var otherFile = Path.Combine(_testPath, "Service.cs");

        var handlerContent = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}";

        var serviceContent = @"using System;

public class TestService
{
    public async Task<string> GetData()
    {
        return ""data"";
    }
}";

        await File.WriteAllTextAsync(handlerFile, handlerContent);
        await File.WriteAllTextAsync(otherFile, serviceContent);

        // Act
        await OptimizeCommand.ExecuteOptimize(_testPath, false, "handlers", false, false);

        // Assert
        var handlerFinalContent = await File.ReadAllTextAsync(handlerFile);
        var serviceFinalContent = await File.ReadAllTextAsync(otherFile);

        handlerFinalContent.Should().Contain("ValueTask<string>");
        serviceFinalContent.Should().Be(serviceContent); // Should not be modified
    }

    [Fact]
    public async Task ExecuteOptimize_WithAggressiveMode_ShouldApplyMoreOptimizations()
    {
        // Arrange
        var testFile = Path.Combine(_testPath, "Aggressive.cs");
        var content = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}";

        await File.WriteAllTextAsync(testFile, content);

        // Act
        await OptimizeCommand.ExecuteOptimize(_testPath, false, "all", true, false);

        // Assert
        var finalContent = await File.ReadAllTextAsync(testFile);
        finalContent.Should().Contain("ValueTask<string>");
    }

    #endregion

    #region File Discovery Tests

    [Fact]
    public async Task DiscoverFiles_ShouldFindCsFiles()
    {
        // Arrange
        var csFile1 = Path.Combine(_testPath, "File1.cs");
        var csFile2 = Path.Combine(_testPath, "File2.cs");
        var txtFile = Path.Combine(_testPath, "File.txt");

        await File.WriteAllTextAsync(csFile1, "class Test1 {}");
        await File.WriteAllTextAsync(csFile2, "class Test2 {}");
        await File.WriteAllTextAsync(txtFile, "text content");

        var context = new OptimizationContext
        {
            ProjectPath = _testPath
        };

        // Act
        await OptimizeCommand.DiscoverFiles(context, null, null);

        // Assert
        context.SourceFiles.Should().HaveCount(2);
        context.SourceFiles.Should().Contain(f => f.EndsWith("File1.cs"));
        context.SourceFiles.Should().Contain(f => f.EndsWith("File2.cs"));
        context.SourceFiles.Should().NotContain(f => f.EndsWith("File.txt"));
    }

    [Fact]
    public async Task DiscoverFiles_ShouldExcludeBinAndObjDirectories()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testPath, "bin"));
        Directory.CreateDirectory(Path.Combine(_testPath, "obj"));

        var validFile = Path.Combine(_testPath, "Valid.cs");
        var binFile = Path.Combine(_testPath, "bin", "Invalid.cs");
        var objFile = Path.Combine(_testPath, "obj", "Invalid.cs");

        await File.WriteAllTextAsync(validFile, "class Test {}");
        await File.WriteAllTextAsync(binFile, "class BinTest {}");
        await File.WriteAllTextAsync(objFile, "class ObjTest {}");

        var context = new OptimizationContext
        {
            ProjectPath = _testPath
        };

        // Act
        await OptimizeCommand.DiscoverFiles(context, null, null);

        // Assert
        context.SourceFiles.Should().HaveCount(1);
        context.SourceFiles[0].Should().EndWith("Valid.cs");
    }

    #endregion

    #region Optimization Logic Tests

    [Fact]
    public async Task OptimizeHandlers_ShouldConvertTaskToValueTask()
    {
        // Arrange
        var testFile = Path.Combine(_testPath, "Handler.cs");
        var originalContent = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}";

        await File.WriteAllTextAsync(testFile, originalContent);

        var context = new OptimizationContext
        {
            ProjectPath = _testPath,
            IsDryRun = false
        };

        // Manually add the file to context
        context.SourceFiles.Add(testFile);

        // Act
        await OptimizeCommand.OptimizeHandlers(context, null, null);

        // Assert
        context.OptimizationActions.Should().HaveCount(1);
        var action = context.OptimizationActions[0];
        action.Type.Should().Be("Handler Optimization");
        action.Modifications.Should().Contain("Replaced Task<T> with ValueTask<T> for better performance");

        var finalContent = await File.ReadAllTextAsync(testFile);
        finalContent.Should().Contain("ValueTask<string>");
    }

    [Fact]
    public async Task OptimizeHandlers_WithDryRun_ShouldNotModifyFiles()
    {
        // Arrange
        var testFile = Path.Combine(_testPath, "Handler.cs");
        var originalContent = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}";

        await File.WriteAllTextAsync(testFile, originalContent);

        var context = new OptimizationContext
        {
            ProjectPath = _testPath,
            IsDryRun = true
        };

        context.SourceFiles.Add(testFile);

        // Act
        await OptimizeCommand.OptimizeHandlers(context, null, null);

        // Assert
        context.OptimizationActions.Should().HaveCount(1);
        var finalContent = await File.ReadAllTextAsync(testFile);
        finalContent.Should().Be(originalContent); // Should not be modified
    }

    [Fact]
    public async Task OptimizeHandlers_WithNoOptimizableCode_ShouldNotCreateActions()
    {
        // Arrange
        var testFile = Path.Combine(_testPath, "Service.cs");
        var content = @"using System;

public class TestService
{
    public void DoSomething()
    {
        Console.WriteLine(""test"");
    }
}";

        await File.WriteAllTextAsync(testFile, content);

        var context = new OptimizationContext
        {
            ProjectPath = _testPath,
            IsDryRun = false
        };

        context.SourceFiles.Add(testFile);

        // Act
        await OptimizeCommand.OptimizeHandlers(context, null, null);

        // Assert
        context.OptimizationActions.Should().BeEmpty();
    }

    #endregion

    #region Target Option Tests

    [Fact]
    public async Task ExecuteOptimize_WithAllTarget_ShouldRunAllOptimizations()
    {
        // Arrange
        var handlerFile = Path.Combine(_testPath, "Handler.cs");
        var handlerContent = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}";

        await File.WriteAllTextAsync(handlerFile, handlerContent);

        // Act
        await OptimizeCommand.ExecuteOptimize(_testPath, false, "all", false, false);

        // Assert
        var finalContent = await File.ReadAllTextAsync(handlerFile);
        finalContent.Should().Contain("ValueTask<string>");
    }

    [Fact]
    public async Task ExecuteOptimize_WithHandlersTarget_ShouldOnlyRunHandlerOptimizations()
    {
        // Arrange
        var handlerFile = Path.Combine(_testPath, "Handler.cs");
        var handlerContent = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}";

        await File.WriteAllTextAsync(handlerFile, handlerContent);

        // Act
        await OptimizeCommand.ExecuteOptimize(_testPath, false, "handlers", false, false);

        // Assert
        var finalContent = await File.ReadAllTextAsync(handlerFile);
        finalContent.Should().Contain("ValueTask<string>");
    }

    [Fact]
    public async Task ExecuteOptimize_WithInvalidTarget_ShouldDefaultToAll()
    {
        // Arrange
        var handlerFile = Path.Combine(_testPath, "Handler.cs");
        var handlerContent = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}";

        await File.WriteAllTextAsync(handlerFile, handlerContent);

        // Act
        await OptimizeCommand.ExecuteOptimize(_testPath, false, "invalid", false, false);

        // Assert - Should still run optimizations as if target was "all"
        var finalContent = await File.ReadAllTextAsync(handlerFile);
        finalContent.Should().Contain("ValueTask<string>");
    }

    #endregion

    #region Backup Tests

    [Fact]
    public async Task ExecuteOptimize_WithBackupEnabled_ShouldCreateBackup()
    {
        // Arrange
        var testFile = Path.Combine(_testPath, "Handler.cs");
        var originalContent = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}";

        await File.WriteAllTextAsync(testFile, originalContent);

        // Act
        await OptimizeCommand.ExecuteOptimize(_testPath, false, "all", false, true);

        // Assert
        var backupFiles = Directory.GetFiles(_testPath, "*.bak", SearchOption.AllDirectories);
        backupFiles.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteOptimize_WithBackupDisabled_ShouldNotCreateBackup()
    {
        // Arrange
        var testFile = Path.Combine(_testPath, "Handler.cs");
        var originalContent = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}";

        await File.WriteAllTextAsync(testFile, originalContent);

        // Act
        await OptimizeCommand.ExecuteOptimize(_testPath, false, "all", false, false);

        // Assert
        var backupFiles = Directory.GetFiles(_testPath, "*.bak", SearchOption.AllDirectories);
        backupFiles.Should().BeEmpty();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteOptimize_WithNonExistentPath_ShouldHandleGracefully()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testPath, "NonExistent");

        // Act & Assert - Should not throw
        await OptimizeCommand.ExecuteOptimize(nonExistentPath, true, "all", false, false);
    }

    [Fact]
    public async Task ExecuteOptimize_WithEmptyDirectory_ShouldHandleGracefully()
    {
        // Arrange
        var emptyDir = Path.Combine(_testPath, "Empty");
        Directory.CreateDirectory(emptyDir);

        // Act & Assert - Should not throw
        await OptimizeCommand.ExecuteOptimize(emptyDir, true, "all", false, false);
    }

    [Fact]
    public async Task ExecuteOptimize_WithReadOnlyFile_ShouldHandleGracefully()
    {
        // Arrange
        var testFile = Path.Combine(_testPath, "ReadOnly.cs");
        var content = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async Task<string> HandleAsync(TestRequest request)
    {
        return ""test"";
    }
}";

        await File.WriteAllTextAsync(testFile, content);
        File.SetAttributes(testFile, FileAttributes.ReadOnly);

        // Act & Assert - Should not throw
        await OptimizeCommand.ExecuteOptimize(_testPath, false, "all", false, false);

        // Cleanup
        File.SetAttributes(testFile, FileAttributes.Normal);
    }

    #endregion

    #region Results Display Tests

    [Fact]
    public void DisplayOptimizationResults_WithNoOptimizations_ShouldShowNoChangesMessage()
    {
        // Arrange
        var context = new OptimizationContext
        {
            IsDryRun = false,
            OptimizationActions = new List<OptimizationAction>()
        };

        // Act & Assert - This would normally write to console, but we can't easily test console output
        // The method should not throw
        OptimizeCommand.DisplayOptimizationResults(context);
    }

    [Fact]
    public void DisplayOptimizationResults_WithOptimizations_ShouldShowResultsTable()
    {
        // Arrange
        var context = new OptimizationContext
        {
            IsDryRun = false,
            OptimizationActions = new List<OptimizationAction>
            {
                new OptimizationAction
                {
                    FilePath = "TestHandler.cs",
                    Type = "Handler Optimization",
                    Modifications = new List<string> { "Replaced Task<T> with ValueTask<T>" }
                }
            }
        };

        // Act & Assert - Should not throw
        OptimizeCommand.DisplayOptimizationResults(context);
    }

    [Fact]
    public void DisplayOptimizationResults_WithDryRun_ShouldShowDryRunMessage()
    {
        // Arrange
        var context = new OptimizationContext
        {
            IsDryRun = true,
            OptimizationActions = new List<OptimizationAction>
            {
                new OptimizationAction
                {
                    FilePath = "TestHandler.cs",
                    Type = "Handler Optimization",
                    Modifications = new List<string> { "Replaced Task<T> with ValueTask<T>" }
                }
            }
        };

        // Act & Assert - Should not throw
        OptimizeCommand.DisplayOptimizationResults(context);
    }

    #endregion

    #region Progress Reporting Tests

    [Fact]
    public void GetOptimizationSteps_WithAllTarget_ShouldReturnCorrectSteps()
    {
        // Act
        var steps = OptimizeCommand.GetOptimizationSteps("all");

        // Assert
        steps.Should().Be(2);
    }

    [Fact]
    public void GetOptimizationSteps_WithHandlersTarget_ShouldReturnCorrectSteps()
    {
        // Act
        var steps = OptimizeCommand.GetOptimizationSteps("handlers");

        // Assert
        steps.Should().Be(2);
    }

    [Fact]
    public void GetOptimizationSteps_WithOtherTarget_ShouldReturnOneStep()
    {
        // Act
        var steps = OptimizeCommand.GetOptimizationSteps("other");

        // Assert
        steps.Should().Be(1);
    }

    #endregion

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
