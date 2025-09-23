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

            // Minimal Relay.Core stubs to satisfy references in test sources
            var relayCoreStubs = CSharpSyntaxTree.ParseText(@"
using System;
namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    public interface INotification { }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HandleAttribute : Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ExposeAsEndpointAttribute : Attribute
    {
        public string? Route { get; set; }
        public string HttpMethod { get; set; } = ""POST"";
        public string? Version { get; set; }
    }
}
");

            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: new[] { relayCoreStubs, syntaxTree },
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