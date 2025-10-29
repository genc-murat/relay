using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.Core.ContractValidation.Testing;

/// <summary>
/// Test implementation of ISchemaProvider for isolated testing.
/// Allows registering schemas for specific types without requiring file system or embedded resources.
/// </summary>
public sealed class TestSchemaProvider : ISchemaProvider
{
    private readonly Dictionary<Type, JsonSchemaContract> _schemas = new();
    private readonly Dictionary<string, JsonSchemaContract> _schemasByName = new();

    /// <summary>
    /// Gets or sets the priority of this provider.
    /// </summary>
    public int Priority { get; set; } = 1000;

    /// <summary>
    /// Registers a schema for a specific type.
    /// </summary>
    /// <param name="type">The type to register the schema for.</param>
    /// <param name="schema">The schema to register.</param>
    public void RegisterSchema(Type type, JsonSchemaContract schema)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (schema == null)
            throw new ArgumentNullException(nameof(schema));

        _schemas[type] = schema;
    }

    /// <summary>
    /// Registers a schema for a specific type using JSON schema string.
    /// </summary>
    /// <param name="type">The type to register the schema for.</param>
    /// <param name="schemaJson">The JSON schema string.</param>
    public void RegisterSchema(Type type, string schemaJson)
    {
        RegisterSchema(type, new JsonSchemaContract { Schema = schemaJson });
    }

    /// <summary>
    /// Registers a schema by type name.
    /// </summary>
    /// <param name="typeName">The type name to register the schema for.</param>
    /// <param name="schema">The schema to register.</param>
    public void RegisterSchemaByName(string typeName, JsonSchemaContract schema)
    {
        if (string.IsNullOrEmpty(typeName))
            throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));
        if (schema == null)
            throw new ArgumentNullException(nameof(schema));

        _schemasByName[typeName] = schema;
    }

    /// <summary>
    /// Registers a schema by type name using JSON schema string.
    /// </summary>
    /// <param name="typeName">The type name to register the schema for.</param>
    /// <param name="schemaJson">The JSON schema string.</param>
    public void RegisterSchemaByName(string typeName, string schemaJson)
    {
        RegisterSchemaByName(typeName, new JsonSchemaContract { Schema = schemaJson });
    }

    /// <summary>
    /// Clears all registered schemas.
    /// </summary>
    public void Clear()
    {
        _schemas.Clear();
        _schemasByName.Clear();
    }

    /// <summary>
    /// Removes the schema for a specific type.
    /// </summary>
    /// <param name="type">The type to remove the schema for.</param>
    /// <returns>True if the schema was removed, false otherwise.</returns>
    public bool RemoveSchema(Type type)
    {
        return _schemas.Remove(type);
    }

    /// <summary>
    /// Checks if a schema is registered for the specified type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if a schema is registered, false otherwise.</returns>
    public bool HasSchema(Type type)
    {
        return _schemas.ContainsKey(type);
    }

    /// <summary>
    /// Gets the number of registered schemas.
    /// </summary>
    public int SchemaCount => _schemas.Count + _schemasByName.Count;

    /// <inheritdoc />
    public ValueTask<JsonSchemaContract?> TryGetSchemaAsync(
        Type type,
        SchemaContext context,
        CancellationToken cancellationToken = default)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        // Try exact type match first
        if (_schemas.TryGetValue(type, out var schema))
        {
            return ValueTask.FromResult<JsonSchemaContract?>(schema);
        }

        // Try by type name
        var typeName = type.Name;
        if (_schemasByName.TryGetValue(typeName, out schema))
        {
            return ValueTask.FromResult<JsonSchemaContract?>(schema);
        }

        // Try by full type name
        var fullTypeName = type.FullName ?? typeName;
        if (_schemasByName.TryGetValue(fullTypeName, out schema))
        {
            return ValueTask.FromResult<JsonSchemaContract?>(schema);
        }

        return ValueTask.FromResult<JsonSchemaContract?>(null);
    }

    /// <summary>
    /// Creates a test schema provider with common test schemas pre-registered.
    /// </summary>
    /// <returns>A TestSchemaProvider with sample schemas.</returns>
    public static TestSchemaProvider CreateWithSampleSchemas()
    {
        var provider = new TestSchemaProvider();

        // Simple object schema
        provider.RegisterSchemaByName("SimpleObject", @"{
            ""type"": ""object"",
            ""properties"": {
                ""Name"": { ""type"": ""string"" },
                ""Value"": { ""type"": ""integer"" }
            },
            ""required"": [""Name"", ""Value""]
        }");

        // Person schema
        provider.RegisterSchemaByName("Person", @"{
            ""type"": ""object"",
            ""properties"": {
                ""FirstName"": { ""type"": ""string"", ""minLength"": 1 },
                ""LastName"": { ""type"": ""string"", ""minLength"": 1 },
                ""Age"": { ""type"": ""integer"", ""minimum"": 0, ""maximum"": 150 },
                ""Email"": { ""type"": ""string"", ""format"": ""email"" }
            },
            ""required"": [""FirstName"", ""LastName""]
        }");

        // Product schema
        provider.RegisterSchemaByName("Product", @"{
            ""type"": ""object"",
            ""properties"": {
                ""Id"": { ""type"": ""integer"" },
                ""Name"": { ""type"": ""string"", ""minLength"": 1, ""maxLength"": 100 },
                ""Price"": { ""type"": ""number"", ""minimum"": 0 },
                ""InStock"": { ""type"": ""boolean"" }
            },
            ""required"": [""Id"", ""Name"", ""Price""]
        }");

        // Order schema with nested objects
        provider.RegisterSchemaByName("Order", @"{
            ""type"": ""object"",
            ""properties"": {
                ""OrderId"": { ""type"": ""string"" },
                ""Customer"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""Name"": { ""type"": ""string"" },
                        ""Email"": { ""type"": ""string"", ""format"": ""email"" }
                    },
                    ""required"": [""Name"", ""Email""]
                },
                ""Items"": {
                    ""type"": ""array"",
                    ""items"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""ProductId"": { ""type"": ""integer"" },
                            ""Quantity"": { ""type"": ""integer"", ""minimum"": 1 }
                        },
                        ""required"": [""ProductId"", ""Quantity""]
                    },
                    ""minItems"": 1
                },
                ""Total"": { ""type"": ""number"", ""minimum"": 0 }
            },
            ""required"": [""OrderId"", ""Customer"", ""Items"", ""Total""]
        }");

        return provider;
    }
}
