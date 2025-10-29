using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.Configuration.Options.ContractValidation;

namespace Relay.Core.ContractValidation.Configuration;

/// <summary>
/// Validates contract validation configuration during application startup.
/// </summary>
public sealed class ContractValidationStartupValidator
{
    private readonly ContractValidationOptions _options;
    private readonly ILogger<ContractValidationStartupValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContractValidationStartupValidator"/> class.
    /// </summary>
    /// <param name="options">The contract validation options.</param>
    /// <param name="logger">The logger.</param>
    public ContractValidationStartupValidator(
        IOptions<ContractValidationOptions> options,
        ILogger<ContractValidationStartupValidator> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates the configuration and logs any issues.
    /// </summary>
    /// <returns>True if configuration is valid; otherwise, false.</returns>
    public bool Validate()
    {
        _logger.LogInformation("Validating contract validation configuration...");

        var errors = ContractValidationOptionsValidator.Validate(_options).ToList();

        if (!errors.Any())
        {
            _logger.LogInformation("Contract validation configuration is valid.");
            LogConfigurationSummary();
            return true;
        }

        _logger.LogError("Contract validation configuration validation failed with {ErrorCount} error(s):", errors.Count);
        foreach (var error in errors)
        {
            _logger.LogError("  - {Error}", error);
        }

        if (_options.FailFastOnInvalidConfiguration)
        {
            var errorMessage = $"Contract validation configuration is invalid. See logs for details.{Environment.NewLine}{string.Join(Environment.NewLine, errors.Select(e => $"  - {e}"))}";
            throw new InvalidOperationException(errorMessage);
        }

        return false;
    }

    /// <summary>
    /// Logs a summary of the current configuration.
    /// </summary>
    private void LogConfigurationSummary()
    {
        _logger.LogInformation("Contract Validation Configuration Summary:");
        _logger.LogInformation("  - EnableAutomaticContractValidation: {Value}", _options.EnableAutomaticContractValidation);
        _logger.LogInformation("  - ValidationStrategy: {Value}", _options.ValidationStrategy);
        _logger.LogInformation("  - ValidationTimeout: {Value}", _options.ValidationTimeout);
        _logger.LogInformation("  - MaxErrorCount: {Value}", _options.MaxErrorCount);
        _logger.LogInformation("  - EnableEnhancedValidation: {Value}", _options.EnableEnhancedValidation);
        _logger.LogInformation("  - EnableSchemaDiscovery: {Value}", _options.EnableSchemaDiscovery);
        _logger.LogInformation("  - EnableCustomValidators: {Value}", _options.EnableCustomValidators);
        _logger.LogInformation("  - SchemaCache.MaxCacheSize: {Value}", _options.SchemaCache.MaxCacheSize);
        _logger.LogInformation("  - SchemaCache.EnableCacheWarming: {Value}", _options.SchemaCache.EnableCacheWarming);
        _logger.LogInformation("  - SchemaDiscovery.EnableEmbeddedResources: {Value}", _options.SchemaDiscovery.EnableEmbeddedResources);
        _logger.LogInformation("  - SchemaDiscovery.EnableFileSystemWatcher: {Value}", _options.SchemaDiscovery.EnableFileSystemWatcher);
        _logger.LogInformation("  - SchemaDiscovery.EnableHttpSchemas: {Value}", _options.SchemaDiscovery.EnableHttpSchemas);
    }
}

/// <summary>
/// Extension methods for registering contract validation startup validation.
/// </summary>
public static class ContractValidationStartupValidatorExtensions
{
    /// <summary>
    /// Validates contract validation configuration during application startup.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>The service provider for chaining.</returns>
    public static IServiceProvider ValidateContractValidationConfiguration(this IServiceProvider serviceProvider)
    {
        var validator = serviceProvider.GetService<ContractValidationStartupValidator>();
        if (validator != null)
        {
            validator.Validate();
        }

        return serviceProvider;
    }
}
