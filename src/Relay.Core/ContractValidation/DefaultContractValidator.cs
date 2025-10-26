using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Json.Schema;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.Core.ContractValidation;

/// <summary>
/// Default implementation of IContractValidator using JsonSchema.Net.
/// </summary>
public class DefaultContractValidator : IContractValidator
{
    private readonly ConcurrentDictionary<string, JsonSchema> _schemaCache = new();
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the DefaultContractValidator class.
    /// </summary>
    public DefaultContractValidator()
    {
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<string>> ValidateRequestAsync(object request, JsonSchemaContract schema, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make method async for interface compliance

        var errors = new List<string>();

        try
        {
            // Validate input
            if (request == null)
            {
                errors.Add("Request cannot be null");
                return errors;
            }

            if (schema == null || string.IsNullOrWhiteSpace(schema.Schema))
            {
                // No schema provided, skip validation
                return errors;
            }

            // Get or parse the JSON schema
            var jsonSchema = GetOrParseSchema(schema.Schema);
            if (jsonSchema == null)
            {
                errors.Add("Invalid JSON schema format");
                return errors;
            }

            // Serialize the request to JSON
            var json = JsonSerializer.Serialize(request, _serializerOptions);
            var jsonNode = JsonNode.Parse(json);

            if (jsonNode == null)
            {
                errors.Add("Failed to parse request as JSON");
                return errors;
            }

            // Validate against schema
            var validationResults = jsonSchema.Evaluate(jsonNode, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List,
                RequireFormatValidation = true
            });

            if (!validationResults.IsValid)
            {
                // Extract error messages from validation results
                ExtractValidationErrors(validationResults, errors);
            }
        }
        catch (JsonException jsonEx)
        {
            errors.Add($"JSON serialization error: {jsonEx.Message}");
        }
        catch (Exception ex)
        {
            errors.Add($"Request validation failed: {ex.Message}");
        }

        return errors;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<string>> ValidateResponseAsync(object response, JsonSchemaContract schema, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make method async for interface compliance

        var errors = new List<string>();

        try
        {
            // Validate input
            if (schema == null || string.IsNullOrWhiteSpace(schema.Schema))
            {
                // No schema provided, skip validation
                return errors;
            }

            // Allow null responses if schema permits it
            if (response == null)
            {
                // Check if schema allows null
                var jsonSchema = GetOrParseSchema(schema.Schema);
                if (jsonSchema != null)
                {
                    var nullNode = JsonValue.Create((string?)null);
                    var validationResults = jsonSchema.Evaluate(nullNode, new EvaluationOptions
                    {
                        OutputFormat = OutputFormat.List
                    });

                    if (!validationResults.IsValid)
                    {
                        errors.Add("Response cannot be null according to schema");
                    }
                }
                return errors;
            }

            // Get or parse the JSON schema
            var responseSchema = GetOrParseSchema(schema.Schema);
            if (responseSchema == null)
            {
                errors.Add("Invalid JSON schema format");
                return errors;
            }

            // Serialize the response to JSON
            var json = JsonSerializer.Serialize(response, _serializerOptions);
            var jsonNode = JsonNode.Parse(json);

            if (jsonNode == null)
            {
                errors.Add("Failed to parse response as JSON");
                return errors;
            }

            // Validate against schema
            var results = responseSchema.Evaluate(jsonNode, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List,
                RequireFormatValidation = true
            });

            if (!results.IsValid)
            {
                // Extract error messages from validation results
                ExtractValidationErrors(results, errors);
            }
        }
        catch (JsonException jsonEx)
        {
            errors.Add($"JSON serialization error: {jsonEx.Message}");
        }
        catch (Exception ex)
        {
            errors.Add($"Response validation failed: {ex.Message}");
        }

        return errors;
    }

    /// <summary>
    /// Gets a cached schema or parses a new one.
    /// </summary>
    /// <param name="schemaJson">The JSON schema string.</param>
    /// <returns>The parsed JSON schema, or null if parsing failed.</returns>
    private JsonSchema? GetOrParseSchema(string schemaJson)
    {
        return _schemaCache.GetOrAdd(schemaJson, json =>
        {
            try
            {
                return JsonSchema.FromText(json);
            }
            catch
            {
                // If parsing fails, return null
                // The calling method will handle the error
                return null!;
            }
        });
    }

    /// <summary>
    /// Extracts validation errors from evaluation results.
    /// </summary>
    /// <param name="results">The evaluation results.</param>
    /// <param name="errors">The list to add errors to.</param>
    private void ExtractValidationErrors(EvaluationResults results, List<string> errors)
    {
        if (!results.IsValid && results.Details != null)
        {
            foreach (var detail in results.Details)
            {
                if (!detail.IsValid && detail.Errors != null)
                {
                    foreach (var (key, value) in detail.Errors)
                    {
                        var path = detail.InstanceLocation?.ToString() ?? "root";
                        errors.Add($"Validation error at '{path}': {key} - {value}");
                    }
                }

                // Recursively extract errors from nested details
                if (detail.Details != null && detail.Details.Any())
                {
                    ExtractValidationErrorsFromDetails(detail.Details, errors);
                }
            }
        }

        // If no detailed errors were found but validation failed, add a generic error
        if (!results.IsValid && errors.Count == 0)
        {
            errors.Add("Validation failed: The data does not match the schema");
        }
    }

    /// <summary>
    /// Recursively extracts validation errors from evaluation details.
    /// </summary>
    /// <param name="details">The evaluation details to extract errors from.</param>
    /// <param name="errors">The list to add errors to.</param>
    private void ExtractValidationErrorsFromDetails(IEnumerable<EvaluationResults> details, List<string> errors)
    {
        foreach (var detail in details)
        {
            if (!detail.IsValid && detail.Errors != null)
            {
                foreach (var (key, value) in detail.Errors)
                {
                    var path = detail.InstanceLocation?.ToString() ?? "root";
                    errors.Add($"Validation error at '{path}': {key} - {value}");
                }
            }

            // Recursively process nested details
            if (detail.Details != null && detail.Details.Any())
            {
                ExtractValidationErrorsFromDetails(detail.Details, errors);
            }
        }
    }
}