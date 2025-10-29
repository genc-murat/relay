using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Json.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.ContractValidation.Caching;
using Relay.Core.ContractValidation.ErrorReporting;
using Relay.Core.ContractValidation.Models;
using Relay.Core.ContractValidation.Observability;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.Core.ContractValidation;

/// <summary>
/// Default implementation of IContractValidator using JsonSchema.Net.
/// </summary>
public class DefaultContractValidator : IContractValidator
{
    private readonly ConcurrentDictionary<string, JsonSchema> _schemaCache = new();
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ISchemaCache? _cache;
    private readonly ISchemaResolver? _schemaResolver;
    private readonly ValidationEngine? _validationEngine;
    private readonly TimeSpan _validationTimeout;
    private readonly ILogger<DefaultContractValidator> _logger;
    private readonly ContractValidationMetrics? _metrics;

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
        _validationTimeout = TimeSpan.FromSeconds(5);
        _logger = NullLogger<DefaultContractValidator>.Instance;
        _metrics = null;
    }

    /// <summary>
    /// Initializes a new instance of the DefaultContractValidator class with enhanced features.
    /// </summary>
    /// <param name="schemaCache">The schema cache for storing parsed schemas.</param>
    /// <param name="schemaResolver">The schema resolver for automatic schema discovery.</param>
    /// <param name="validationEngine">The validation engine for orchestrating validation.</param>
    /// <param name="validationTimeout">The timeout for validation operations.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <param name="metrics">Optional metrics collector.</param>
    public DefaultContractValidator(
        ISchemaCache? schemaCache = null,
        ISchemaResolver? schemaResolver = null,
        ValidationEngine? validationEngine = null,
        TimeSpan? validationTimeout = null,
        ILogger<DefaultContractValidator>? logger = null,
        ContractValidationMetrics? metrics = null)
    {
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
        _cache = schemaCache;
        _schemaResolver = schemaResolver;
        _validationEngine = validationEngine;
        _validationTimeout = validationTimeout ?? TimeSpan.FromSeconds(5);
        _logger = logger ?? NullLogger<DefaultContractValidator>.Instance;
        _metrics = metrics;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<string>> ValidateRequestAsync(object request, JsonSchemaContract schema, CancellationToken cancellationToken = default)
    {
        var context = ValidationContext.ForRequest(request?.GetType() ?? typeof(object), request, schema);
        var result = await ValidateRequestDetailedAsync(request, schema, context, cancellationToken);
        
        // Convert ValidationResult to simple string errors for backward compatibility
        return result.Errors.Select(e => e.ToString());
    }

    /// <summary>
    /// Validates a request contract against its schema with detailed error reporting.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="schema">The JSON schema to validate against.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A detailed validation result.</returns>
    public async ValueTask<ValidationResult> ValidateRequestDetailedAsync(
        object request,
        JsonSchemaContract schema,
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        using var activity = ContractValidationActivitySource.Instance.StartActivity(
            "ContractValidator.ValidateRequest",
            ActivityKind.Internal);

        activity?.SetTag("request_type", request?.GetType().Name ?? "null");
        activity?.SetTag("has_schema", schema != null && !string.IsNullOrWhiteSpace(schema.Schema));

        var stopwatch = Stopwatch.StartNew();
        _metrics?.IncrementActiveValidations();

        try
        {
            _logger.LogDebug(
                ValidationEventIds.ValidationStarted,
                "Starting request validation for {RequestType}",
                request?.GetType().Name ?? "null");

            // Apply timeout
            using var timeoutCts = new CancellationTokenSource(_validationTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var result = await ValidateInternalAsync(request, schema, context, linkedCts.Token);
            
            stopwatch.Stop();

            _logger.LogInformation(
                result.IsValid ? ValidationEventIds.ValidationCompleted : ValidationEventIds.ValidationFailed,
                "Request validation completed for {RequestType}. IsValid: {IsValid}, ErrorCount: {ErrorCount}, Duration: {Duration}ms",
                request?.GetType().Name ?? "null",
                result.IsValid,
                result.Errors.Count,
                stopwatch.ElapsedMilliseconds);

            // Record metrics
            _metrics?.RecordValidation(
                request?.GetType().Name ?? "unknown",
                isRequest: true,
                result.IsValid,
                stopwatch.Elapsed.TotalMilliseconds,
                result.Errors.Count);

            // Record individual errors
            foreach (var error in result.Errors)
            {
                _metrics?.RecordValidationError(error.ErrorCode);
            }

            // Check for performance issues
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                _logger.LogWarning(
                    ValidationEventIds.PerformanceWarning,
                    "Request validation took {Duration}ms for {RequestType}, which exceeds the 100ms threshold",
                    stopwatch.ElapsedMilliseconds,
                    request?.GetType().Name ?? "null");
            }

            activity?.SetTag("is_valid", result.IsValid);
            activity?.SetTag("error_count", result.Errors.Count);
            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);

            return new ValidationResult
            {
                IsValid = result.IsValid,
                Errors = result.Errors,
                ValidationDuration = stopwatch.Elapsed,
                ValidatorName = nameof(DefaultContractValidator)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // User-requested cancellation
            _logger.LogWarning(
                "Request validation cancelled by user for {RequestType}",
                request?.GetType().Name ?? "null");
            throw;
        }
        catch (OperationCanceledException)
        {
            // Timeout occurred
            stopwatch.Stop();

            _logger.LogError(
                ValidationEventIds.ValidationTimeout,
                "Request validation timed out after {Timeout}s for {RequestType}",
                _validationTimeout.TotalSeconds,
                request?.GetType().Name ?? "null");

            activity?.SetStatus(ActivityStatusCode.Error, "Validation timeout");

            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    ValidationError.Create(
                        ValidationErrorCodes.ValidationTimeout,
                        $"Validation timed out after {_validationTimeout.TotalSeconds} seconds")
                },
                ValidationDuration = stopwatch.Elapsed,
                ValidatorName = nameof(DefaultContractValidator)
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ValidationEventIds.ValidationFailed,
                ex,
                "Request validation failed for {RequestType}",
                request?.GetType().Name ?? "null");

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    ValidationError.Create(
                        ValidationErrorCodes.GeneralValidationError,
                        $"Request validation failed: {ex.Message}")
                },
                ValidationDuration = stopwatch.Elapsed,
                ValidatorName = nameof(DefaultContractValidator)
            };
        }
        finally
        {
            _metrics?.DecrementActiveValidations();
        }
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<string>> ValidateResponseAsync(object response, JsonSchemaContract schema, CancellationToken cancellationToken = default)
    {
        var context = ValidationContext.ForResponse(response?.GetType() ?? typeof(object), response, schema);
        var result = await ValidateResponseDetailedAsync(response, schema, context, cancellationToken);
        
        // Convert ValidationResult to simple string errors for backward compatibility
        return result.Errors.Select(e => e.ToString());
    }

    /// <summary>
    /// Validates a response contract against its schema with detailed error reporting.
    /// </summary>
    /// <param name="response">The response to validate.</param>
    /// <param name="schema">The JSON schema to validate against.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A detailed validation result.</returns>
    public async ValueTask<ValidationResult> ValidateResponseDetailedAsync(
        object response,
        JsonSchemaContract schema,
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        using var activity = ContractValidationActivitySource.Instance.StartActivity(
            "ContractValidator.ValidateResponse",
            ActivityKind.Internal);

        activity?.SetTag("response_type", response?.GetType().Name ?? "null");
        activity?.SetTag("has_schema", schema != null && !string.IsNullOrWhiteSpace(schema.Schema));

        var stopwatch = Stopwatch.StartNew();
        _metrics?.IncrementActiveValidations();

        try
        {
            _logger.LogDebug(
                ValidationEventIds.ValidationStarted,
                "Starting response validation for {ResponseType}",
                response?.GetType().Name ?? "null");

            // Apply timeout
            using var timeoutCts = new CancellationTokenSource(_validationTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var result = await ValidateInternalAsync(response, schema, context, linkedCts.Token);
            
            stopwatch.Stop();

            _logger.LogInformation(
                result.IsValid ? ValidationEventIds.ValidationCompleted : ValidationEventIds.ValidationFailed,
                "Response validation completed for {ResponseType}. IsValid: {IsValid}, ErrorCount: {ErrorCount}, Duration: {Duration}ms",
                response?.GetType().Name ?? "null",
                result.IsValid,
                result.Errors.Count,
                stopwatch.ElapsedMilliseconds);

            // Record metrics
            _metrics?.RecordValidation(
                response?.GetType().Name ?? "unknown",
                isRequest: false,
                result.IsValid,
                stopwatch.Elapsed.TotalMilliseconds,
                result.Errors.Count);

            // Record individual errors
            foreach (var error in result.Errors)
            {
                _metrics?.RecordValidationError(error.ErrorCode);
            }

            // Check for performance issues
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                _logger.LogWarning(
                    ValidationEventIds.PerformanceWarning,
                    "Response validation took {Duration}ms for {ResponseType}, which exceeds the 100ms threshold",
                    stopwatch.ElapsedMilliseconds,
                    response?.GetType().Name ?? "null");
            }

            activity?.SetTag("is_valid", result.IsValid);
            activity?.SetTag("error_count", result.Errors.Count);
            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);

            return new ValidationResult
            {
                IsValid = result.IsValid,
                Errors = result.Errors,
                ValidationDuration = stopwatch.Elapsed,
                ValidatorName = nameof(DefaultContractValidator)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // User-requested cancellation
            _logger.LogWarning(
                "Response validation cancelled by user for {ResponseType}",
                response?.GetType().Name ?? "null");
            throw;
        }
        catch (OperationCanceledException)
        {
            // Timeout occurred
            stopwatch.Stop();

            _logger.LogError(
                ValidationEventIds.ValidationTimeout,
                "Response validation timed out after {Timeout}s for {ResponseType}",
                _validationTimeout.TotalSeconds,
                response?.GetType().Name ?? "null");

            activity?.SetStatus(ActivityStatusCode.Error, "Validation timeout");

            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    ValidationError.Create(
                        ValidationErrorCodes.ValidationTimeout,
                        $"Validation timed out after {_validationTimeout.TotalSeconds} seconds")
                },
                ValidationDuration = stopwatch.Elapsed,
                ValidatorName = nameof(DefaultContractValidator)
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ValidationEventIds.ValidationFailed,
                ex,
                "Response validation failed for {ResponseType}",
                response?.GetType().Name ?? "null");

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    ValidationError.Create(
                        ValidationErrorCodes.GeneralValidationError,
                        $"Response validation failed: {ex.Message}")
                },
                ValidationDuration = stopwatch.Elapsed,
                ValidatorName = nameof(DefaultContractValidator)
            };
        }
        finally
        {
            _metrics?.DecrementActiveValidations();
        }
    }

    /// <summary>
    /// Internal validation method that performs the actual validation logic.
    /// </summary>
    private async ValueTask<ValidationResult> ValidateInternalAsync(
        object obj,
        JsonSchemaContract schema,
        ValidationContext context,
        CancellationToken cancellationToken)
    {
        var aggregator = new ErrorAggregator();

        // Validate input
        if (obj == null && context.IsRequest)
        {
            aggregator.AddError(ValidationError.Create(
                ValidationErrorCodes.GeneralValidationError,
                "Request cannot be null"));
            return aggregator.ToValidationResult(nameof(DefaultContractValidator));
        }

        if (schema == null || string.IsNullOrWhiteSpace(schema.Schema))
        {
            // No schema provided, skip schema validation but run custom validators if available
            if (_validationEngine != null && _validationEngine.HasCustomValidators && obj != null)
            {
                return await _validationEngine.ValidateAsync(obj, context, cancellationToken);
            }
            return ValidationResult.Success(nameof(DefaultContractValidator));
        }

        // Get or parse the JSON schema using cache if available
        var jsonSchema = GetOrParseSchemaWithCache(schema.Schema);
        if (jsonSchema == null)
        {
            aggregator.AddError(ValidationError.Create(
                ValidationErrorCodes.SchemaParsingFailed,
                "Invalid JSON schema format"));
            return aggregator.ToValidationResult(nameof(DefaultContractValidator));
        }

        // Handle null responses
        if (obj == null)
        {
            var nullNode = JsonValue.Create((string?)null);
            var nullValidationResults = jsonSchema.Evaluate(nullNode, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List
            });

            if (!nullValidationResults.IsValid)
            {
                aggregator.AddError(ValidationError.Create(
                    ValidationErrorCodes.GeneralValidationError,
                    "Value cannot be null according to schema",
                    "root"));
            }
            return aggregator.ToValidationResult(nameof(DefaultContractValidator));
        }

        // Serialize the object to JSON
        string json;
        JsonNode? jsonNode;
        try
        {
            json = JsonSerializer.Serialize(obj, _serializerOptions);
            jsonNode = JsonNode.Parse(json);
        }
        catch (JsonException jsonEx)
        {
            aggregator.AddError(ValidationError.Create(
                ValidationErrorCodes.GeneralValidationError,
                $"JSON serialization error: {jsonEx.Message}"));
            return aggregator.ToValidationResult(nameof(DefaultContractValidator));
        }

        if (jsonNode == null)
        {
            aggregator.AddError(ValidationError.Create(
                ValidationErrorCodes.GeneralValidationError,
                "Failed to parse object as JSON"));
            return aggregator.ToValidationResult(nameof(DefaultContractValidator));
        }

        // Validate against schema
        var validationResults = jsonSchema.Evaluate(jsonNode, new EvaluationOptions
        {
            OutputFormat = OutputFormat.List,
            RequireFormatValidation = true
        });

        if (!validationResults.IsValid)
        {
            // Extract validation errors using the new error model
            ExtractValidationErrorsToAggregator(validationResults, aggregator);
        }

        // Run custom validators if available
        if (_validationEngine != null && _validationEngine.HasCustomValidators)
        {
            var customResult = await _validationEngine.ValidateAsync(obj, context, cancellationToken);
            if (!customResult.IsValid)
            {
                aggregator.AddErrors(customResult.Errors);
            }
        }

        return aggregator.ToValidationResult(nameof(DefaultContractValidator));
    }

    /// <summary>
    /// Gets a cached schema or parses a new one using ISchemaCache if available.
    /// </summary>
    /// <param name="schemaJson">The JSON schema string.</param>
    /// <returns>The parsed JSON schema, or null if parsing failed.</returns>
    private JsonSchema? GetOrParseSchemaWithCache(string schemaJson)
    {
        // Use ISchemaCache if available
        if (_cache != null)
        {
            var cacheKey = GenerateCacheKey(schemaJson);
            var cachedSchema = _cache.Get(cacheKey);
            if (cachedSchema != null)
            {
                return cachedSchema;
            }

            try
            {
                var schema = JsonSchema.FromText(schemaJson);
                _cache.Set(cacheKey, schema);
                return schema;
            }
            catch
            {
                return null;
            }
        }

        // Fall back to legacy cache
        return _schemaCache.GetOrAdd(schemaJson, json =>
        {
            try
            {
                return JsonSchema.FromText(json);
            }
            catch
            {
                return null!;
            }
        });
    }

    /// <summary>
    /// Generates a cache key for a schema.
    /// </summary>
    private string GenerateCacheKey(string schemaJson)
    {
        // Use a hash of the schema JSON as the cache key
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(schemaJson));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Gets a cached schema or parses a new one (legacy method for backward compatibility).
    /// </summary>
    /// <param name="schemaJson">The JSON schema string.</param>
    /// <returns>The parsed JSON schema, or null if parsing failed.</returns>
    private JsonSchema? GetOrParseSchema(string schemaJson)
    {
        return GetOrParseSchemaWithCache(schemaJson);
    }

    /// <summary>
    /// Extracts validation errors from evaluation results into an ErrorAggregator.
    /// </summary>
    /// <param name="results">The evaluation results.</param>
    /// <param name="aggregator">The error aggregator to add errors to.</param>
    private void ExtractValidationErrorsToAggregator(EvaluationResults results, ErrorAggregator aggregator)
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
                        var errorCode = DetermineErrorCode(key);
                        
                        var error = new ValidationError
                        {
                            ErrorCode = errorCode,
                            Message = $"{key}: {value}",
                            JsonPath = path,
                            SchemaConstraint = key,
                            Severity = ValidationSeverity.Error
                        };

                        if (!aggregator.AddError(error))
                        {
                            // Max errors reached
                            return;
                        }
                    }
                }

                // Recursively extract errors from nested details
                if (detail.Details != null && detail.Details.Any())
                {
                    ExtractValidationErrorsFromDetailsToAggregator(detail.Details, aggregator);
                    if (aggregator.HasReachedMaxErrors)
                    {
                        return;
                    }
                }
            }
        }

        // If no detailed errors were found but validation failed, add a generic error
        if (!results.IsValid && !aggregator.HasErrors)
        {
            aggregator.AddError(ValidationError.Create(
                ValidationErrorCodes.GeneralValidationError,
                "The data does not match the schema",
                "root"));
        }
    }

    /// <summary>
    /// Recursively extracts validation errors from evaluation details into an ErrorAggregator.
    /// </summary>
    /// <param name="details">The evaluation details to extract errors from.</param>
    /// <param name="aggregator">The error aggregator to add errors to.</param>
    private void ExtractValidationErrorsFromDetailsToAggregator(IEnumerable<EvaluationResults> details, ErrorAggregator aggregator)
    {
        foreach (var detail in details)
        {
            if (!detail.IsValid && detail.Errors != null)
            {
                foreach (var (key, value) in detail.Errors)
                {
                    var path = detail.InstanceLocation?.ToString() ?? "root";
                    var errorCode = DetermineErrorCode(key);
                    
                    var error = new ValidationError
                    {
                        ErrorCode = errorCode,
                        Message = $"{key}: {value}",
                        JsonPath = path,
                        SchemaConstraint = key,
                        Severity = ValidationSeverity.Error
                    };

                    if (!aggregator.AddError(error))
                    {
                        // Max errors reached
                        return;
                    }
                }
            }

            // Recursively process nested details
            if (detail.Details != null && detail.Details.Any())
            {
                ExtractValidationErrorsFromDetailsToAggregator(detail.Details, aggregator);
                if (aggregator.HasReachedMaxErrors)
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Determines the appropriate error code based on the schema constraint.
    /// </summary>
    /// <param name="constraint">The schema constraint that was violated.</param>
    /// <returns>The appropriate error code.</returns>
    private string DetermineErrorCode(string constraint)
    {
        return constraint?.ToLowerInvariant() switch
        {
            "required" => ValidationErrorCodes.RequiredPropertyMissing,
            "type" => ValidationErrorCodes.TypeMismatch,
            "minimum" or "maximum" or "minlength" or "maxlength" or "pattern" or "format" 
                => ValidationErrorCodes.ConstraintViolation,
            _ => ValidationErrorCodes.GeneralValidationError
        };
    }

    /// <summary>
    /// Extracts validation errors from evaluation results (legacy method for backward compatibility).
    /// </summary>
    /// <param name="results">The evaluation results.</param>
    /// <param name="errors">The list to add errors to.</param>
    private void ExtractValidationErrors(EvaluationResults results, List<string> errors)
    {
        var aggregator = new ErrorAggregator();
        ExtractValidationErrorsToAggregator(results, aggregator);
        errors.AddRange(aggregator.GetErrors().Select(e => e.ToString()));
    }

    /// <summary>
    /// Recursively extracts validation errors from evaluation details (legacy method for backward compatibility).
    /// </summary>
    /// <param name="details">The evaluation details to extract errors from.</param>
    /// <param name="errors">The list to add errors to.</param>
    private void ExtractValidationErrorsFromDetails(IEnumerable<EvaluationResults> details, List<string> errors)
    {
        var aggregator = new ErrorAggregator();
        ExtractValidationErrorsFromDetailsToAggregator(details, aggregator);
        errors.AddRange(aggregator.GetErrors().Select(e => e.ToString()));
    }
}