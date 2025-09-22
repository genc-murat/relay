using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Helper methods for creating test compilations.
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Creates a compilation from the provided source code.
        /// </summary>
        /// <param name="source">The source code to compile.</param>
        /// <returns>A tuple containing the compilation and any diagnostics.</returns>
        public static (Compilation compilation, IEnumerable<Diagnostic> diagnostics) CreateCompilation(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: new[] { syntaxTree },
                references: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location)
                },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var diagnostics = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning);

            return (compilation, diagnostics);
        }
    }
}