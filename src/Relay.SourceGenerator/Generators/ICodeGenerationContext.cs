namespace Relay.SourceGenerator.Generators;

/// <summary>
/// Context interface for code generation.
/// Provides all necessary information for code generation strategies.
/// </summary>
public interface ICodeGenerationContext
{
    /// <summary>
    /// Gets the handler discovery result.
    /// </summary>
    HandlerDiscoveryResult DiscoveryResult { get; }

    /// <summary>
    /// Gets the generation options.
    /// </summary>
    GenerationOptions Options { get; }

    /// <summary>
    /// Gets the compilation context.
    /// </summary>
    RelayCompilationContext CompilationContext { get; }

    /// <summary>
    /// Gets additional context data.
    /// </summary>
    /// <typeparam name="T">The type of data to retrieve</typeparam>
    /// <param name="key">The key for the data</param>
    /// <returns>The data if found, otherwise default value</returns>
    T? GetData<T>(string key);

    /// <summary>
    /// Sets additional context data.
    /// </summary>
    /// <typeparam name="T">The type of data to store</typeparam>
    /// <param name="key">The key for the data</param>
    /// <param name="value">The value to store</param>
    void SetData<T>(string key, T value);
}
