using System.Linq;
using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator.Validators;

/// <summary>
/// Validates types used in handlers and pipelines.
/// </summary>
internal static class TypeValidator
{
    /// <summary>
    /// Checks if a type is a valid request type (implements IRequest or IRequest<T>).
    /// </summary>
    public static bool IsValidRequestType(ITypeSymbol type)
    {
        var interfaces = type.AllInterfaces;

        var requestInterfaceCount = interfaces.Count(i =>
            (i.Name == "IRequest" && i.TypeArguments.Length == 0) ||
            (i.Name == "IRequest" && i.TypeArguments.Length == 1) ||
            (i.Name == "IStreamRequest" && i.TypeArguments.Length == 1));

        // A type is valid if it implements exactly one request interface
        return requestInterfaceCount == 1;
    }

    /// <summary>
    /// Checks if a type is a valid notification type (implements INotification).
    /// </summary>
    public static bool IsValidNotificationType(ITypeSymbol type)
    {
        var interfaces = type.AllInterfaces;
        return interfaces.Any(i => i.Name == "INotification" && i.TypeArguments.Length == 0);
    }

    /// <summary>
    /// Checks if a return type is valid for handlers that return a response.
    /// </summary>
    public static bool IsValidHandlerReturnType(ITypeSymbol returnType, ITypeSymbol expectedResponseType)
    {
        // Valid return types: TResponse, Task<TResponse>, ValueTask<TResponse>
        if (SymbolEqualityComparer.Default.Equals(returnType, expectedResponseType))
            return true;

        if (returnType is INamedTypeSymbol namedReturnType)
        {
            if (namedReturnType.Name == "Task" && namedReturnType.TypeArguments.Length == 1)
            {
                return SymbolEqualityComparer.Default.Equals(namedReturnType.TypeArguments[0], expectedResponseType);
            }

            if (namedReturnType.Name == "ValueTask" && namedReturnType.TypeArguments.Length == 1)
            {
                return SymbolEqualityComparer.Default.Equals(namedReturnType.TypeArguments[0], expectedResponseType);
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a return type is valid for handlers that don't return a response.
    /// </summary>
    public static bool IsValidVoidHandlerReturnType(ITypeSymbol returnType)
    {
        // Valid return types: Task, ValueTask (without generic parameters)
        if (returnType is INamedTypeSymbol namedReturnType)
        {
            return (namedReturnType.Name == "Task" && namedReturnType.TypeArguments.Length == 0) ||
                   (namedReturnType.Name == "ValueTask" && namedReturnType.TypeArguments.Length == 0);
        }

        return returnType.Name == "Task" || returnType.Name == "ValueTask";
    }

    /// <summary>
    /// Checks if a return type is valid for stream handlers.
    /// </summary>
    public static bool IsValidStreamHandlerReturnType(ITypeSymbol returnType, ITypeSymbol expectedResponseType)
    {
        // Valid return type: IAsyncEnumerable<TResponse>
        if (returnType is INamedTypeSymbol namedReturnType)
        {
            if (namedReturnType.Name == "IAsyncEnumerable" && namedReturnType.TypeArguments.Length == 1)
            {
                return SymbolEqualityComparer.Default.Equals(namedReturnType.TypeArguments[0], expectedResponseType);
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a return type is valid for notification handlers.
    /// </summary>
    public static bool IsValidNotificationReturnType(ITypeSymbol returnType)
    {
        // Valid return types: Task, ValueTask
        return returnType.Name == "Task" || returnType.Name == "ValueTask";
    }
}
