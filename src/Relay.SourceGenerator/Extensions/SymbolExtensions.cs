using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator.Extensions;

/// <summary>
/// Extension methods for Roslyn symbols to reduce code duplication.
/// </summary>
public static class SymbolExtensions
{
    /// <summary>
    /// Checks if a type symbol implements a specific interface.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check</param>
    /// <param name="interfaceName">The interface name (without generic parameters)</param>
    /// <returns>True if the type implements the interface, false otherwise</returns>
    public static bool ImplementsInterface(this ITypeSymbol typeSymbol, string interfaceName)
    {
        if (typeSymbol == null) throw new ArgumentNullException(nameof(typeSymbol));
        if (string.IsNullOrWhiteSpace(interfaceName)) throw new ArgumentNullException(nameof(interfaceName));

        return typeSymbol.AllInterfaces.Any(i => i.Name == interfaceName);
    }

    /// <summary>
    /// Checks if a type symbol implements a specific interface with type arguments.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check</param>
    /// <param name="interfaceName">The interface name</param>
    /// <param name="typeArgumentCount">The expected number of type arguments</param>
    /// <returns>True if the type implements the interface with the specified type arguments, false otherwise</returns>
    public static bool ImplementsInterface(this ITypeSymbol typeSymbol, string interfaceName, int typeArgumentCount)
    {
        if (typeSymbol == null) throw new ArgumentNullException(nameof(typeSymbol));
        if (string.IsNullOrWhiteSpace(interfaceName)) throw new ArgumentNullException(nameof(interfaceName));

        return typeSymbol.AllInterfaces.Any(i => 
            i.Name == interfaceName && 
            i.TypeArguments.Length == typeArgumentCount);
    }

    /// <summary>
    /// Gets the interface implementation with the specified name.
    /// </summary>
    /// <param name="typeSymbol">The type symbol</param>
    /// <param name="interfaceName">The interface name</param>
    /// <returns>The interface symbol if found, null otherwise</returns>
    public static INamedTypeSymbol? GetInterface(this ITypeSymbol typeSymbol, string interfaceName)
    {
        if (typeSymbol == null) throw new ArgumentNullException(nameof(typeSymbol));
        if (string.IsNullOrWhiteSpace(interfaceName)) throw new ArgumentNullException(nameof(interfaceName));

        return typeSymbol.AllInterfaces.FirstOrDefault(i => i.Name == interfaceName);
    }

    /// <summary>
    /// Gets the interface implementation with the specified name and type argument count.
    /// </summary>
    /// <param name="typeSymbol">The type symbol</param>
    /// <param name="interfaceName">The interface name</param>
    /// <param name="typeArgumentCount">The expected number of type arguments</param>
    /// <returns>The interface symbol if found, null otherwise</returns>
    public static INamedTypeSymbol? GetInterface(this ITypeSymbol typeSymbol, string interfaceName, int typeArgumentCount)
    {
        if (typeSymbol == null) throw new ArgumentNullException(nameof(typeSymbol));
        if (string.IsNullOrWhiteSpace(interfaceName)) throw new ArgumentNullException(nameof(interfaceName));

        return typeSymbol.AllInterfaces.FirstOrDefault(i => 
            i.Name == interfaceName && 
            i.TypeArguments.Length == typeArgumentCount);
    }

    /// <summary>
    /// Checks if a method symbol has a specific attribute.
    /// </summary>
    /// <param name="methodSymbol">The method symbol</param>
    /// <param name="attributeName">The attribute name (without "Attribute" suffix)</param>
    /// <returns>True if the method has the attribute, false otherwise</returns>
    public static bool HasAttribute(this IMethodSymbol methodSymbol, string attributeName)
    {
        if (methodSymbol == null) throw new ArgumentNullException(nameof(methodSymbol));
        if (string.IsNullOrWhiteSpace(attributeName)) throw new ArgumentNullException(nameof(attributeName));

        var fullName = attributeName.EndsWith("Attribute") ? attributeName : $"{attributeName}Attribute";
        
        return methodSymbol.GetAttributes().Any(a => 
            a.AttributeClass?.Name == attributeName || 
            a.AttributeClass?.Name == fullName);
    }

    /// <summary>
    /// Gets an attribute from a method symbol.
    /// </summary>
    /// <param name="methodSymbol">The method symbol</param>
    /// <param name="attributeName">The attribute name (without "Attribute" suffix)</param>
    /// <returns>The attribute data if found, null otherwise</returns>
    public static AttributeData? GetAttribute(this IMethodSymbol methodSymbol, string attributeName)
    {
        if (methodSymbol == null) throw new ArgumentNullException(nameof(methodSymbol));
        if (string.IsNullOrWhiteSpace(attributeName)) throw new ArgumentNullException(nameof(attributeName));

        var fullName = attributeName.EndsWith("Attribute") ? attributeName : $"{attributeName}Attribute";
        
        return methodSymbol.GetAttributes().FirstOrDefault(a => 
            a.AttributeClass?.Name == attributeName || 
            a.AttributeClass?.Name == fullName);
    }

