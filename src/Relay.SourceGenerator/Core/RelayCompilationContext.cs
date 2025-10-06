using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Compilation context for the Relay source generator.
    /// Provides access to compilation information and helper methods.
    /// </summary>
    public class RelayCompilationContext
    {
        public Compilation Compilation { get; }
        public CancellationToken CancellationToken { get; }
        public string AssemblyName { get; }

        public RelayCompilationContext(Compilation compilation, CancellationToken cancellationToken)
        {
            Compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
            CancellationToken = cancellationToken;
            AssemblyName = compilation.AssemblyName ?? "Unknown";
        }

        /// <summary>
        /// Gets the semantic model for a syntax tree.
        /// </summary>
        public SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
        {
            return Compilation.GetSemanticModel(syntaxTree);
        }

        /// <summary>
        /// Finds a type by its full name.
        /// </summary>
        public INamedTypeSymbol? FindType(string fullTypeName)
        {
            return Compilation.GetTypeByMetadataName(fullTypeName);
        }

        /// <summary>
        /// Checks if the compilation references the Relay.Core assembly.
        /// </summary>
        public bool HasRelayCoreReference()
        {
            // Allow the generator project itself to compile without Relay.Core
            if (string.Equals(AssemblyName, "Relay.SourceGenerator", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (Compilation.ReferencedAssemblyNames
                .Any(name => name.Name.Equals("Relay.Core", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Fallback: detect by presence of known Relay.Core types in the compilation
            var knownTypeNames = new[]
            {
                "Relay.Core.Contracts.Core.IRelay",
                "Relay.Core.Contracts.Requests.IRequest`1",
                "Relay.Core.Contracts.Requests.INotification"
            };

            foreach (var typeName in knownTypeNames)
            {
                if (Compilation.GetTypeByMetadataName(typeName) is not null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}