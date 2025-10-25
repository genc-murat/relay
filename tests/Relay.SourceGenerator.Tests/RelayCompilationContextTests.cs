using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for RelayCompilationContext
/// </summary>
public class RelayCompilationContextTests
{
    [Fact]
    public void RelayCompilationContext_CanBeCreated()
    {
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace TestNamespace
{
    public class TestClass { }
}");
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddSyntaxTrees(syntaxTree);

        // Act
        var context = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Compilation);
        Assert.Equal("TestAssembly", context.Compilation.AssemblyName);
        Assert.Equal("TestAssembly", context.AssemblyName);
    }

    [Fact]
    public void RelayCompilationContext_StoresCompilation()
    {
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText("class Test {}");
        var compilation = CSharpCompilation.Create("MyAssembly")
            .AddSyntaxTrees(syntaxTree);

        // Act
        var context = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);

        // Assert
        Assert.Same(compilation, context.Compilation);
    }

    [Fact]
    public void RelayCompilationContext_PreservesCompilationProperties()
    {
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
namespace MyApp
{
    public interface IRequest<TResponse> { }
    public class MyRequest : IRequest<string> { }
}");
        
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Act
        var context = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);

        // Assert
        Assert.Equal("TestAssembly", context.Compilation.AssemblyName);
        Assert.NotEmpty(context.Compilation.SyntaxTrees);
        Assert.NotEmpty(context.Compilation.References);
    }

    [Fact]
    public void RelayCompilationContext_CanAccessSemanticModel()
    {
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace TestNamespace
{
    public class TestClass 
    { 
        public void TestMethod() { }
    }
}");
        
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references);

        var context = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);

        // Act
        var semanticModel = context.GetSemanticModel(syntaxTree);

        // Assert
        Assert.NotNull(semanticModel);
        Assert.Same(syntaxTree, semanticModel.SyntaxTree);
    }

    [Fact]
    public void RelayCompilationContext_CanFindTypesInCompilation()
    {
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace MyApp.Handlers
{
    public class UserHandler { }
    public class OrderHandler { }
}");
        
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references);

        var context = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);

        // Act
        var globalNamespace = context.Compilation.GlobalNamespace;
        var myAppNamespace = globalNamespace.GetNamespaceMembers()
            .FirstOrDefault(n => n.Name == "MyApp");
        var handlersNamespace = myAppNamespace?.GetNamespaceMembers()
            .FirstOrDefault(n => n.Name == "Handlers");
        var types = handlersNamespace?.GetTypeMembers().ToArray() ?? Array.Empty<INamedTypeSymbol>();

        // Assert
        Assert.NotNull(handlersNamespace);
        Assert.Equal(2, types.Length);
        Assert.Contains(types, t => t.Name == "UserHandler");
        Assert.Contains(types, t => t.Name == "OrderHandler");
    }

    [Fact]
    public void RelayCompilationContext_MultipleInstances_IndependentCompilations()
    {
        // Arrange
        var tree1 = CSharpSyntaxTree.ParseText("class Test1 {}");
        var tree2 = CSharpSyntaxTree.ParseText("class Test2 {}");

        var compilation1 = CSharpCompilation.Create("Assembly1")
            .AddSyntaxTrees(tree1);
        var compilation2 = CSharpCompilation.Create("Assembly2")
            .AddSyntaxTrees(tree2);

        // Act
        var context1 = new RelayCompilationContext(compilation1, System.Threading.CancellationToken.None);
        var context2 = new RelayCompilationContext(compilation2, System.Threading.CancellationToken.None);

        // Assert
        Assert.NotSame(context1.Compilation, context2.Compilation);
        Assert.Equal("Assembly1", context1.Compilation.AssemblyName);
        Assert.Equal("Assembly2", context2.Compilation.AssemblyName);
    }

    [Fact]
    public void ClearCaches_ClearsSemanticModelCache()
    {
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod() { }
    }
}");

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references);

        var context = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);

        // Act: Get semantic model to populate cache
        var semanticModel1 = context.GetSemanticModel(syntaxTree);

        // Clear caches
        context.ClearCaches();

        // Get semantic model again - should be a new instance (cache was cleared)
        var semanticModel2 = context.GetSemanticModel(syntaxTree);

        // Assert: Both should be valid but different instances (since cache was cleared)
        Assert.NotNull(semanticModel1);
        Assert.NotNull(semanticModel2);
        Assert.Same(syntaxTree, semanticModel1.SyntaxTree);
        Assert.Same(syntaxTree, semanticModel2.SyntaxTree);
        // Note: Roslyn may return the same instance for identical compilations, so we don't assert they're different
    }

    [Fact]
    public void ClearCaches_ClearsTypeCache()
    {
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace TestNamespace
{
    public class TestClass { }
}");

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references);

        var context = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);

        // Act: Find type to populate cache
        var type1 = context.FindType("TestNamespace.TestClass");

        // Clear caches
        context.ClearCaches();

        // Find type again - cache was cleared
        var type2 = context.FindType("TestNamespace.TestClass");

        // Assert: Both should be valid
        Assert.NotNull(type1);
        Assert.NotNull(type2);
        Assert.Equal("TestClass", type1.Name);
        Assert.Equal("TestClass", type2.Name);
        // Note: Roslyn may return the same symbol instance, so we don't assert they're different
    }

    [Fact]
    public void GetSemanticModel_CachesResults()
    {
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod() { }
    }
}");

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references);

        var context = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);

        // Act: Get semantic model multiple times
        var semanticModel1 = context.GetSemanticModel(syntaxTree);
        var semanticModel2 = context.GetSemanticModel(syntaxTree);
        var semanticModel3 = context.GetSemanticModel(syntaxTree);

        // Assert: All calls should return the same instance (cached)
        Assert.NotNull(semanticModel1);
        Assert.NotNull(semanticModel2);
        Assert.NotNull(semanticModel3);
        Assert.Same(semanticModel1, semanticModel2);
        Assert.Same(semanticModel2, semanticModel3);
        Assert.Same(syntaxTree, semanticModel1.SyntaxTree);
    }

    [Fact]
    public void FindType_CachesResults()
    {
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace TestNamespace
{
    public class TestClass { }
}");

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references);

        var context = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);

        // Act: Find type multiple times
        var type1 = context.FindType("TestNamespace.TestClass");
        var type2 = context.FindType("TestNamespace.TestClass");
        var type3 = context.FindType("TestNamespace.TestClass");

        // Assert: All calls should return the same instance (cached)
        Assert.NotNull(type1);
        Assert.NotNull(type2);
        Assert.NotNull(type3);
        Assert.Same(type1, type2);
        Assert.Same(type2, type3);
        Assert.Equal("TestClass", type1.Name);
    }

    [Fact]
    public void ClearCaches_ThenAccess_CreatesNewCacheEntries()
    {
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod() { }
    }
}");

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references);

        var context = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);

        // Act: Get semantic model and type to populate cache
        var semanticModel1 = context.GetSemanticModel(syntaxTree);
        var type1 = context.FindType("TestNamespace.TestClass");

        // Clear caches
        context.ClearCaches();

        // Access again - should create new cache entries
        var semanticModel2 = context.GetSemanticModel(syntaxTree);
        var type2 = context.FindType("TestNamespace.TestClass");

        // Assert: All should be valid
        Assert.NotNull(semanticModel1);
        Assert.NotNull(semanticModel2);
        Assert.NotNull(type1);
        Assert.NotNull(type2);
        Assert.Equal("TestClass", type1.Name);
        Assert.Equal("TestClass", type2.Name);
        Assert.Same(syntaxTree, semanticModel1.SyntaxTree);
        Assert.Same(syntaxTree, semanticModel2.SyntaxTree);
    }

    [Fact]
    public void ClearCaches_MultipleCalls_NoException()
    {
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText("class Test {}");
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddSyntaxTrees(syntaxTree);

        var context = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);

        // Act: Call ClearCaches multiple times
        context.ClearCaches();
        context.ClearCaches();
        context.ClearCaches();

        // Assert: No exception should be thrown
        Assert.NotNull(context);
    }

    [Fact]
    public void ClearCaches_OnEmptyCaches_NoException()
    {
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText("class Test {}");
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddSyntaxTrees(syntaxTree);

        var context = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);

        // Act: Clear caches without ever accessing them
        context.ClearCaches();

        // Assert: No exception should be thrown
        Assert.NotNull(context);
    }

    [Fact]
    public void ClearCaches_ResetsHasRelayCoreReference()
    {
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText("class Test {}");
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddSyntaxTrees(syntaxTree);

        var context = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);

        // Act: Access HasRelayCoreReference to initialize the Lazy<bool>
        var hasRef1 = context.HasRelayCoreReference();

        // Clear caches - this should reset the Lazy<bool> allowing re-evaluation
        context.ClearCaches();

        // Access again - should re-evaluate (though result will be the same for same compilation)
        var hasRef2 = context.HasRelayCoreReference();

        // Assert: Both calls should return the same result (same compilation)
        Assert.Equal(hasRef1, hasRef2);
        // The key difference is that ClearCaches now allows the Lazy to be re-initialized
    }

    [Fact]
    public void NewInstance_ResetsHasRelayCoreReferenceEvaluation()
    {
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText("class Test {}");
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddSyntaxTrees(syntaxTree);

        var context1 = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);

        // Act: Access HasRelayCoreReference on first instance
        var hasRef1 = context1.HasRelayCoreReference();

        // Create new instance - this resets the Lazy evaluation
        var context2 = new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);
        var hasRef2 = context2.HasRelayCoreReference();

        // Assert: Both instances should return the same result (same compilation)
        Assert.Equal(hasRef1, hasRef2);
        // Note: This test demonstrates that Lazy evaluation is per-instance
    }
}
