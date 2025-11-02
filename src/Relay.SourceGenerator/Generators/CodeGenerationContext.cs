using System;
using System.Collections.Generic;

namespace Relay.SourceGenerator.Generators;

/// <summary>
/// Default implementation of <see cref="ICodeGenerationContext"/>.
/// </summary>
public class CodeGenerationContext : ICodeGenerationContext
{
    private readonly Dictionary<string, object> _data = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeGenerationContext"/> class.
    /// </summary>
    /// <param name="discoveryResult">The handler discovery result</param>
    /// <param name="options">The generation options</param>
    /// <param name="compilationContext">The compilation context</param>
    public CodeGenerationContext(
        HandlerDiscoveryResult discoveryResult,
        GenerationOptions options,
        RelayCompilationContext compilationContext)
    {
        DiscoveryResult = discoveryResult ?? throw new ArgumentNullException(nameof(discoveryResult));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        CompilationContext = compilationContext ?? throw new ArgumentNullException(nameof(compilationContext));
    }

    /// <inheritdoc/>
    public HandlerDiscoveryResult DiscoveryResult { get; }

    /// <inheritdoc/>
    public GenerationOptions Options { get; }

    /// <inheritdoc/>
    public RelayCompilationContext CompilationContext { get; }

    /// <inheritdoc/>
    public T? GetData<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
        
        return _data.TryGetValue(key, out var value) && value is T typedValue
            ? typedValue
            : default;
    }

    /// <inheritdoc/>
    public void SetData<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));
        
        _data[key] = value;
    }
}
