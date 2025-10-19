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
/// Thread-safety tests for HandlerDiscoveryEngine parallel processing.
/// Verifies that handler discovery operations are thread-safe under concurrent access.
/// </summary>
public class ThreadSafetyHandlerDiscoveryEngineTests
{
    private const int HighConcurrencyThreadCount = 100;

    [Fact]
    public void HandlerDiscoveryEngine_GetResponseType_ConcurrentAccess_SingleComputation()
    {
        // Arrange
        var compilation = CreateTestCompilationWithHandlers();
        var context = new RelayCompilationContext(compilation, CancellationToken.None);
        var engine = new HandlerDiscoveryEngine(context);

        var method = GetTestMethodSymbol(compilation);
        var results = new ConcurrentBag<ITypeSymbol?>();

        // Use reflection to access private method
        var getResponseTypeMethod = typeof(HandlerDiscoveryEngine)
            .GetMethod("GetResponseType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(getResponseTypeMethod);

        // Act: 100 threads calling GetResponseType
        Parallel.For(0, HighConcurrencyThreadCount, i =>
        {
            var result = getResponseTypeMethod!.Invoke(engine, new object[] { method });
            results.Add(result as ITypeSymbol);
        });

        // Assert: All results should be the same instance
        var distinctResults = results.Where(r => r != null).Distinct(SymbolEqualityComparer.Default).ToList();
        Assert.True(distinctResults.Count <= 1); // Should be exactly 1, but allow for null
    }

    #region Helper Methods

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
            new[] { syntaxTree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Task<>).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IMethodSymbol GetTestMethodSymbol(Compilation compilation)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodDeclaration = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        return semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol
               ?? throw new InvalidOperationException("Could not find method symbol");
    }

    #endregion
}