using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Helper class for creating test compilations with Roslyn.
/// Provides reusable infrastructure for testing source generator components.
/// </summary>
public static class TestCompilationHelper
{
    /// <summary>
    /// Creates a basic C# compilation for testing.
    /// </summary>
    /// <param name="source">The source code to compile</param>
    /// <param name="assemblyName">The assembly name</param>
    /// <returns>A CSharpCompilation instance</returns>
    public static CSharpCompilation CreateCompilation(string source, string assemblyName = "TestAssembly")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = GetBasicReferences();

        return CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>
    /// Creates a compilation with multiple source files.
    /// </summary>
    /// <param name="sources">The source code files to compile</param>
    /// <param name="assemblyName">The assembly name</param>
    /// <returns>A CSharpCompilation instance</returns>
    public static CSharpCompilation CreateCompilation(string[] sources, string assemblyName = "TestAssembly")
    {
        var syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();
        var references = GetBasicReferences();

        return CSharpCompilation.Create(
            assemblyName,
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>
    /// Gets the basic references needed for most tests.
    /// </summary>
    /// <returns>An array of MetadataReference instances</returns>
    public static MetadataReference[] GetBasicReferences()
    {
        return new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree).Assembly.Location),
        };
    }

    /// <summary>
    /// Gets a type symbol from a compilation by its full name.
    /// </summary>
    /// <param name="compilation">The compilation</param>
    /// <param name="fullTypeName">The full type name (e.g., "System.String")</param>
    /// <returns>The type symbol if found, null otherwise</returns>
    public static ITypeSymbol? GetTypeSymbol(Compilation compilation, string fullTypeName)
    {
        return compilation.GetTypeByMetadataName(fullTypeName);
    }

    /// <summary>
    /// Gets a method symbol from a type by its name.
    /// </summary>
    /// <param name="typeSymbol">The type symbol</param>
    /// <param name="methodName">The method name</param>
    /// <returns>The method symbol if found, null otherwise</returns>
    public static IMethodSymbol? GetMethodSymbol(ITypeSymbol typeSymbol, string methodName)
    {
        if (typeSymbol is INamedTypeSymbol namedType)
        {
            return namedType.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault();
        }
        return null;
    }

    /// <summary>
    /// Creates a simple test class with interfaces.
    /// </summary>
    /// <param name="className">The class name</param>
    /// <param name="interfaces">The interfaces to implement</param>
    /// <returns>Source code for the test class</returns>
    public static string CreateTestClass(string className, params string[] interfaces)
    {
        var interfaceList = interfaces.Length > 0 ? $" : {string.Join(", ", interfaces)}" : "";
        return $@"
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{{
    public class {className}{interfaceList}
    {{
        public void TestMethod() {{ }}
    }}
}}";
    }

    /// <summary>
    /// Creates a test interface.
    /// </summary>
    /// <param name="interfaceName">The interface name</param>
    /// <param name="methods">The methods in the interface</param>
    /// <returns>Source code for the test interface</returns>
    public static string CreateTestInterface(string interfaceName, params string[] methods)
    {
        var methodDeclarations = methods.Select(m => $"        {m};").ToArray();
        return $@"
namespace TestNamespace
{{
    public interface {interfaceName}
    {{
{string.Join("\n", methodDeclarations)}
    }}
}}";
    }
}