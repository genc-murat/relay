using System;
using System.Collections.Generic;
using System.Text;

namespace Relay.SourceGenerator.Generators;

/// <summary>
/// Strategy interface for code generation.
/// Follows the Strategy Pattern and Open/Closed Principle.
/// New generation strategies can be added without modifying existing code.
/// </summary>
public interface ICodeGenerationStrategy
{
    /// <summary>
    /// Gets the name of this generation strategy.
    /// </summary>
    string StrategyName { get; }

    /// <summary>
    /// Determines if this strategy can be applied to the given context.
    /// </summary>
    /// <param name="context">The generation context</param>
    /// <returns>True if the strategy can be applied, false otherwise</returns>
    bool CanApply(ICodeGenerationContext context);

    /// <summary>
    /// Applies the generation strategy to the given context.
    /// </summary>
    /// <param name="context">The generation context</param>
    /// <param name="builder">The string builder to append generated code to</param>
    void Apply(ICodeGenerationContext context, StringBuilder builder);
}

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
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        
        return _data.TryGetValue(key, out var value) && value is T typedValue
            ? typedValue
            : default;
    }

    /// <inheritdoc/>
    public void SetData<T>(string key, T value)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));
        
        _data[key] = value;
    }
}

/// <summary>
/// Base class for code generation strategies.
/// Provides common functionality for all strategies.
/// </summary>
public abstract class CodeGenerationStrategyBase : ICodeGenerationStrategy
{
    /// <inheritdoc/>
    public abstract string StrategyName { get; }

    /// <inheritdoc/>
    public abstract bool CanApply(ICodeGenerationContext context);

    /// <inheritdoc/>
    public abstract void Apply(ICodeGenerationContext context, StringBuilder builder);

    /// <summary>
    /// Appends an indented line to the builder.
    /// </summary>
    /// <param name="builder">The string builder</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <param name="line">The line to append</param>
    protected void AppendIndented(StringBuilder builder, int indentLevel, string line)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        
        builder.Append(new string(' ', indentLevel * 4));
        builder.AppendLine(line);
    }

    /// <summary>
    /// Appends XML documentation comment.
    /// </summary>
    /// <param name="builder">The string builder</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <param name="summary">The summary text</param>
    protected void AppendXmlDoc(StringBuilder builder, int indentLevel, string summary)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrEmpty(summary)) return;

        AppendIndented(builder, indentLevel, "/// <summary>");
        AppendIndented(builder, indentLevel, $"/// {summary}");
        AppendIndented(builder, indentLevel, "/// </summary>");
    }
}
