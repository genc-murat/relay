using System.Linq;
using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Extensions;

namespace Relay.SourceGenerator.Helpers;

/// <summary>
/// Helper class for common type checking operations.
/// Reduces code duplication across validators and generators.
/// </summary>
public static class TypeHelper
{
    /// <summary>
    /// Checks if a type is a valid request type.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check</param>
    /// <returns>True if the type is a valid request type, false otherwise</returns>
    public static bool IsRequestType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null) return false;

        // Check for IRequest or IRequest<T>
        return typeSymbol.ImplementsInterface("IRequest") || 
               typeSymbol.ImplementsInterface("IRequest", 1);
    }

    /// <summary>
    /// Checks if a type is a valid stream request type.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check</param>
    /// <returns>True if the type is a valid stream request type, false otherwise</returns>
    public static bool IsStreamRequestType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null) return false;

        return typeSymbol.ImplementsInterface("IStreamRequest", 1);
    }

    /// <summary>
    /// Checks if a type is a valid notification type.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check</param>
    /// <returns>True if the type is a valid notification type, false otherwise</returns>
    public static bool IsNotificationType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null) return false;

        return typeSymbol.ImplementsInterface("INotification");
    }

    /// <summary>
    /// Gets the response type from a request type.
    /// </summary>
    /// <param name="requestType">The request type symbol</param>
    /// <returns>The response type if found, null otherwise</returns>
    public static ITypeSymbol? GetResponseType(ITypeSymbol requestType)
    {
        if (requestType == null) return null;

        // Check for IRequest<TResponse>
        var genericRequest = requestType.GetInterface("IRequest", 1);
        if (genericRequest != null && genericRequest.TypeArguments.Length == 1)
        {
            return genericRequest.TypeArguments[0];
        }

        // Check for IStreamRequest<TResponse>
        var streamRequest = requestType.GetInterface("IStreamRequest", 1);
        if (streamRequest != null && streamRequest.TypeArguments.Length == 1)
        {
            return streamRequest.TypeArguments[0];
        }

        return null;
    }

    /// <summary>
    /// Checks if a return type matches the expected response type.
    /// </summary>
    /// <param name="returnType">The actual return type</param>
    /// <param name="expectedResponseType">The expected response type</param>
    /// <returns>True if the return type matches, false otherwise</returns>
    public static bool IsValidReturnType(ITypeSymbol returnType, ITypeSymbol expectedResponseType)
    {
        if (returnType == null || expectedResponseType == null) return false;

        // Direct match
        if (SymbolEqualityComparer.Default.Equals(returnType, expectedResponseType))
        {
            return true;
        }

        // Task<TResponse> or ValueTask<TResponse>
        var taskResultType = returnType.GetTaskResultType();
        if (taskResultType != null)
        {
            return SymbolEqualityComparer.Default.Equals(taskResultType, expectedResponseType);
        }

        return false;
    }

    /// <summary>
    /// Checks if a return type is valid for a void request.
    /// </summary>
    /// <param name="returnType">The return type to check</param>
    /// <returns>True if the return type is valid for void requests, false otherwise</returns>
    public static bool IsValidVoidReturnType(ITypeSymbol returnType)
    {
        if (returnType == null) return false;

        // Task or ValueTask (without generic parameter)
        return returnType.IsTaskType() && !returnType.IsGenericTaskType();
    }

    /// <summary>
    /// Checks if a return type is valid for a stream request.
    /// </summary>
    /// <param name="returnType">The return type to check</param>
    /// <param name="expectedResponseType">The expected response type</param>
    /// <returns>True if the return type is valid for stream requests, false otherwise</returns>
    public static bool IsValidStreamReturnType(ITypeSymbol returnType, ITypeSymbol expectedResponseType)
    {
        if (returnType == null || expectedResponseType == null) return false;

        // IAsyncEnumerable<TResponse>
        if (returnType is INamedTypeSymbol namedType &&
            namedType.Name == "IAsyncEnumerable" &&
            namedType.TypeArguments.Length == 1)
        {
            return SymbolEqualityComparer.Default.Equals(
                namedType.TypeArguments[0], 
                expectedResponseType);
        }

        return false;
    }

    /// <summary>
    /// Gets the handler interface type from a handler class.
    /// </summary>
    /// <param name="handlerType">The handler class type</param>
    /// <returns>The handler interface type if found, null otherwise</returns>
    public static INamedTypeSymbol? GetHandlerInterface(ITypeSymbol handlerType)
    {
        if (handlerType == null) return null;

        // Check for IRequestHandler<TRequest, TResponse>
        var requestHandler = handlerType.GetInterface("IRequestHandler");
        if (requestHandler != null) return requestHandler;

        // Check for INotificationHandler<TNotification>
        var notificationHandler = handlerType.GetInterface("INotificationHandler");
        if (notificationHandler != null) return notificationHandler;

        // Check for IStreamHandler<TRequest, TResponse>
        var streamHandler = handlerType.GetInterface("IStreamHandler");
        if (streamHandler != null) return streamHandler;

        return null;
    }

    /// <summary>
    /// Checks if a method signature is valid for a handler.
    /// </summary>
    /// <param name="methodSymbol">The method symbol to check</param>
    /// <returns>True if the signature is valid, false otherwise</returns>
    public static bool IsValidHandlerSignature(IMethodSymbol methodSymbol)
    {
        if (methodSymbol == null) return false;

        // Must have at least one parameter (the request)
        if (methodSymbol.Parameters.Length == 0) return false;

        // First parameter must be a request type
        var firstParam = methodSymbol.Parameters[0];
        if (!IsRequestType(firstParam.Type) && 
            !IsStreamRequestType(firstParam.Type) && 
            !IsNotificationType(firstParam.Type))
        {
            return false;
        }

        // Must return Task or ValueTask
        if (!methodSymbol.ReturnType.IsTaskType()) return false;

        return true;
    }

    /// <summary>
    /// Gets a friendly display name for a type.
    /// </summary>
    /// <param name="typeSymbol">The type symbol</param>
    /// <returns>A friendly display name</returns>
    public static string GetFriendlyName(ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null) return "unknown";

        // Handle generic types
        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var baseName = namedType.Name;
            var typeArgs = string.Join(", ", namedType.TypeArguments.Select(GetFriendlyName));
            return $"{baseName}<{typeArgs}>";
        }

        return typeSymbol.Name;
    }

    /// <summary>
    /// Checks if two types are equal using symbol equality.
    /// </summary>
    /// <param name="type1">The first type</param>
    /// <param name="type2">The second type</param>
    /// <returns>True if the types are equal, false otherwise</returns>
    public static bool AreEqual(ITypeSymbol? type1, ITypeSymbol? type2)
    {
        if (type1 == null && type2 == null) return true;
        if (type1 == null || type2 == null) return false;

        return SymbolEqualityComparer.Default.Equals(type1, type2);
    }
}
