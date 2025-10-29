using System;
using Relay.Core.ContractValidation.Caching;
using Relay.Core.ContractValidation.SchemaDiscovery;

namespace Relay.Core.Configuration.Options.ContractValidation;

/// <summary>
/// Configuration options for contract validation.
/// </summary>
public class ContractValidationOptions
{
    /// <summary>
    /// Gets or sets whether to enable automatic contract validation for all requests.
    /// </summary>
    public bool EnableAutomaticContractValidation { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to validate request contracts.
    /// </summary>
    public bool ValidateRequests { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate response contracts.
    /// </summary>
    public bool ValidateResponses { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to throw an exception when validation fails.
    /// </summary>
    public bool ThrowOnValidationFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets the default order for contract validation pipeline behaviors.
    /// </summary>
    public int DefaultOrder { get; set; } = -750; // Run early in the pipeline

    /// <summary>
    /// Gets or sets the validation strategy to use (e.g., "Strict", "Lenient").
    /// Default is "Strict" which throws exceptions on validation failures.
    /// </summary>
    public string ValidationStrategy { get; set; } = "Strict";

    /// <summary>
    /// Gets or sets the timeout for validation operations.
    /// If validation takes longer than this timeout, it will be cancelled.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the maximum number of validation errors to collect before stopping validation.
    /// This prevents excessive memory usage when validating large invalid payloads.
    /// Default is 100.
    /// </summary>
    public int MaxErrorCount { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to include detailed error information in validation results.
    /// When enabled, errors will include JSON paths, expected values, and actual values.
    /// Default is true.
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate suggested fixes for validation errors.
    /// When enabled, the system will provide recommendations for fixing common validation issues.
    /// Default is true.
    /// </summary>
    public bool EnableSuggestedFixes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable performance metrics tracking for validation operations.
    /// When enabled, validation duration and other metrics will be logged.
    /// Default is true.
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the schema discovery configuration options.
    /// </summary>
    public SchemaDiscoveryOptions SchemaDiscovery { get; set; } = new();

    /// <summary>
    /// Gets or sets the schema cache configuration options.
    /// </summary>
    public SchemaCacheOptions SchemaCache { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to enable enhanced validation features.
    /// This feature flag allows gradual adoption of new validation capabilities.
    /// Default is true.
    /// </summary>
    public bool EnableEnhancedValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable schema discovery from configured sources.
    /// When false, only explicitly provided schemas will be used.
    /// Default is true.
    /// </summary>
    public bool EnableSchemaDiscovery { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable custom validators.
    /// When false, only JSON schema validation will be performed.
    /// Default is true.
    /// </summary>
    public bool EnableCustomValidators { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable validation result caching.
    /// When enabled, validation results for identical inputs may be cached.
    /// Default is false.
    /// </summary>
    public bool EnableValidationResultCaching { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to fail fast during application startup if configuration is invalid.
    /// When true, the application will not start if validation configuration is invalid.
    /// Default is true.
    /// </summary>
    public bool FailFastOnInvalidConfiguration { get; set; } = true;
}
