using Relay.Core.Configuration.Options.ContractValidation;

namespace Relay.Core.ContractValidation.Configuration;

/// <summary>
/// Helper class for checking contract validation feature flags.
/// </summary>
public sealed class ContractValidationFeatureFlags
{
    private readonly ContractValidationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContractValidationFeatureFlags"/> class.
    /// </summary>
    /// <param name="options">The contract validation options.</param>
    public ContractValidationFeatureFlags(ContractValidationOptions options)
    {
        _options = options ?? throw new System.ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets a value indicating whether enhanced validation features are enabled.
    /// </summary>
    public bool IsEnhancedValidationEnabled => _options.EnableEnhancedValidation;

    /// <summary>
    /// Gets a value indicating whether schema discovery is enabled.
    /// </summary>
    public bool IsSchemaDiscoveryEnabled => _options.EnableEnhancedValidation && _options.EnableSchemaDiscovery;

    /// <summary>
    /// Gets a value indicating whether custom validators are enabled.
    /// </summary>
    public bool IsCustomValidatorsEnabled => _options.EnableEnhancedValidation && _options.EnableCustomValidators;

    /// <summary>
    /// Gets a value indicating whether detailed error reporting is enabled.
    /// </summary>
    public bool IsDetailedErrorsEnabled => _options.EnableEnhancedValidation && _options.EnableDetailedErrors;

    /// <summary>
    /// Gets a value indicating whether suggested fixes are enabled.
    /// </summary>
    public bool IsSuggestedFixesEnabled => _options.EnableEnhancedValidation && _options.EnableSuggestedFixes;

    /// <summary>
    /// Gets a value indicating whether performance metrics are enabled.
    /// </summary>
    public bool IsPerformanceMetricsEnabled => _options.EnablePerformanceMetrics;

    /// <summary>
    /// Gets a value indicating whether validation result caching is enabled.
    /// </summary>
    public bool IsValidationResultCachingEnabled => _options.EnableEnhancedValidation && _options.EnableValidationResultCaching;

    /// <summary>
    /// Gets a value indicating whether schema caching is enabled.
    /// </summary>
    public bool IsSchemaCachingEnabled => _options.EnableEnhancedValidation && _options.SchemaCache.EnableMetrics;

    /// <summary>
    /// Gets a value indicating whether cache warming is enabled.
    /// </summary>
    public bool IsCacheWarmingEnabled => _options.EnableEnhancedValidation && _options.SchemaCache.EnableCacheWarming;

    /// <summary>
    /// Gets a value indicating whether file system watching is enabled for schema changes.
    /// </summary>
    public bool IsFileSystemWatcherEnabled => _options.EnableEnhancedValidation && _options.SchemaDiscovery.EnableFileSystemWatcher;

    /// <summary>
    /// Gets a value indicating whether HTTP schema loading is enabled.
    /// </summary>
    public bool IsHttpSchemasEnabled => _options.EnableEnhancedValidation && _options.SchemaDiscovery.EnableHttpSchemas;

    /// <summary>
    /// Gets a value indicating whether embedded resource schemas are enabled.
    /// </summary>
    public bool IsEmbeddedResourcesEnabled => _options.EnableEnhancedValidation && _options.SchemaDiscovery.EnableEmbeddedResources;

    /// <summary>
    /// Determines whether validation should be performed based on configuration.
    /// </summary>
    /// <param name="isRequest">True if validating a request; false if validating a response.</param>
    /// <returns>True if validation should be performed; otherwise, false.</returns>
    public bool ShouldValidate(bool isRequest)
    {
        if (!_options.EnableAutomaticContractValidation)
        {
            return false;
        }

        return isRequest ? _options.ValidateRequests : _options.ValidateResponses;
    }

    /// <summary>
    /// Determines whether to use legacy validation behavior.
    /// </summary>
    /// <returns>True if legacy behavior should be used; otherwise, false.</returns>
    public bool UseLegacyBehavior => !_options.EnableEnhancedValidation;
}