    /// <summary>
    /// Checks if a type symbol is a Task or ValueTask.
    /// </summary>
    /// <param name="typeSymbol">The type symbol</param>
    /// <returns>True if the type is Task or ValueTask, false otherwise</returns>
    public static bool IsTaskType(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null) throw new ArgumentNullException(nameof(typeSymbol));

        return typeSymbol.Name == "Task" || typeSymbol.Name == "ValueTask";
    }

    /// <summary>
    /// Checks if a type symbol is a generic Task or ValueTask.
    /// </summary>
    /// <param name="typeSymbol">The type symbol</param>
    /// <returns>True if the type is Task&lt;T&gt; or ValueTask&lt;T&gt;, false otherwise</returns>
    public static bool IsGenericTaskType(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null) throw new ArgumentNullException(nameof(typeSymbol));

        if (typeSymbol is not INamedTypeSymbol namedType) return false;

        return (namedType.Name == "Task" || namedType.Name == "ValueTask") && 
               namedType.TypeArguments.Length == 1;
    }

    /// <summary>
    /// Gets the result type from a Task&lt;T&gt; or ValueTask&lt;T&gt;.
    /// </summary>
    /// <param name="typeSymbol">The type symbol</param>
    /// <returns>The result type if the symbol is a generic task, null otherwise</returns>
    public static ITypeSymbol? GetTaskResultType(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null) throw new ArgumentNullException(nameof(typeSymbol));

        if (typeSymbol is INamedTypeSymbol namedType && 
            (namedType.Name == "Task" || namedType.Name == "ValueTask") && 
            namedType.TypeArguments.Length == 1)
        {
            return namedType.TypeArguments[0];
        }

        return null;
    }

    /// <summary>
    /// Checks if a method symbol is async.
    /// </summary>
    /// <param name="methodSymbol">The method symbol</param>
    /// <returns>True if the method is async, false otherwise</returns>
    public static bool IsAsync(this IMethodSymbol methodSymbol)
    {
        if (methodSymbol == null) throw new ArgumentNullException(nameof(methodSymbol));

        return methodSymbol.IsAsync || methodSymbol.ReturnType.IsTaskType();
    }

    /// <summary>
    /// Gets the full namespace of a symbol.
    /// </summary>
    /// <param name="symbol">The symbol</param>
    /// <returns>The full namespace, or empty string if no namespace</returns>
    public static string GetFullNamespace(this ISymbol symbol)
    {
        if (symbol == null) throw new ArgumentNullException(nameof(symbol));

        var parts = new List<string>();
        var current = symbol.ContainingNamespace;

        while (current != null && !current.IsGlobalNamespace)
        {
            parts.Insert(0, current.Name);
            current = current.ContainingNamespace;
        }

        return string.Join(".", parts);
    }

    /// <summary>
    /// Gets the full type name including namespace.
    /// </summary>
    /// <param name="typeSymbol">The type symbol</param>
    /// <returns>The full type name</returns>
    public static string GetFullTypeName(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null) throw new ArgumentNullException(nameof(typeSymbol));

        var ns = typeSymbol.GetFullNamespace();
        return string.IsNullOrEmpty(ns) ? typeSymbol.Name : $"{ns}.{typeSymbol.Name}";
    }

    /// <summary>
    /// Checks if a parameter is a CancellationToken.
    /// </summary>
    /// <param name="parameter">The parameter symbol</param>
    /// <returns>True if the parameter is a CancellationToken, false otherwise</returns>
    public static bool IsCancellationToken(this IParameterSymbol parameter)
    {
        if (parameter == null) throw new ArgumentNullException(nameof(parameter));

        return parameter.Type.Name == "CancellationToken" &&
               parameter.Type.ContainingNamespace?.ToDisplayString() == "System.Threading";
    }

    /// <summary>
    /// Checks if a type is accessible from the given location.
    /// </summary>
    /// <param name="typeSymbol">The type symbol</param>
    /// <param name="fromSymbol">The symbol from which accessibility is checked</param>
    /// <returns>True if the type is accessible, false otherwise</returns>
    public static bool IsAccessibleFrom(this ITypeSymbol typeSymbol, ISymbol fromSymbol)
    {
        if (typeSymbol == null) throw new ArgumentNullException(nameof(typeSymbol));
        if (fromSymbol == null) throw new ArgumentNullException(nameof(fromSymbol));

        return typeSymbol.DeclaredAccessibility switch
        {
            Accessibility.Public => true,
            Accessibility.Internal => SymbolEqualityComparer.Default.Equals(
                typeSymbol.ContainingAssembly, 
                fromSymbol.ContainingAssembly),
            Accessibility.Private => SymbolEqualityComparer.Default.Equals(
                typeSymbol.ContainingType, 
                fromSymbol.ContainingType),
            _ => false
        };
    }
}
