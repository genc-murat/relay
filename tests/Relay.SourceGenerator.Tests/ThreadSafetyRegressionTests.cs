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
/// Regression tests for TASK-001 and TASK-002 thread-safety fixes.
/// Ensures that previously identified issues remain resolved.
/// </summary>
public class ThreadSafetyRegressionTests
{
    [Fact]
    public void RelayCompilationContext_NullableReferenceTypes_NoWarnings()
    {
        // Arrange
        var compilation = CreateTestCompilation();
        var context = new RelayCompilationContext(compilation, CancellationToken.None);

        // Act & Assert: Should not throw NullReferenceException
        var syntaxTree = compilation.SyntaxTrees.FirstOrDefault();
        if (syntaxTree != null)
        {
            var model = context.GetSemanticModel(syntaxTree);
            Assert.NotNull(model);
        }

        var type = context.FindType("NonExistentType");
        // Null is valid for non-existent types
        Assert.Null(type);
    }

    [Fact]
    public void RelayCompilationContext_LazyInitialization_NoDoubleExecution()
    {
        // This test verifies TASK-002 fix: Lazy<T> prevents double execution
        // Arrange
        var compilation = CreateTestCompilation();
        var context = new RelayCompilationContext(compilation, CancellationToken.None);

        // We can't directly test private Lazy execution, but we can verify behavior
        // Multiple calls should not cause performance degradation that would indicate re-execution
        var sw = Stopwatch.StartNew();

        // Act: First call (cache miss)
        _ = context.HasRelayCoreReference();
        var firstCallTime = sw.ElapsedMilliseconds;

        // Act: Subsequent calls (cache hits)
        sw.Restart();
        for (int i = 0; i < 1000; i++)
        {
            _ = context.HasRelayCoreReference();
        }
        var subsequentCallsTime = sw.ElapsedMilliseconds;

        // Assert: Subsequent calls should be MUCH faster (they're just reading Lazy.Value)
        Assert.True(subsequentCallsTime < firstCallTime * 10,
            "Cache hits are too slow - Lazy<T> might not be working correctly");
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

    #endregion
}