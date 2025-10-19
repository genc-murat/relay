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
/// Thread-safety tests for RelayCompilationContext caching mechanisms.
/// Verifies that semantic model and type lookup caching is truly thread-safe.
/// </summary>
public class ThreadSafetyRelayCompilationContextTests
{
    private const int HighConcurrencyThreadCount = 100;
    private const int ExtremeConcurrencyThreadCount = 1000;

    [Fact]
    public void RelayCompilationContext_GetSemanticModel_ConcurrentAccess_ReturnsSameInstance()
    {
        // Arrange
        var compilation = CreateTestCompilation();
        var context = new RelayCompilationContext(compilation, CancellationToken.None);
        var syntaxTree = compilation.SyntaxTrees.First();

        var models = new ConcurrentBag<SemanticModel>();
        var callCounter = 0;

        // Act: 100 threads accessing simultaneously
        Parallel.For(0, HighConcurrencyThreadCount, i =>
        {
            var model = context.GetSemanticModel(syntaxTree);
            models.Add(model);
            Interlocked.Increment(ref callCounter);
        });

        // Assert: All threads should get the EXACT same instance
        var distinctModels = models.Distinct().ToList();
        Assert.Single(distinctModels);
        Assert.Equal(HighConcurrencyThreadCount, callCounter);
        Assert.Equal(HighConcurrencyThreadCount, models.Count);
    }

    [Fact]
    public void RelayCompilationContext_FindType_ConcurrentAccess_ReturnsSameInstance()
    {
        // Arrange
        var compilation = CreateTestCompilationWithSystemTypes();
        var context = new RelayCompilationContext(compilation, CancellationToken.None);
        const string typeName = "System.String";

        var types = new ConcurrentBag<INamedTypeSymbol?>();
        var callCounter = 0;

        // Act: 100 threads looking up the same type
        Parallel.For(0, HighConcurrencyThreadCount, i =>
        {
            var type = context.FindType(typeName);
            types.Add(type);
            Interlocked.Increment(ref callCounter);
        });

        // Assert: All should get the same type instance
        var distinctTypes = types.Where(t => t != null).Distinct(SymbolEqualityComparer.Default).ToList();
        Assert.Single(distinctTypes);
        Assert.Equal(HighConcurrencyThreadCount, callCounter);
        Assert.All(types, t => Assert.NotNull(t));
    }

    [Fact]
    public void RelayCompilationContext_HasRelayCoreReference_ConcurrentAccess_ConsistentResult()
    {
        // Arrange
        var compilation = CreateTestCompilation();
        var context = new RelayCompilationContext(compilation, CancellationToken.None);

        var results = new ConcurrentBag<bool>();
        var callCounter = 0;

        // Act: 1000 threads checking simultaneously
        Parallel.For(0, ExtremeConcurrencyThreadCount, i =>
        {
            var hasRef = context.HasRelayCoreReference();
            results.Add(hasRef);
            Interlocked.Increment(ref callCounter);
        });

        // Assert: All threads should see the same result
        var distinctResults = results.Distinct().ToList();
        Assert.Single(distinctResults);
        Assert.Equal(ExtremeConcurrencyThreadCount, callCounter);
    }

    [Fact]
    public void RelayCompilationContext_MixedOperations_NoRaceConditions()
    {
        // Arrange
        var compilation = CreateTestCompilationWithSystemTypes();
        var context = new RelayCompilationContext(compilation, CancellationToken.None);
        var syntaxTree = compilation.SyntaxTrees.First();

        var exceptions = new ConcurrentBag<Exception>();
        var successCount = 0;

        // Act: Mix of different operations from multiple threads
        Parallel.For(0, HighConcurrencyThreadCount, i =>
        {
            try
            {
                // Each thread performs multiple operations
                _ = context.GetSemanticModel(syntaxTree);
                _ = context.FindType("System.String");
                _ = context.FindType("System.Int32");
                _ = context.HasRelayCoreReference();
                _ = context.GetSemanticModel(syntaxTree); // Access again

                Interlocked.Increment(ref successCount);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert: No exceptions, all operations successful
        Assert.Empty(exceptions);
        Assert.Equal(HighConcurrencyThreadCount, successCount);
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

    #endregion
}