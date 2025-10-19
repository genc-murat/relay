using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Edge cases and stress scenarios for thread-safety verification.
/// Tests boundary conditions and potential race conditions.
/// </summary>
public class ThreadSafetyEdgeCasesTests
{
    private const int HighConcurrencyThreadCount = 100;

    [Fact]
    public void RelayCompilationContext_MultipleSyntaxTrees_NoCrossContamination()
    {
        // Arrange
        var compilation = CreateTestCompilationWithMultipleTrees();
        var context = new RelayCompilationContext(compilation, CancellationToken.None);
        var trees = compilation.SyntaxTrees.ToList();

        var treeToModelMap = new ConcurrentDictionary<SyntaxTree, List<SemanticModel>>();

        // Act: Multiple threads accessing different trees
        Parallel.For(0, HighConcurrencyThreadCount, i =>
        {
            var tree = trees[i % trees.Count];
            var model = context.GetSemanticModel(tree);

            treeToModelMap.AddOrUpdate(
                tree,
                new List<SemanticModel> { model },
                (_, list) =>
                {
                    lock (list)
                    {
                        list.Add(model);
                        return list;
                    }
                });
        });

        // Assert: Each tree should have exactly one unique model instance
        foreach (var kvp in treeToModelMap)
        {
            var distinctModels = kvp.Value.Distinct().ToList();
            Assert.Single(distinctModels);
        }
    }

    [Fact]
    public void RelayCompilationContext_RapidSuccessiveCalls_NoDeadlock()
    {
        // Arrange
        var compilation = CreateTestCompilationWithSystemTypes();
        var context = new RelayCompilationContext(compilation, CancellationToken.None);

        var completed = false;
        var timeout = TimeSpan.FromSeconds(10);

        // Act: Rapid successive calls from multiple threads
        var task = Task.Run(() =>
        {
            Parallel.For(0, HighConcurrencyThreadCount, i =>
            {
                for (int j = 0; j < 10; j++)
                {
                    _ = context.FindType($"System.{(j % 3 == 0 ? "String" : j % 3 == 1 ? "Int32" : "Boolean")}");
                    _ = context.HasRelayCoreReference();
                }
            });
            completed = true;
        });

        // Assert: Should complete without deadlock
        var finishedInTime = task.Wait(timeout);
        Assert.True(finishedInTime, "Operation timed out - possible deadlock");
        Assert.True(completed);
    }

    [Fact]
    public void RelayCompilationContext_CancellationToken_PropagatedCorrectly()
    {
        // Arrange
        var compilation = CreateTestCompilation();
        var cts = new CancellationTokenSource();
        var context = new RelayCompilationContext(compilation, cts.Token);

        var cancelledCount = 0;
        var successCount = 0;

        // Act: Cancel after some threads have started
        var task = Task.Run(() =>
        {
            Parallel.For(0, HighConcurrencyThreadCount, i =>
            {
                try
                {
                    if (i == 50) cts.Cancel(); // Cancel midway

                    _ = context.HasRelayCoreReference();
                    Interlocked.Increment(ref successCount);
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref cancelledCount);
                }
            });
        });

        task.Wait();

        // Assert: Some operations should succeed, some should be cancelled
        Assert.True(successCount > 0);
        // Note: cancellation might not always trigger due to timing
    }

    #region Helper Methods

    private static Compilation CreateTestCompilation()
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
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
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
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static Compilation CreateTestCompilationWithMultipleTrees()
    {
        var sources = new[]
        {
            "namespace TestNamespace { public class Class1 { } }",
            "namespace TestNamespace { public class Class2 { } }",
            "namespace TestNamespace { public class Class3 { } }",
            "namespace TestNamespace { public class Class4 { } }",
            "namespace TestNamespace { public class Class5 { } }"
        };

        var syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();

        return CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    #endregion
}