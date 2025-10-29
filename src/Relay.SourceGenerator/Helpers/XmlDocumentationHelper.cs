using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Relay.SourceGenerator.Extensions;

namespace Relay.SourceGenerator.Helpers;

/// <summary>
/// Helper class for generating XML documentation comments in generated code.
/// Ensures consistent documentation across all generated files.
/// </summary>
public static class XmlDocumentationHelper
{
    /// <summary>
    /// Generates a summary XML documentation comment.
    /// </summary>
    /// <param name="summary">The summary text</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <returns>The formatted XML documentation</returns>
    public static string GenerateSummary(string summary, int indentLevel = 0)
    {
        if (string.IsNullOrWhiteSpace(summary)) return string.Empty;

        var builder = new StringBuilder();
        builder.AppendXmlSummary(indentLevel, summary);
        return builder.ToString();
    }

    /// <summary>
    /// Generates a complete method documentation with summary, parameters, and returns.
    /// </summary>
    /// <param name="summary">The method summary</param>
    /// <param name="parameters">The parameters as (name, description) tuples</param>
    /// <param name="returns">The return value description</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <returns>The formatted XML documentation</returns>
    public static string GenerateMethodDocumentation(
        string summary,
        IEnumerable<(string Name, string Description)>? parameters = null,
        string? returns = null,
        int indentLevel = 0)
    {
        var builder = new StringBuilder();

        // Summary
        builder.AppendXmlSummary(indentLevel, summary);

        // Parameters
        if (parameters != null)
        {
            foreach (var (name, description) in parameters)
            {
                builder.AppendXmlParam(indentLevel, name, description);
            }
        }

        // Returns
        if (!string.IsNullOrWhiteSpace(returns))
        {
            builder.AppendXmlReturns(indentLevel, returns!);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Generates a property documentation with summary and value.
    /// </summary>
    /// <param name="summary">The property summary</param>
    /// <param name="value">The value description</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <returns>The formatted XML documentation</returns>
    public static string GeneratePropertyDocumentation(
        string summary,
        string? value = null,
        int indentLevel = 0)
    {
        var builder = new StringBuilder();

        // Summary
        builder.AppendXmlSummary(indentLevel, summary);

        // Value
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendIndentedLine(indentLevel, $"/// <value>{value}</value>");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Generates a class documentation with summary and remarks.
    /// </summary>
    /// <param name="summary">The class summary</param>
    /// <param name="remarks">Optional remarks</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <returns>The formatted XML documentation</returns>
    public static string GenerateClassDocumentation(
        string summary,
        string? remarks = null,
        int indentLevel = 0)
    {
        var builder = new StringBuilder();

        // Summary
        builder.AppendXmlSummary(indentLevel, summary);

        // Remarks
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            builder.AppendIndentedLine(indentLevel, "/// <remarks>");
            
            var lines = remarks!.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                builder.AppendIndentedLine(indentLevel, $"/// {line.Trim()}");
            }
            
            builder.AppendIndentedLine(indentLevel, "/// </remarks>");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Generates an exception documentation tag.
    /// </summary>
    /// <param name="exceptionType">The exception type</param>
    /// <param name="description">The description of when the exception is thrown</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <returns>The formatted XML documentation</returns>
    public static string GenerateException(
        string exceptionType,
        string description,
        int indentLevel = 0)
    {
        if (string.IsNullOrWhiteSpace(exceptionType))
        {
            throw new ArgumentNullException(nameof(exceptionType));
        }
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentNullException(nameof(description));
        }

        var builder = new StringBuilder();
        builder.AppendIndentedLine(indentLevel, $"/// <exception cref=\"{exceptionType}\">");
        builder.AppendIndentedLine(indentLevel, $"/// {description}");
        builder.AppendIndentedLine(indentLevel, "/// </exception>");
        return builder.ToString();
    }

    /// <summary>
    /// Generates an example documentation tag.
    /// </summary>
    /// <param name="description">The example description</param>
    /// <param name="code">The example code</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <returns>The formatted XML documentation</returns>
    public static string GenerateExample(
        string description,
        string code,
        int indentLevel = 0)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentNullException(nameof(code));
        }

        var builder = new StringBuilder();
        builder.AppendIndentedLine(indentLevel, "/// <example>");
        
        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.AppendIndentedLine(indentLevel, $"/// {description}");
        }
        
        builder.AppendIndentedLine(indentLevel, "/// <code>");
        
