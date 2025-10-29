using System;
using System.Linq;
using System.Text;

namespace Relay.SourceGenerator.Extensions;

/// <summary>
/// Extension methods for StringBuilder to reduce code duplication.
/// </summary>
public static class StringBuilderExtensions
{
    /// <summary>
    /// Appends an indented line to the StringBuilder.
    /// </summary>
    /// <param name="builder">The StringBuilder instance</param>
    /// <param name="indentLevel">The indentation level (each level is 4 spaces)</param>
    /// <param name="line">The line to append</param>
    /// <returns>The StringBuilder for method chaining</returns>
    public static StringBuilder AppendIndentedLine(this StringBuilder builder, int indentLevel, string line)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (indentLevel < 0) throw new ArgumentOutOfRangeException(nameof(indentLevel));

        if (indentLevel > 0)
        {
            builder.Append(new string(' ', indentLevel * 4));
        }
        builder.AppendLine(line);
        return builder;
    }

    /// <summary>
    /// Appends multiple indented lines to the StringBuilder.
    /// </summary>
    /// <param name="builder">The StringBuilder instance</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <param name="lines">The lines to append</param>
    /// <returns>The StringBuilder for method chaining</returns>
    public static StringBuilder AppendIndentedLines(this StringBuilder builder, int indentLevel, params string[] lines)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (lines == null) throw new ArgumentNullException(nameof(lines));

        foreach (var line in lines)
        {
            builder.AppendIndentedLine(indentLevel, line);
        }
        return builder;
    }

    /// <summary>
    /// Appends an XML documentation summary comment.
    /// </summary>
    /// <param name="builder">The StringBuilder instance</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <param name="summary">The summary text</param>
    /// <returns>The StringBuilder for method chaining</returns>
    public static StringBuilder AppendXmlSummary(this StringBuilder builder, int indentLevel, string summary)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(summary)) return builder;

        builder.AppendIndentedLine(indentLevel, "/// <summary>");
        
        // Split multi-line summaries
        var lines = summary.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            builder.AppendIndentedLine(indentLevel, $"/// {line.Trim()}");
        }
        
        builder.AppendIndentedLine(indentLevel, "/// </summary>");
        return builder;
    }

    /// <summary>
    /// Appends an XML documentation parameter comment.
    /// </summary>
    /// <param name="builder">The StringBuilder instance</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <param name="paramName">The parameter name</param>
    /// <param name="description">The parameter description</param>
    /// <returns>The StringBuilder for method chaining</returns>
    public static StringBuilder AppendXmlParam(this StringBuilder builder, int indentLevel, string paramName, string description)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(paramName)) throw new ArgumentNullException(nameof(paramName));
        if (string.IsNullOrWhiteSpace(description)) return builder;

        builder.AppendIndentedLine(indentLevel, $"/// <param name=\"{paramName}\">{description}</param>");
        return builder;
    }

    /// <summary>
    /// Appends an XML documentation returns comment.
    /// </summary>
    /// <param name="builder">The StringBuilder instance</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <param name="description">The return value description</param>
    /// <returns>The StringBuilder for method chaining</returns>
    public static StringBuilder AppendXmlReturns(this StringBuilder builder, int indentLevel, string description)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(description)) return builder;

        builder.AppendIndentedLine(indentLevel, $"/// <returns>{description}</returns>");
        return builder;
    }

    /// <summary>
    /// Appends a file header with auto-generated comment and timestamp.
    /// </summary>
    /// <param name="builder">The StringBuilder instance</param>
    /// <param name="generatorName">The name of the generator</param>
    /// <param name="includeTimestamp">Whether to include a timestamp</param>
    /// <returns>The StringBuilder for method chaining</returns>
    public static StringBuilder AppendFileHeader(this StringBuilder builder, string generatorName, bool includeTimestamp = true)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.AppendLine("// <auto-generated />");
        builder.AppendLine($"// Generated by {generatorName ?? "Relay.SourceGenerator"}");
        
        if (includeTimestamp)
        {
            builder.AppendLine($"// Generation time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        }
        
        builder.AppendLine();
        return builder;
    }

    /// <summary>
    /// Appends a namespace declaration with opening brace.
    /// </summary>
    /// <param name="builder">The StringBuilder instance</param>
    /// <param name="namespaceName">The namespace name</param>
    /// <returns>The StringBuilder for method chaining</returns>
    public static StringBuilder AppendNamespaceStart(this StringBuilder builder, string namespaceName)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(namespaceName)) throw new ArgumentNullException(nameof(namespaceName));

        builder.AppendLine($"namespace {namespaceName}");
        builder.AppendLine("{");
        return builder;
    }

    /// <summary>
    /// Appends a namespace closing brace.
    /// </summary>
    /// <param name="builder">The StringBuilder instance</param>
    /// <returns>The StringBuilder for method chaining</returns>
    public static StringBuilder AppendNamespaceEnd(this StringBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        builder.AppendLine("}");
        return builder;
    }

    /// <summary>
    /// Appends a class declaration with opening brace.
    /// </summary>
    /// <param name="builder">The StringBuilder instance</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <param name="accessibility">The accessibility modifier (e.g., "public", "internal")</param>
    /// <param name="className">The class name</param>
    /// <param name="modifiers">Additional modifiers (e.g., "static", "sealed")</param>
    /// <param name="baseTypes">Base types and interfaces</param>
    /// <returns>The StringBuilder for method chaining</returns>
    public static StringBuilder AppendClassStart(
        this StringBuilder builder,
        int indentLevel,
        string accessibility,
        string className,
        string? modifiers = null,
        params string[] baseTypes)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(className)) throw new ArgumentNullException(nameof(className));

        var declaration = $"{accessibility}";
        if (!string.IsNullOrWhiteSpace(modifiers))
        {
            declaration += $" {modifiers}";
        }
        declaration += $" class {className}";

        if (baseTypes != null && baseTypes.Length > 0)
        {
            declaration += $" : {string.Join(", ", baseTypes)}";
        }

        builder.AppendIndentedLine(indentLevel, declaration);
        builder.AppendIndentedLine(indentLevel, "{");
        return builder;
    }

    /// <summary>
    /// Appends a class closing brace.
    /// </summary>
    /// <param name="builder">The StringBuilder instance</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <returns>The StringBuilder for method chaining</returns>
    public static StringBuilder AppendClassEnd(this StringBuilder builder, int indentLevel)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        builder.AppendIndentedLine(indentLevel, "}");
        return builder;
    }

    /// <summary>
    /// Appends a method declaration with opening brace.
    /// </summary>
    /// <param name="builder">The StringBuilder instance</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <param name="accessibility">The accessibility modifier</param>
    /// <param name="returnType">The return type</param>
    /// <param name="methodName">The method name</param>
    /// <param name="parameters">The method parameters</param>
    /// <param name="modifiers">Additional modifiers (e.g., "static", "async")</param>
    /// <returns>The StringBuilder for method chaining</returns>
    public static StringBuilder AppendMethodStart(
        this StringBuilder builder,
        int indentLevel,
        string accessibility,
        string returnType,
        string methodName,
        string parameters = "",
        string? modifiers = null)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));

        var declaration = $"{accessibility}";
        if (!string.IsNullOrWhiteSpace(modifiers))
        {
            declaration += $" {modifiers}";
        }
        declaration += $" {returnType} {methodName}({parameters})";

        builder.AppendIndentedLine(indentLevel, declaration);
        builder.AppendIndentedLine(indentLevel, "{");
        return builder;
    }

    /// <summary>
    /// Appends a method closing brace.
    /// </summary>
    /// <param name="builder">The StringBuilder instance</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <returns>The StringBuilder for method chaining</returns>
    public static StringBuilder AppendMethodEnd(this StringBuilder builder, int indentLevel)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        builder.AppendIndentedLine(indentLevel, "}");
        return builder;
    }

    /// <summary>
    /// Appends using directives.
    /// </summary>
    /// <param name="builder">The StringBuilder instance</param>
    /// <param name="namespaces">The namespaces to import</param>
    /// <returns>The StringBuilder for method chaining</returns>
    public static StringBuilder AppendUsings(this StringBuilder builder, params string[] namespaces)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (namespaces == null || namespaces.Length == 0) return builder;

        foreach (var ns in namespaces.OrderBy(n => n))
        {
            builder.AppendLine($"using {ns};");
        }
        builder.AppendLine();
        return builder;
    }

    /// <summary>
    /// Appends a nullable reference type directive.
    /// </summary>
    /// <param name="builder">The StringBuilder instance</param>
    /// <param name="enable">Whether to enable nullable reference types</param>
    /// <returns>The StringBuilder for method chaining</returns>
    public static StringBuilder AppendNullableDirective(this StringBuilder builder, bool enable = true)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        
        builder.AppendLine(enable ? "#nullable enable" : "#nullable disable");
        builder.AppendLine();
        return builder;
    }
}
