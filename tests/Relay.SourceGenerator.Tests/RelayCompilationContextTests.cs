using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

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
}