        var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            builder.AppendIndentedLine(indentLevel, $"/// {line}");
        }
        
        builder.AppendIndentedLine(indentLevel, "/// </code>");
        builder.AppendIndentedLine(indentLevel, "/// </example>");
        return builder.ToString();
    }

    /// <summary>
    /// Generates a see reference tag.
    /// </summary>
    /// <param name="reference">The reference to link to</param>
    /// <returns>The formatted see tag</returns>
    public static string GenerateSeeTag(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new ArgumentNullException(nameof(reference));
        }

        return $"<see cref=\"{reference}\"/>";
    }

    /// <summary>
    /// Generates a seealso reference tag.
    /// </summary>
    /// <param name="reference">The reference to link to</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <returns>The formatted seealso tag</returns>
    public static string GenerateSeeAlsoTag(string reference, int indentLevel = 0)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new ArgumentNullException(nameof(reference));
        }

        var builder = new StringBuilder();
        builder.AppendIndentedLine(indentLevel, $"/// <seealso cref=\"{reference}\"/>");
        return builder.ToString();
    }

    /// <summary>
    /// Generates a typeparam documentation tag.
    /// </summary>
    /// <param name="name">The type parameter name</param>
    /// <param name="description">The type parameter description</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <returns>The formatted typeparam tag</returns>
    public static string GenerateTypeParam(
        string name,
        string description,
        int indentLevel = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentNullException(nameof(description));
        }

        var builder = new StringBuilder();
        builder.AppendIndentedLine(indentLevel, $"/// <typeparam name=\"{name}\">{description}</typeparam>");
        return builder.ToString();
    }

    /// <summary>
    /// Generates an inheritdoc tag.
    /// </summary>
    /// <param name="indentLevel">The indentation level</param>
    /// <returns>The formatted inheritdoc tag</returns>
    public static string GenerateInheritDoc(int indentLevel = 0)
    {
        var builder = new StringBuilder();
        builder.AppendIndentedLine(indentLevel, "/// <inheritdoc/>");
        return builder.ToString();
    }

    /// <summary>
    /// Generates documentation for a generated class.
    /// </summary>
    /// <param name="className">The class name</param>
    /// <param name="purpose">The purpose of the class</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <returns>The formatted XML documentation</returns>
    public static string GenerateGeneratedClassDocumentation(
        string className,
        string purpose,
        int indentLevel = 0)
    {
        var summary = $"Generated {className} class.";
        var remarks = $"This class is automatically generated by Relay.SourceGenerator.\n" +
                     $"Purpose: {purpose}\n" +
                     $"Do not modify this file directly.";

        return GenerateClassDocumentation(summary, remarks, indentLevel);
    }

    /// <summary>
    /// Generates documentation for a generated method.
    /// </summary>
    /// <param name="methodName">The method name</param>
    /// <param name="purpose">The purpose of the method</param>
    /// <param name="parameters">The method parameters</param>
    /// <param name="returns">The return value description</param>
    /// <param name="indentLevel">The indentation level</param>
    /// <returns>The formatted XML documentation</returns>
    public static string GenerateGeneratedMethodDocumentation(
        string methodName,
        string purpose,
        IEnumerable<(string Name, string Description)>? parameters = null,
        string? returns = null,
        int indentLevel = 0)
    {
        var summary = $"{purpose}";
        return GenerateMethodDocumentation(summary, parameters, returns, indentLevel);
    }

    /// <summary>
    /// Generates a standard auto-generated warning comment.
    /// </summary>
    /// <param name="indentLevel">The indentation level</param>
    /// <returns>The formatted warning comment</returns>
    public static string GenerateAutoGeneratedWarning(int indentLevel = 0)
    {
        var builder = new StringBuilder();
        builder.AppendIndentedLine(indentLevel, "/// <remarks>");
        builder.AppendIndentedLine(indentLevel, "/// This code is automatically generated by Relay.SourceGenerator.");
        builder.AppendIndentedLine(indentLevel, "/// Do not modify this file directly as your changes will be overwritten.");
        builder.AppendIndentedLine(indentLevel, "/// </remarks>");
        return builder.ToString();
    }

    /// <summary>
    /// Wraps text to fit within a specified line length for documentation.
    /// </summary>
    /// <param name="text">The text to wrap</param>
    /// <param name="maxLength">The maximum line length</param>
    /// <returns>The wrapped text lines</returns>
    public static IEnumerable<string> WrapText(string text, int maxLength = 80)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            if (currentLine.Length + word.Length + 1 > maxLength)
            {
                if (currentLine.Length > 0)
                {
                    yield return currentLine.ToString();
                    currentLine.Clear();
                }
            }

            if (currentLine.Length > 0)
            {
                currentLine.Append(' ');
            }
            currentLine.Append(word);
        }

        if (currentLine.Length > 0)
        {
            yield return currentLine.ToString();
        }
    }
}
