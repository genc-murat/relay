using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.Configuration;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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
    private readonly string _handlerKey;

    public ContractValidationPipelineBehavior(
        IContractValidator contractValidator,
        ILogger<ContractValidationPipelineBehavior<TRequest, TResponse>> logger,
        IOptions<RelayOptions> options)
    {
        _contractValidator = contractValidator ?? throw new ArgumentNullException(nameof(contractValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _handlerKey = typeof(TRequest).FullName ?? typeof(TRequest).Name;
    }

    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Get contract validation configuration
        var contractValidationOptions = GetContractValidationOptions();
        var validateContractAttribute = typeof(TRequest).GetCustomAttribute<ValidateContractAttribute>();

        // Check if contract validation is enabled for this request
        if (!IsContractValidationEnabled(contractValidationOptions, validateContractAttribute))
        {
            return await next();
        }

        // Get validation parameters
        var (validateRequest, validateResponse, throwOnFailure) = GetValidationParameters(contractValidationOptions, validateContractAttribute);

        // Validate request contract if enabled
        if (validateRequest)
        {
            var requestErrors = await ValidateRequestContract(request, cancellationToken);
            if (requestErrors.Any())
            {
                _logger.LogWarning("Request contract validation failed for {RequestType}: {Errors}",
                    typeof(TRequest).Name, string.Join(", ", requestErrors));

                if (throwOnFailure)
                {
                    throw new ContractValidationException(typeof(TRequest), requestErrors);
                }
            }
        }

        // Execute the handler
        var response = await next();

        // Validate response contract if enabled
        if (validateResponse)
        {
            var responseErrors = await ValidateResponseContract(response, cancellationToken);
            if (responseErrors.Any())
            {
                _logger.LogWarning("Response contract validation failed for {ResponseType}: {Errors}",
                    typeof(TResponse).Name, string.Join(", ", responseErrors));

                if (throwOnFailure)
                {
                    throw new ContractValidationException(typeof(TResponse), responseErrors);
                }
            }
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

    private async ValueTask<string[]> ValidateRequestContract(TRequest request, CancellationToken cancellationToken)
    {
        // In a real implementation, you would get the schema from metadata
        // For now, we'll create a simple schema
        var schema = new JsonSchemaContract
        {
            Schema = "{}", // Simple schema for now
            ContentType = "application/json"
        };

        var errors = await _contractValidator.ValidateRequestAsync(request!, schema, cancellationToken);
        return errors.ToArray();
    }

    private async ValueTask<string[]> ValidateResponseContract(TResponse response, CancellationToken cancellationToken)
    {
        // In a real implementation, you would get the schema from metadata
        // For now, we'll create a simple schema
        var schema = new JsonSchemaContract
        {
            Schema = "{}", // Simple schema for now
            ContentType = "application/json"
        };

        var errors = await _contractValidator.ValidateResponseAsync(response!, schema, cancellationToken);
        return errors.ToArray();
    }
}