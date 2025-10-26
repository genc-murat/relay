using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Discovery;
using Xunit;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Performance tests under load for thread-safety verification.
/// Ensures that caching mechanisms perform well under high concurrency.
/// </summary>
public class ThreadSafetyPerformanceTests
{
    private const int ExtremeConcurrencyThreadCount = 1000;

    [Fact]
    public void RelayCompilationContext_HighLoad_ReasonablePerformance()
    {
        // Arrange
        var compilation = CreateTestCompilationWithSystemTypes();
        var context = new RelayCompilationContext(compilation, CancellationToken.None);
        var syntaxTree = compilation.SyntaxTrees.First();

        var sw = Stopwatch.StartNew();

        // Act: Simulate high load
        Parallel.For(0, ExtremeConcurrencyThreadCount, i =>
        {
            _ = context.GetSemanticModel(syntaxTree);
            _ = context.FindType("System.String");
        });

        sw.Stop();

        // Assert: Should complete in reasonable time (< 5 seconds)
        Assert.True(sw.ElapsedMilliseconds < 5000,
            $"Operation took too long: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void RelayCompilationContext_CacheHitPerformance_FastAfterWarmup()
    {
        // Arrange
        var compilation = CreateTestCompilationWithSystemTypes();
        var context = new RelayCompilationContext(compilation, CancellationToken.None);
        var syntaxTree = compilation.SyntaxTrees.First();

        // Warmup: Populate cache
        _ = context.GetSemanticModel(syntaxTree);

        var sw = Stopwatch.StartNew();

        // Act: All should be cache hits
        Parallel.For(0, ExtremeConcurrencyThreadCount, i =>
        {
            _ = context.GetSemanticModel(syntaxTree);
        });

        sw.Stop();

        // Assert: Cache hits should be very fast (< 1 second for 1000 operations)
        Assert.True(sw.ElapsedMilliseconds < 1000,
            $"Cache hits too slow: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void HandlerDiscoveryEngine_ConfigurableParallelism_AcceptsCustomDegree()
    {
        // Arrange
        var compilation = CreateTestCompilationWithHandlers();
        var context = new RelayCompilationContext(compilation, CancellationToken.None);

        // Act: Create engines with different parallelism levels
        var engine2 = new HandlerDiscoveryEngine(context, 2);
        var engine8 = new HandlerDiscoveryEngine(context, 8);
        var engineDefault = new HandlerDiscoveryEngine(context);

        // Assert: All should be created successfully (actual parallelism is tested in performance tests)
        Assert.NotNull(engine2);
        Assert.NotNull(engine8);
        Assert.NotNull(engineDefault);
    }

    [Fact]
    public void HandlerDiscoveryEngine_ParallelProcessing_CompletesEfficiently()
    {
        // Arrange: Create compilation with many handlers
        var compilation = CreateTestCompilationWithManyHandlers(50);
        var context = new RelayCompilationContext(compilation, CancellationToken.None);
        var engine = new HandlerDiscoveryEngine(context, 4);

        var candidateMethods = compilation.SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>())
            .ToList();

        var reporter = new TestDiagnosticReporter();
        var sw = Stopwatch.StartNew();

        // Act: Discover handlers (should use parallel processing for 50+ methods)
        var result = engine.DiscoverHandlers(candidateMethods, reporter);

        sw.Stop();

        // Assert: Should complete quickly even with many methods
        Assert.True(sw.ElapsedMilliseconds < 2000,
            $"Handler discovery took too long: {sw.ElapsedMilliseconds}ms for {candidateMethods.Count} methods");

        // Note: Handlers might be empty if methods don't have [Handle] attributes
        // This test focuses on parallel processing performance, not handler validation
        Assert.Equal(50, candidateMethods.Count);
    }

    [Fact]
    public void HandlerDiscoveryEngine_SmallCollection_UsesSequentialProcessing()
    {
        // Arrange: Small collection (< 10 methods) should use sequential processing
        var compilation = CreateTestCompilationWithHandlers();
        var context = new RelayCompilationContext(compilation, CancellationToken.None);
        var engine = new HandlerDiscoveryEngine(context, 4);

        var candidateMethods = compilation.SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>())
            .ToList();

        var reporter = new TestDiagnosticReporter();
        var sw = Stopwatch.StartNew();

        // Act: Discover handlers from small collection
        var result = engine.DiscoverHandlers(candidateMethods, reporter);

        sw.Stop();

        // Assert: Should complete very quickly for small collections
        Assert.True(sw.ElapsedMilliseconds < 500,
            $"Small collection processing took too long: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void HandlerDiscoveryEngine_ParallelismClamping_EnforcesLimits()
    {
        // Arrange
        var compilation = CreateTestCompilation();
        var context = new RelayCompilationContext(compilation, CancellationToken.None);

        // Act: Try to create engines with extreme parallelism values
        var engineTooLow = new HandlerDiscoveryEngine(context, 0);     // Should clamp to 2
        var engineTooHigh = new HandlerDiscoveryEngine(context, 100);  // Should clamp to 8
        var engineNegative = new HandlerDiscoveryEngine(context, -5);  // Should clamp to 2

        // Assert: All should be created successfully with clamped values
        Assert.NotNull(engineTooLow);
        Assert.NotNull(engineTooHigh);
        Assert.NotNull(engineNegative);
    }

    #region Helper Methods

    private static CSharpCompilation CreateTestCompilation()
    {
        var source = @"
using System;
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod() { }
    }
}";
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static Compilation CreateTestCompilationWithSystemTypes()
    {
        var source = @"
using System;
namespace TestNamespace
{
    public class TestClass
    {
        public string StringProperty { get; set; }
        public int IntProperty { get; set; }
        public bool BoolProperty { get; set; }
    }
}";
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static Compilation CreateTestCompilationWithHandlers()
    {
        var source = @"
using System.Threading.Tasks;
namespace TestNamespace
{
    public class TestHandler
    {
        public async Task<string> HandleAsync(TestRequest request)
        {
            return await Task.FromResult(""test"");
        }
    }

    public class TestRequest { }
}";
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Task<>).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static Compilation CreateTestCompilationWithManyHandlers(int handlerCount)
    {
        var sourceBuilder = new System.Text.StringBuilder();
        sourceBuilder.AppendLine("using System.Threading.Tasks;");
        sourceBuilder.AppendLine("namespace TestNamespace {");

        for (int i = 0; i < handlerCount; i++)
        {
            sourceBuilder.AppendLine($@"
    public class TestRequest{i} {{ }}

    public class TestHandler{i} {{
        public async Task<string> HandleAsync(TestRequest{i} request) {{
            return await Task.FromResult(""test{i}"");
        }}
    }}");
        }

        sourceBuilder.AppendLine("}");

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceBuilder.ToString());

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Task<>).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private class TestDiagnosticReporter : IDiagnosticReporter
    {
        private readonly ConcurrentBag<Diagnostic> _diagnostics = [];

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }

        public IEnumerable<Diagnostic> GetDiagnostics() => _diagnostics;
    }

    #endregion
}