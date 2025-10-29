using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Relay.Core.Configuration.Options.ContractValidation;
using Relay.Core.ContractValidation.Caching;
using Relay.Core.ContractValidation.CustomValidators;
using Relay.Core.ContractValidation.Models;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.Core.ContractValidation.Testing;

/// <summary>
/// Test fixture for setting up contract validation components in tests.
/// Provides factory methods for creating validators, schemas, and validation errors.
/// </summary>
public sealed class ContractValidationTestFixture
{
    /// <summary>
    /// Creates a contract validator with optional configuration.
    /// </summary>
    /// <param name="configure">Optional action to configure validation options.</param>
    /// <returns>A configured IContractValidator instance.</returns>
    public IContractValidator CreateValidator(Action<ContractValidationOptions>? configure = null)
    {
        var options = new ContractValidationOptions();
        configure?.Invoke(options);

        var cacheOptions = Options.Create(options.SchemaCache);
        var schemaCache = new LruSchemaCache(cacheOptions);

        return new DefaultContractValidator(schemaCache: schemaCache);
    }

    /// <summary>
    /// Creates a contract validator with custom components.
    /// </summary>
    /// <param name="schemaCache">Optional schema cache instance.</param>
    /// <param name="schemaResolver">Optional schema resolver instance.</param>
    /// <param name="validationEngine">Optional validation engine instance.</param>
    /// <param name="validationTimeout">Optional validation timeout.</param>
    /// <returns>A configured IContractValidator instance.</returns>
    public IContractValidator CreateValidatorWithComponents(
        ISchemaCache? schemaCache = null,
        ISchemaResolver? schemaResolver = null,
        ValidationEngine? validationEngine = null,
        TimeSpan? validationTimeout = null)
    {
        return new DefaultContractValidator(
            schemaCache: schemaCache,
            schemaResolver: schemaResolver,
            validationEngine: validationEngine,
            validationTimeout: validationTimeout);
    }

    /// <summary>
    /// Creates a test schema from JSON schema string.
    /// </summary>
    /// <param name="schemaJson">The JSON schema as a string.</param>
    /// <returns>A JsonSchemaContract instance.</returns>
    public JsonSchemaContract CreateTestSchema(string schemaJson)
    {
        return new JsonSchemaContract { Schema = schemaJson };
    }

    /// <summary>
    /// Creates a test schema for a simple object with string and integer properties.
    /// </summary>
    /// <returns>A JsonSchemaContract instance.</returns>
    public JsonSchemaContract CreateSimpleTestSchema()
    {
        return CreateTestSchema(@"{
            ""type"": ""object"",
            ""properties"": {
                ""Name"": { ""type"": ""string"" },
                ""Value"": { ""type"": ""integer"" }
            },
            ""required"": [""Name"", ""Value""]
        }");
    }

    /// <summary>
    /// Creates a test schema with validation constraints.
    /// </summary>
    /// <returns>A JsonSchemaContract instance.</returns>
    public JsonSchemaContract CreateConstrainedTestSchema()
    {
        return CreateTestSchema(@"{
            ""type"": ""object"",
            ""properties"": {
                ""Name"": { 
                    ""type"": ""string"",
                    ""minLength"": 1,
                    ""maxLength"": 100
                },
                ""Value"": { 
                    ""type"": ""integer"",
                    ""minimum"": 0,
                    ""maximum"": 1000
                },
                ""Email"": {
                    ""type"": ""string"",
                    ""format"": ""email""
                }
            },
            ""required"": [""Name"", ""Value""]
        }");
    }

    /// <summary>
    /// Creates a validation error for testing.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="path">The JSON path to the error.</param>
    /// <returns>A ValidationError instance.</returns>
    public ValidationError CreateValidationError(string code, string message, string path = "")
    {
        return ValidationError.Create(code, message, path);
    }

    /// <summary>
    /// Creates a validation error with additional details.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="path">The JSON path to the error.</param>
    /// <param name="expectedValue">The expected value.</param>
    /// <param name="actualValue">The actual value.</param>
    /// <param name="severity">The error severity.</param>
    /// <returns>A ValidationError instance.</returns>
    public ValidationError CreateValidationError(
        string code,
        string message,
        string path,
        object? expectedValue,
        object? actualValue,
        ValidationSeverity severity = ValidationSeverity.Error)
    {
        return new ValidationError
        {
            ErrorCode = code,
            Message = message,
            JsonPath = path,
            ExpectedValue = expectedValue,
            ActualValue = actualValue,
            Severity = severity
        };
    }

