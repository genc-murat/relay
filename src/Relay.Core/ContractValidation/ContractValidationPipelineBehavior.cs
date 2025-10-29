using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.Configuration.Options.ContractValidation;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Configuration.Core;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.ContractValidation.Models;
using Relay.Core.ContractValidation.Observability;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Relay.Core.ContractValidation.Strategies;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.Core.ContractValidation;

/// <summary>
/// A pipeline behavior that implements contract validation for requests and responses.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class ContractValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IContractValidator _contractValidator;
    private readonly ILogger<ContractValidationPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IOptions<RelayOptions> _options;
    private readonly ISchemaResolver? _schemaResolver;
    private readonly ValidationStrategyFactory? _strategyFactory;
    private readonly ContractValidationMetrics? _metrics;
    private readonly string _handlerKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContractValidationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="contractValidator">The contract validator.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The relay options.</param>
    /// <param name="schemaResolver">Optional schema resolver for automatic schema discovery.</param>
    /// <param name="strategyFactory">Optional validation strategy factory.</param>
    /// <param name="metrics">Optional metrics collector.</param>
    public ContractValidationPipelineBehavior(
        IContractValidator contractValidator,
        ILogger<ContractValidationPipelineBehavior<TRequest, TResponse>> logger,
        IOptions<RelayOptions> options,
        ISchemaResolver? schemaResolver = null,
        ValidationStrategyFactory? strategyFactory = null,
        ContractValidationMetrics? metrics = null)
    {
        _contractValidator = contractValidator ?? throw new ArgumentNullException(nameof(contractValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _schemaResolver = schemaResolver;
        _strategyFactory = strategyFactory;
        _metrics = metrics;
        _handlerKey = typeof(TRequest).FullName ?? typeof(TRequest).Name;
    }

    /// <inheritdoc />
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        using var activity = ContractValidationActivitySource.Instance.StartActivity(
            "ContractValidationPipeline.Handle",
            ActivityKind.Internal);

        activity?.SetTag("request_type", typeof(TRequest).Name);
        activity?.SetTag("response_type", typeof(TResponse).Name);
        activity?.SetTag("handler_key", _handlerKey);

        var overallStopwatch = Stopwatch.StartNew();

        // Get contract validation configuration
        var contractValidationOptions = GetContractValidationOptions();
        var validateContractAttribute = typeof(TRequest).GetCustomAttribute<ValidateContractAttribute>();

        // Check if contract validation is enabled for this request
        if (!IsContractValidationEnabled(contractValidationOptions, validateContractAttribute))
        {
            _logger.LogDebug(
                "Contract validation is disabled for {RequestType}",
                typeof(TRequest).Name);

            activity?.SetTag("validation_enabled", false);
            return await next();
        }

        activity?.SetTag("validation_enabled", true);

        // Get validation parameters
        var (validateRequest, validateResponse, throwOnFailure) = GetValidationParameters(contractValidationOptions, validateContractAttribute);

        activity?.SetTag("validate_request", validateRequest);
        activity?.SetTag("validate_response", validateResponse);

        // Get validation strategy
        var validationStrategy = GetValidationStrategy(contractValidationOptions, validateContractAttribute);

        activity?.SetTag("validation_strategy", validationStrategy.Name);

        // Validate request contract if enabled
        if (validateRequest)
        {
            var requestValidationStopwatch = Stopwatch.StartNew();
            var requestResult = await ValidateRequestContractDetailed(request, validationStrategy, cancellationToken);
            requestValidationStopwatch.Stop();

            // Log performance metrics
            if (contractValidationOptions.EnablePerformanceMetrics)
            {
                _logger.LogDebug(
                    "Request validation completed for {RequestType} in {Duration}ms. IsValid: {IsValid}, ErrorCount: {ErrorCount}",
                    typeof(TRequest).Name,
                    requestValidationStopwatch.ElapsedMilliseconds,
                    requestResult.IsValid,
                    requestResult.Errors.Count);
            }

            // Handle validation result according to strategy
            if (!requestResult.IsValid)
            {
                var context = new ValidationContext
                {
                    ObjectType = typeof(TRequest),
                    ObjectInstance = request,
                    Schema = null,
                    IsRequest = true,
                    HandlerName = _handlerKey
                };

                var handledResult = await validationStrategy.HandleResultAsync(requestResult, context, cancellationToken);

                // If strategy didn't throw and result is still invalid, check throwOnFailure
                if (!handledResult.IsValid && throwOnFailure)
                {
                    throw new ContractValidationException(typeof(TRequest), 
                        handledResult.Errors.Select(e => e.ToString()).ToArray());
                }
            }
        }

        // Execute the handler
        var response = await next();

        // Validate response contract if enabled
        if (validateResponse)
        {
            var responseValidationStopwatch = Stopwatch.StartNew();
            var responseResult = await ValidateResponseContractDetailed(response, validationStrategy, cancellationToken);
            responseValidationStopwatch.Stop();

            // Log performance metrics
            if (contractValidationOptions.EnablePerformanceMetrics)
            {
                _logger.LogDebug(
                    "Response validation completed for {ResponseType} in {Duration}ms. IsValid: {IsValid}, ErrorCount: {ErrorCount}",
                    typeof(TResponse).Name,
                    responseValidationStopwatch.ElapsedMilliseconds,
                    responseResult.IsValid,
                    responseResult.Errors.Count);
            }

            // Handle validation result according to strategy
            if (!responseResult.IsValid)
            {
                var context = new ValidationContext
                {
                    ObjectType = typeof(TResponse),
                    ObjectInstance = response,
                    Schema = null,
                    IsRequest = false,
                    HandlerName = _handlerKey
                };

                var handledResult = await validationStrategy.HandleResultAsync(responseResult, context, cancellationToken);

                // If strategy didn't throw and result is still invalid, check throwOnFailure
                if (!handledResult.IsValid && throwOnFailure)
                {
                    throw new ContractValidationException(typeof(TResponse),
                        handledResult.Errors.Select(e => e.ToString()).ToArray());
                }
            }
        }

        overallStopwatch.Stop();

        // Log overall performance metrics
        if (contractValidationOptions.EnablePerformanceMetrics)
        {
            _logger.LogDebug(
                "Contract validation pipeline completed for {RequestType} -> {ResponseType} in {Duration}ms",
                typeof(TRequest).Name,
                typeof(TResponse).Name,
                overallStopwatch.ElapsedMilliseconds);
        }

        return response;
    }

    private ContractValidationOptions GetContractValidationOptions()
    {
        // Check for handler-specific overrides
        if (_options.Value.ContractValidationOverrides.TryGetValue(_handlerKey, out var handlerOptions))
        {
            return handlerOptions;
        }

        // Return default options
        return _options.Value.DefaultContractValidationOptions;
    }

    private static bool IsContractValidationEnabled(ContractValidationOptions contractValidationOptions, ValidateContractAttribute? validateContractAttribute)
    {
        // If contract validation is explicitly disabled globally, return false
        if (!contractValidationOptions.EnableAutomaticContractValidation && validateContractAttribute == null)
        {
            return false;
        }

        // If contract validation is enabled globally or explicitly enabled with ValidateContractAttribute, return true
        return contractValidationOptions.EnableAutomaticContractValidation || validateContractAttribute != null;
    }

    private static (bool validateRequest, bool validateResponse, bool throwOnFailure) GetValidationParameters(
        ContractValidationOptions contractValidationOptions, ValidateContractAttribute? validateContractAttribute)
    {
        if (validateContractAttribute != null)
        {
            return (validateContractAttribute.ValidateRequest,
                   validateContractAttribute.ValidateResponse,
                   validateContractAttribute.ThrowOnValidationFailure);
        }

        return (contractValidationOptions.ValidateRequests,
               contractValidationOptions.ValidateResponses,
               contractValidationOptions.ThrowOnValidationFailure);
    }

    /// <summary>
    /// Gets the validation strategy to use for this request.
    /// </summary>
    /// <param name="contractValidationOptions">The contract validation options.</param>
    /// <param name="validateContractAttribute">The validate contract attribute, if present.</param>
    /// <returns>The validation strategy to use.</returns>
    private IValidationStrategy GetValidationStrategy(
        ContractValidationOptions contractValidationOptions,
        ValidateContractAttribute? validateContractAttribute)
    {
        // Check for per-handler strategy override in attribute
        string? strategyName = null;
        
        // Use attribute strategy if available (future enhancement)
        // For now, use configuration strategy
        strategyName = contractValidationOptions.ValidationStrategy;

        // Create strategy using factory if available
        if (_strategyFactory != null && !string.IsNullOrWhiteSpace(strategyName))
        {
            try
            {
                return _strategyFactory.CreateStrategy(strategyName);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, 
                    "Failed to create validation strategy '{StrategyName}', falling back to Strict strategy", 
                    strategyName);
            }
        }

        // Fallback to strict strategy
        return new StrictValidationStrategy();
    }

    /// <summary>
    /// Validates the request contract with detailed error reporting.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="validationStrategy">The validation strategy to use.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A detailed validation result.</returns>
    private async ValueTask<ValidationResult> ValidateRequestContractDetailed(
        TRequest request,
        IValidationStrategy validationStrategy,
        CancellationToken cancellationToken)
    {
        // Resolve schema using SchemaResolver if available
        var schema = await ResolveSchemaAsync(typeof(TRequest), isRequest: true, cancellationToken);

        // Create validation context
        var context = new ValidationContext
        {
            ObjectType = typeof(TRequest),
            ObjectInstance = request,
            Schema = schema,
            IsRequest = true,
            HandlerName = _handlerKey
        };

        // Check if validation should be performed according to strategy
        if (!validationStrategy.ShouldValidate(context))
        {
            return ValidationResult.Success(nameof(ContractValidationPipelineBehavior<TRequest, TResponse>));
        }

        // If no schema found, return success (no validation to perform)
        if (schema == null || string.IsNullOrWhiteSpace(schema.Schema))
        {
            _logger.LogDebug(
                "No schema found for request type {RequestType}, skipping schema validation",
                typeof(TRequest).Name);
            return ValidationResult.Success(nameof(ContractValidationPipelineBehavior<TRequest, TResponse>));
        }

        // Perform validation using the enhanced validator
        if (_contractValidator is DefaultContractValidator enhancedValidator)
        {
            return await enhancedValidator.ValidateRequestDetailedAsync(request!, schema, context, cancellationToken);
        }

        // Fallback to legacy validation for backward compatibility
        var errors = await _contractValidator.ValidateRequestAsync(request!, schema, cancellationToken);
        var errorList = errors.Select(e => ValidationError.Create(
            ValidationErrorCodes.GeneralValidationError,
            e)).ToList();

        return errorList.Any()
            ? ValidationResult.Failure(errorList.ToArray())
            : ValidationResult.Success(nameof(ContractValidationPipelineBehavior<TRequest, TResponse>));
    }

    /// <summary>
    /// Validates the response contract with detailed error reporting.
    /// </summary>
    /// <param name="response">The response to validate.</param>
    /// <param name="validationStrategy">The validation strategy to use.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A detailed validation result.</returns>
    private async ValueTask<ValidationResult> ValidateResponseContractDetailed(
        TResponse response,
        IValidationStrategy validationStrategy,
        CancellationToken cancellationToken)
    {
        // Resolve schema using SchemaResolver if available
        var schema = await ResolveSchemaAsync(typeof(TResponse), isRequest: false, cancellationToken);

        // Create validation context
        var context = new ValidationContext
        {
            ObjectType = typeof(TResponse),
            ObjectInstance = response,
            Schema = schema,
            IsRequest = false,
            HandlerName = _handlerKey
        };

        // Check if validation should be performed according to strategy
        if (!validationStrategy.ShouldValidate(context))
        {
            return ValidationResult.Success(nameof(ContractValidationPipelineBehavior<TRequest, TResponse>));
        }

        // If no schema found, return success (no validation to perform)
        if (schema == null || string.IsNullOrWhiteSpace(schema.Schema))
        {
            _logger.LogDebug(
                "No schema found for response type {ResponseType}, skipping schema validation",
                typeof(TResponse).Name);
            return ValidationResult.Success(nameof(ContractValidationPipelineBehavior<TRequest, TResponse>));
        }

        // Perform validation using the enhanced validator
        if (_contractValidator is DefaultContractValidator enhancedValidator)
        {
            return await enhancedValidator.ValidateResponseDetailedAsync(response!, schema, context, cancellationToken);
        }

        // Fallback to legacy validation for backward compatibility
        var errors = await _contractValidator.ValidateResponseAsync(response!, schema, cancellationToken);
        var errorList = errors.Select(e => ValidationError.Create(
            ValidationErrorCodes.GeneralValidationError,
            e)).ToList();

        return errorList.Any()
            ? ValidationResult.Failure(errorList.ToArray())
            : ValidationResult.Success(nameof(ContractValidationPipelineBehavior<TRequest, TResponse>));
    }

    /// <summary>
    /// Resolves a schema for the specified type using the schema resolver or metadata system.
    /// </summary>
    /// <param name="type">The type to resolve a schema for.</param>
    /// <param name="isRequest">Whether this is a request (true) or response (false) schema.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The resolved schema contract, or null if no schema is found.</returns>
    private async ValueTask<JsonSchemaContract?> ResolveSchemaAsync(
        Type type,
        bool isRequest,
        CancellationToken cancellationToken)
    {
        // Try to resolve schema using SchemaResolver if available
        if (_schemaResolver != null)
        {
            try
            {
                var schemaContext = new SchemaContext
                {
                    RequestType = type,
                    IsRequest = isRequest,
                    Metadata = null
                };

                var schema = await _schemaResolver.ResolveSchemaAsync(type, schemaContext, cancellationToken);
                if (schema != null)
                {
                    _logger.LogDebug(
                        "Schema resolved for {TypeName} using SchemaResolver",
                        type.Name);
                    return schema;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to resolve schema for {TypeName} using SchemaResolver",
                    type.Name);
            }
        }

        // Try to get schema from type metadata (future enhancement)
        // For now, return null to indicate no schema found
        _logger.LogDebug(
            "No schema resolver available or schema not found for {TypeName}",
            type.Name);

        return null;
    }
}