using System;
using System.Collections.Generic;
using System.Linq;
using Relay.Core.Configuration.Options.ContractValidation;

namespace Relay.Core.ContractValidation.Configuration;

/// <summary>
/// Validates <see cref="ContractValidationOptions"/> configuration.
/// </summary>
public static class ContractValidationOptionsValidator
{
    /// <summary>
    /// Validates the contract validation options and returns any validation errors.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>A collection of validation error messages. Empty if valid.</returns>
    public static IEnumerable<string> Validate(ContractValidationOptions options)
    {
        if (options == null)
        {
            yield return "ContractValidationOptions cannot be null.";
            yield break;
        }

        // Validate ValidationTimeout
        if (options.ValidationTimeout <= TimeSpan.Zero)
        {
            yield return $"ValidationTimeout must be greater than zero. Current value: {options.ValidationTimeout}";
        }

        if (options.ValidationTimeout > TimeSpan.FromMinutes(5))
        {
            yield return $"ValidationTimeout should not exceed 5 minutes to prevent request timeouts. Current value: {options.ValidationTimeout}";
        }

        // Validate MaxErrorCount
        if (options.MaxErrorCount <= 0)
        {
            yield return $"MaxErrorCount must be greater than zero. Current value: {options.MaxErrorCount}";
        }

        if (options.MaxErrorCount > 10000)
        {
            yield return $"MaxErrorCount should not exceed 10000 to prevent excessive memory usage. Current value: {options.MaxErrorCount}";
        }

        // Validate ValidationStrategy
        var validStrategies = new[] { "Strict", "Lenient", "Custom" };
        if (string.IsNullOrWhiteSpace(options.ValidationStrategy))
        {
            yield return "ValidationStrategy cannot be null or empty.";
        }
        else if (!validStrategies.Contains(options.ValidationStrategy, StringComparer.OrdinalIgnoreCase))
        {
            yield return $"ValidationStrategy must be one of: {string.Join(", ", validStrategies)}. Current value: {options.ValidationStrategy}";
        }

        // Validate SchemaCache options
        if (options.SchemaCache != null)
        {
            foreach (var error in ValidateSchemaCacheOptions(options.SchemaCache))
            {
                yield return error;
            }
        }

        // Validate SchemaDiscovery options
        if (options.SchemaDiscovery != null)
        {
            foreach (var error in ValidateSchemaDiscoveryOptions(options.SchemaDiscovery))
            {
                yield return error;
            }
        }

        // Validate feature flag combinations
        if (!options.EnableEnhancedValidation && options.EnableSchemaDiscovery)
        {
            yield return "EnableSchemaDiscovery requires EnableEnhancedValidation to be true.";
        }

        if (!options.EnableEnhancedValidation && options.EnableCustomValidators)
        {
            yield return "EnableCustomValidators requires EnableEnhancedValidation to be true.";
        }

        // Validate logical consistency
        if (!options.ValidateRequests && !options.ValidateResponses)
        {
            yield return "At least one of ValidateRequests or ValidateResponses must be true when EnableAutomaticContractValidation is enabled.";
        }
    }

    /// <summary>
    /// Validates the schema cache options.
    /// </summary>
    private static IEnumerable<string> ValidateSchemaCacheOptions(Relay.Core.ContractValidation.Caching.SchemaCacheOptions options)
    {
        if (options.MaxCacheSize <= 0)
        {
            yield return $"SchemaCache.MaxCacheSize must be greater than zero. Current value: {options.MaxCacheSize}";
        }

        if (options.MaxCacheSize > 100000)
        {
            yield return $"SchemaCache.MaxCacheSize should not exceed 100000 to prevent excessive memory usage. Current value: {options.MaxCacheSize}";
        }

        if (options.MetricsReportingInterval <= TimeSpan.Zero)
        {
            yield return $"SchemaCache.MetricsReportingInterval must be greater than zero. Current value: {options.MetricsReportingInterval}";
        }

        if (options.EnableCacheWarming && (options.WarmupTypes == null || !options.WarmupTypes.Any()))
        {
            yield return "SchemaCache.EnableCacheWarming is true but WarmupTypes is empty. Provide types to warm up or disable cache warming.";
        }
    }

    /// <summary>
    /// Validates the schema discovery options.
    /// </summary>
    private static IEnumerable<string> ValidateSchemaDiscoveryOptions(Relay.Core.ContractValidation.SchemaDiscovery.SchemaDiscoveryOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.NamingConvention))
        {
            yield return "SchemaDiscovery.NamingConvention cannot be null or empty.";
        }
        else if (!options.NamingConvention.Contains("{TypeName}"))
        {
            yield return "SchemaDiscovery.NamingConvention must contain the {TypeName} placeholder.";
        }

        if (options.HttpSchemaTimeout <= TimeSpan.Zero)
        {
            yield return $"SchemaDiscovery.HttpSchemaTimeout must be greater than zero. Current value: {options.HttpSchemaTimeout}";
        }

        if (options.HttpSchemaTimeout > TimeSpan.FromMinutes(1))
        {
            yield return $"SchemaDiscovery.HttpSchemaTimeout should not exceed 1 minute. Current value: {options.HttpSchemaTimeout}";
        }

        if (options.EnableHttpSchemas && (options.HttpSchemaEndpoints == null || !options.HttpSchemaEndpoints.Any()))
        {
            yield return "SchemaDiscovery.EnableHttpSchemas is true but HttpSchemaEndpoints is empty. Provide endpoints or disable HTTP schemas.";
        }

        if (options.HttpSchemaEndpoints != null)
        {
            foreach (var endpoint in options.HttpSchemaEndpoints)
            {
                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    yield return "SchemaDiscovery.HttpSchemaEndpoints contains null or empty endpoint.";
                }
                else if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    yield return $"SchemaDiscovery.HttpSchemaEndpoints contains invalid URL: {endpoint}";
                }
            }
        }

        if (options.SchemaDirectories != null)
        {
            foreach (var directory in options.SchemaDirectories)
            {
                if (string.IsNullOrWhiteSpace(directory))
                {
                    yield return "SchemaDiscovery.SchemaDirectories contains null or empty directory path.";
                }
            }
        }
    }

    /// <summary>
    /// Validates the options and throws an exception if validation fails.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    public static void ValidateAndThrow(ContractValidationOptions options)
    {
        var errors = Validate(options).ToList();
        if (errors.Any())
        {
            var errorMessage = $"Contract validation configuration is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors.Select(e => $"  - {e}"))}";
            throw new InvalidOperationException(errorMessage);
        }
    }
}