    /// <summary>
    /// Creates a validation context for request validation.
    /// </summary>
    /// <param name="requestType">The request type.</param>
    /// <param name="request">The request instance.</param>
    /// <param name="schema">Optional schema.</param>
    /// <returns>A ValidationContext instance.</returns>
    public ValidationContext CreateRequestContext(
        Type requestType,
        object? request,
        JsonSchemaContract? schema = null)
    {
        return ValidationContext.ForRequest(requestType, request, schema);
    }

    /// <summary>
    /// Creates a validation context for response validation.
    /// </summary>
    /// <param name="responseType">The response type.</param>
    /// <param name="response">The response instance.</param>
    /// <param name="schema">Optional schema.</param>
    /// <returns>A ValidationContext instance.</returns>
    public ValidationContext CreateResponseContext(
        Type responseType,
        object? response,
        JsonSchemaContract? schema = null)
    {
        return ValidationContext.ForResponse(responseType, response, schema);
    }

    /// <summary>
    /// Creates a schema cache with specified options.
    /// </summary>
    /// <param name="maxCacheSize">Maximum cache size.</param>
    /// <returns>An ISchemaCache instance.</returns>
    public ISchemaCache CreateSchemaCache(int maxCacheSize = 100)
    {
        var options = Options.Create(new SchemaCacheOptions { MaxCacheSize = maxCacheSize });
        return new LruSchemaCache(options);
    }

    /// <summary>
    /// Creates a validation engine with custom validators.
    /// </summary>
    /// <param name="validators">Custom validators to include.</param>
    /// <returns>A ValidationEngine instance.</returns>
    public ValidationEngine CreateValidationEngine(params ICustomValidator[] validators)
    {
        if (validators.Length == 0)
        {
            return new ValidationEngine();
        }

        var composer = new ValidatorComposer(validators);
        return new ValidationEngine(composer);
    }

    /// <summary>
    /// Creates a test custom validator with configurable behavior.
    /// </summary>
    /// <param name="appliesTo">Function to determine if validator applies to a type.</param>
    /// <param name="validateFunc">Function to perform validation.</param>
    /// <param name="priority">Validator priority.</param>
    /// <returns>An ICustomValidator instance.</returns>
    public ICustomValidator CreateTestValidator(
        Func<Type, bool> appliesTo,
        Func<object, ValidationContext, IEnumerable<ValidationError>> validateFunc,
        int priority = 100)
    {
        return new TestCustomValidator(appliesTo, validateFunc, priority);
    }

    /// <summary>
    /// Creates sample test data for validation testing.
    /// </summary>
    /// <returns>A dictionary of test objects.</returns>
    public Dictionary<string, object> CreateSampleTestData()
    {
        return new Dictionary<string, object>
        {
            ["ValidObject"] = new { Name = "Test", Value = 123 },
            ["InvalidObject"] = new { Name = "", Value = -1 },
            ["NullObject"] = null!,
            ["EmptyObject"] = new { },
            ["ComplexObject"] = new
            {
                Name = "Complex",
                Value = 456,
                Nested = new { Id = 1, Description = "Nested object" },
                Items = new[] { 1, 2, 3 }
            }
        };
    }

    // Internal test validator implementation
    private sealed class TestCustomValidator : ICustomValidator
    {
        private readonly Func<Type, bool> _appliesTo;
        private readonly Func<object, ValidationContext, IEnumerable<ValidationError>> _validateFunc;

        public TestCustomValidator(
            Func<Type, bool> appliesTo,
            Func<object, ValidationContext, IEnumerable<ValidationError>> validateFunc,
            int priority)
        {
            _appliesTo = appliesTo;
            _validateFunc = validateFunc;
            Priority = priority;
        }

        public int Priority { get; }

        public bool AppliesTo(Type type) => _appliesTo(type);

        public ValueTask<IEnumerable<ValidationError>> ValidateAsync(
            object obj,
            ValidationContext context,
            CancellationToken cancellationToken = default)
        {
            var errors = _validateFunc(obj, context);
            return ValueTask.FromResult(errors);
        }
    }
}
