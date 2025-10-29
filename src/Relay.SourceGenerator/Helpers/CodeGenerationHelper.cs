using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Relay.SourceGenerator.Extensions;

namespace Relay.SourceGenerator.Helpers;

/// <summary>
/// Helper class for common code generation operations.
/// Reduces code duplication across generators.
/// </summary>
public static class CodeGenerationHelper
{
    /// <summary>
    /// Generates a standard file header.
    /// </summary>
    /// <param name="generatorName">The name of the generator</param>
    /// <param name="includeTimestamp">Whether to include a timestamp</param>
    /// <returns>The file header string</returns>
    public static string GenerateFileHeader(string generatorName, bool includeTimestamp = true)
    {
        var builder = new StringBuilder();
        builder.AppendFileHeader(generatorName, includeTimestamp);
        return builder.ToString();
    }

    /// <summary>
    /// Generates using directives for common namespaces.
    /// </summary>
    /// <param name="includeRelay">Whether to include Relay namespaces</param>
    /// <param name="includeDI">Whether to include DI namespaces</param>
    /// <param name="includeAsync">Whether to include async namespaces</param>
    /// <param name="additionalNamespaces">Additional namespaces to include</param>
    /// <returns>The using directives string</returns>
    public static string GenerateUsings(
        bool includeRelay = true,
        bool includeDI = false,
        bool includeAsync = false,
        params string[] additionalNamespaces)
    {
        var namespaces = new List<string>
        {
            "System"
        };

        if (includeAsync)
        {
            namespaces.Add("System.Threading");
            namespaces.Add("System.Threading.Tasks");
        }

        if (includeDI)
        {
            namespaces.Add("Microsoft.Extensions.DependencyInjection");
        }

        if (includeRelay)
        {
            namespaces.Add("Relay.Core");
            namespaces.Add("Relay.Core.Contracts.Core");
            namespaces.Add("Relay.Core.Contracts.Requests");
        }

        if (additionalNamespaces != null && additionalNamespaces.Length > 0)
        {
            namespaces.AddRange(additionalNamespaces);
        }

        var builder = new StringBuilder();
        builder.AppendUsings(namespaces.ToArray());
        return builder.ToString();
    }

    /// <summary>
    /// Generates a method parameter list string.
    /// </summary>
    /// <param name="parameters">The parameters as (type, name) tuples</param>
    /// <returns>The parameter list string</returns>
    public static string GenerateParameterList(params (string Type, string Name)[] parameters)
    {
        if (parameters == null || parameters.Length == 0) return string.Empty;

        return string.Join(", ", parameters.Select(p => $"{p.Type} {p.Name}"));
    }

    /// <summary>
    /// Generates a method argument list string.
    /// </summary>
    /// <param name="arguments">The argument names</param>
    /// <returns>The argument list string</returns>
    public static string GenerateArgumentList(params string[] arguments)
    {
        if (arguments == null || arguments.Length == 0) return string.Empty;

        return string.Join(", ", arguments);
    }

    /// <summary>
    /// Generates a generic type parameter list string.
    /// </summary>
    /// <param name="typeParameters">The type parameter names</param>
    /// <returns>The type parameter list string</returns>
    public static string GenerateTypeParameterList(params string[] typeParameters)
    {
        if (typeParameters == null || typeParameters.Length == 0) return string.Empty;

        return $"<{string.Join(", ", typeParameters)}>";
    }

    /// <summary>
    /// Generates a property declaration.
    /// </summary>
    /// <param name="accessibility">The accessibility modifier</param>
    /// <param name="type">The property type</param>
    /// <param name="name">The property name</param>
    /// <param name="getter">The getter body (null for auto-property)</param>
    /// <param name="setter">The setter body (null for auto-property, empty for init-only)</param>
    /// <returns>The property declaration string</returns>
    public static string GenerateProperty(
        string accessibility,
        string type,
        string name,
        string? getter = null,
        string? setter = null)
    {
        var builder = new StringBuilder();
        builder.Append($"{accessibility} {type} {name}");

        if (getter == null && setter == null)
        {
            // Auto-property
            builder.Append(" { get; set; }");
        }
        else
        {
            builder.AppendLine();
            builder.AppendLine("{");

            if (getter != null)
            {
                builder.AppendLine($"    get {{ {getter} }}");
            }
            else
            {
                builder.AppendLine("    get;");
            }

            if (setter != null)
            {
                if (string.IsNullOrEmpty(setter))
                {
                    builder.AppendLine("    init;");
                }
                else
                {
                    builder.AppendLine($"    set {{ {setter} }}");
                }
            }

            builder.Append("}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Generates an attribute declaration.
    /// </summary>
    /// <param name="attributeName">The attribute name (without "Attribute" suffix)</param>
    /// <param name="arguments">The attribute arguments</param>
    /// <returns>The attribute declaration string</returns>
    public static string GenerateAttribute(string attributeName, params string[] arguments)
    {
        if (string.IsNullOrWhiteSpace(attributeName))
        {
            throw new ArgumentNullException(nameof(attributeName));
        }

        var name = attributeName.EndsWith("Attribute") 
            ? attributeName.Substring(0, attributeName.Length - 9) 
            : attributeName;

        if (arguments == null || arguments.Length == 0)
        {
            return $"[{name}]";
        }

        return $"[{name}({string.Join(", ", arguments)})]";
    }

    /// <summary>
    /// Escapes a string for use in generated code.
    /// </summary>
    /// <param name="value">The string to escape</param>
    /// <returns>The escaped string</returns>
    public static string EscapeString(string value)
    {
        if (value == null) return "null";

        return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }

    /// <summary>
    /// Generates a null-conditional operator chain.
    /// </summary>
    /// <param name="members">The member access chain</param>
    /// <returns>The null-conditional chain string</returns>
    public static string GenerateNullConditional(params string[] members)
    {
        if (members == null || members.Length == 0)
        {
            throw new ArgumentException("At least one member is required", nameof(members));
        }

        return string.Join("?.", members);
    }

    /// <summary>
    /// Generates a switch expression case.
    /// </summary>
    /// <param name="pattern">The pattern to match</param>
    /// <param name="expression">The result expression</param>
    /// <returns>The switch case string</returns>
    public static string GenerateSwitchCase(string pattern, string expression)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentNullException(nameof(pattern));
        }
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentNullException(nameof(expression));
        }

        return $"{pattern} => {expression}";
    }

    /// <summary>
    /// Generates a complete switch expression.
    /// </summary>
    /// <param name="switchValue">The value to switch on</param>
    /// <param name="cases">The switch cases as (pattern, expression) tuples</param>
    /// <param name="defaultCase">The default case expression</param>
    /// <returns>The switch expression string</returns>
    public static string GenerateSwitchExpression(
        string switchValue,
        IEnumerable<(string Pattern, string Expression)> cases,
        string? defaultCase = null)
    {
        if (string.IsNullOrWhiteSpace(switchValue))
        {
            throw new ArgumentNullException(nameof(switchValue));
        }
        if (cases == null)
        {
            throw new ArgumentNullException(nameof(cases));
        }

        var builder = new StringBuilder();
        builder.Append($"{switchValue} switch");
        builder.AppendLine();
        builder.AppendLine("{");

        foreach (var (pattern, expression) in cases)
        {
            builder.AppendLine($"    {GenerateSwitchCase(pattern, expression)},");
        }

        if (!string.IsNullOrWhiteSpace(defaultCase))
        {
            builder.AppendLine($"    _ => {defaultCase}");
        }

        builder.Append("}");

        return builder.ToString();
    }
}
