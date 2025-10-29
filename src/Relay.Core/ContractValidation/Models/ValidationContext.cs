using System;
using System.Collections.Generic;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.Core.ContractValidation.Models;

/// <summary>
/// Represents the context for a validation operation.
/// </summary>
public sealed class ValidationContext
{
    /// <summary>
    /// Gets or sets the type of the object being validated.
    /// </summary>
    public Type ObjectType { get; init; } = typeof(object);

    /// <summary>
    /// Gets or sets the object instance being validated.
    /// </summary>
    public object? ObjectInstance { get; init; }

    /// <summary>
    /// Gets or sets the JSON schema contract for validation.
    /// </summary>
    public JsonSchemaContract? Schema { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a request validation.
    /// </summary>
    public bool IsRequest { get; init; }

    /// <summary>
    /// Gets or sets additional metadata for the validation context.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets or sets the name of the handler being validated.
    /// </summary>
    public string? HandlerName { get; init; }

    /// <summary>
    /// Creates a new validation context for a request.
    /// </summary>
    /// <param name="objectType">The type of the request.</param>
    /// <param name="objectInstance">The request instance.</param>
    /// <param name="schema">The JSON schema contract.</param>
    /// <returns>A new validation context instance.</returns>
    public static ValidationContext ForRequest(Type objectType, object? objectInstance, JsonSchemaContract? schema)
    {
        return new ValidationContext
        {
            ObjectType = objectType,
            ObjectInstance = objectInstance,
            Schema = schema,
            IsRequest = true
        };
    }

    /// <summary>
    /// Creates a new validation context for a response.
    /// </summary>
    /// <param name="objectType">The type of the response.</param>
    /// <param name="objectInstance">The response instance.</param>
    /// <param name="schema">The JSON schema contract.</param>
    /// <returns>A new validation context instance.</returns>
    public static ValidationContext ForResponse(Type objectType, object? objectInstance, JsonSchemaContract? schema)
    {
        return new ValidationContext
        {
            ObjectType = objectType,
            ObjectInstance = objectInstance,
            Schema = schema,
            IsRequest = false
        };
    }
}
