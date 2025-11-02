using System;
using System.Text;

namespace Relay.SourceGenerator.Generators;

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
