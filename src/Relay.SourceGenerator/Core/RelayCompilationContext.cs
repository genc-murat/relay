using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Compilation context for the Relay source generator.
    /// Provides access to compilation information and helper methods with aggressive caching.
    /// </summary>
    public class RelayCompilationContext
    {
        private readonly ConcurrentDictionary<SyntaxTree, SemanticModel> _semanticModelCache = new();
        private readonly ConcurrentDictionary<string, INamedTypeSymbol?> _typeCache = new();
        private bool? _hasRelayCoreReference;

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
        /// Gets the semantic model for a syntax tree with caching.
        /// </summary>
        public SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
        {
            return _semanticModelCache.GetOrAdd(syntaxTree, tree => Compilation.GetSemanticModel(tree));
        }

        /// <summary>
        /// Finds a type by its full name with caching.
        /// </summary>
        public INamedTypeSymbol? FindType(string fullTypeName)
        {
            return _typeCache.GetOrAdd(fullTypeName, typeName => Compilation.GetTypeByMetadataName(typeName));
        }

        /// <summary>
        /// Checks if the compilation references the Relay.Core assembly (cached).
        /// </summary>
        public bool HasRelayCoreReference()
        {
            if (_hasRelayCoreReference.HasValue)
                return _hasRelayCoreReference.Value;

            // Allow the generator project itself to compile without Relay.Core
            if (string.Equals(AssemblyName, "Relay.SourceGenerator", StringComparison.OrdinalIgnoreCase))
            {
                _hasRelayCoreReference = true;
                return true;
            }

            if (Compilation.ReferencedAssemblyNames
                .Any(name => name.Name.Equals("Relay.Core", StringComparison.OrdinalIgnoreCase)))
            {
                _hasRelayCoreReference = true;
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
                    _hasRelayCoreReference = true;
                    return true;
                }
            }

            _hasRelayCoreReference = false;
            return false;
        }

        /// <summary>
        /// Clears all internal caches. Useful for testing.
        /// </summary>
        public void ClearCaches()
        {
            _semanticModelCache.Clear();
            _typeCache.Clear();
            _hasRelayCoreReference = null;
        }
    }
}