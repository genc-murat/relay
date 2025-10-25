using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator;

/// <summary>
/// Compilation context for the Relay source generator.
/// Provides access to compilation information and helper methods with aggressive caching.
/// Thread-safe for concurrent access.
/// </summary>
public class RelayCompilationContext
{
    private readonly ConcurrentDictionary<SyntaxTree, Lazy<SemanticModel>> _semanticModelCache = new();
    private readonly ConcurrentDictionary<string, Lazy<INamedTypeSymbol?>> _typeCache = new();
    private Lazy<bool>? _hasRelayCoreReference;

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
    /// Gets the semantic model for a syntax tree with thread-safe caching.
    /// Uses Lazy&lt;T&gt; to ensure semantic model is created only once per syntax tree.
    /// </summary>
    public SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
    {
        var lazy = _semanticModelCache.GetOrAdd(
            syntaxTree,
            tree => new Lazy<SemanticModel>(
                () => Compilation.GetSemanticModel(tree),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return lazy.Value;
    }

    /// <summary>
    /// Finds a type by its full name with thread-safe caching.
    /// Uses Lazy&lt;T&gt; to ensure type lookup is performed only once per type name.
    /// </summary>
    public INamedTypeSymbol? FindType(string fullTypeName)
    {
        var lazy = _typeCache.GetOrAdd(
            fullTypeName,
            typeName => new Lazy<INamedTypeSymbol?>(
                () => Compilation.GetTypeByMetadataName(typeName),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return lazy.Value;
    }

    /// <summary>
    /// Checks if the compilation references the Relay.Core assembly (thread-safe cached).
    /// Uses Lazy&lt;bool&gt; to ensure computation happens only once across all threads.
    /// </summary>
    public bool HasRelayCoreReference()
    {
        // Initialize Lazy<bool> if not already done or if reset
        _hasRelayCoreReference ??= new Lazy<bool>(() => ComputeHasRelayCoreReference(), LazyThreadSafetyMode.ExecutionAndPublication);
        return _hasRelayCoreReference.Value;
    }

    /// <summary>
    /// Computes whether Relay.Core is referenced. Called by Lazy&lt;bool&gt; initialization.
    /// </summary>
    private bool ComputeHasRelayCoreReference()
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

    /// <summary>
    /// Clears all internal caches. Useful for testing.
    /// WARNING: Not thread-safe. Should only be called when no other threads are accessing this context.
    /// </summary>
    public void ClearCaches()
    {
        _semanticModelCache.Clear();
        _typeCache.Clear();
        // Reset the Lazy<bool> to allow re-evaluation
        _hasRelayCoreReference = null;
    }
}