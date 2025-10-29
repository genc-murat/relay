# Contract Validation Configuration

This directory contains configuration classes and validators for the Relay contract validation system.

## Configuration Classes

### ContractValidationOptions

The main configuration class for contract validation. Located at `src/Relay.Core/Configuration/Options/ContractValidation/ContractValidationOptions.cs`.

**Key Properties:**

- `EnableAutomaticContractValidation`: Master switch for automatic validation
- `ValidationStrategy`: Strategy to use ("Strict", "Lenient", "Custom")
- `ValidationTimeout`: Maximum time allowed for validation operations
- `MaxErrorCount`: Maximum number of errors to collect before stopping
- `EnableDetailedErrors`: Include detailed error information with JSON paths
- `EnableSuggestedFixes`: Generate suggested fixes for common errors
- `SchemaDiscovery`: Nested configuration for schema discovery
- `SchemaCache`: Nested configuration for schema caching

**Feature Flags:**

- `EnableEnhancedValidation`: Master feature flag for all enhanced features
- `EnableSchemaDiscovery`: Enable automatic schema discovery
- `EnableCustomValidators`: Enable custom validator support
- `EnableValidationResultCaching`: Enable caching of validation results
- `FailFastOnInvalidConfiguration`: Fail during startup if configuration is invalid

### SchemaDiscoveryOptions

Configuration for schema discovery from various sources.

**Key Properties:**

- `SchemaDirectories`: Directories to search for schema files
- `NamingConvention`: Pattern for schema file names (must include `{TypeName}`)
- `EnableEmbeddedResources`: Search for schemas in embedded resources
- `EnableFileSystemWatcher`: Watch for schema file changes
- `EnableHttpSchemas`: Load schemas from HTTP endpoints
- `HttpSchemaEndpoints`: List of HTTP endpoints to load schemas from
- `HttpSchemaTimeout`: Timeout for HTTP schema requests

### SchemaCacheOptions

Configuration for schema caching behavior.

**Key Properties:**

- `MaxCacheSize`: Maximum number of schemas to cache (default: 1000)
- `EnableCacheWarming`: Preload frequently used schemas at startup
- `WarmupTypes`: Types to preload during cache warming
- `EnableMetrics`: Enable cache metrics collection
- `MetricsReportingInterval`: How often to report cache metrics

## Validation

### ContractValidationOptionsValidator

Static validator that checks configuration for errors.

**Usage:**

```csharp
var options = new ContractValidationOptions();
var errors = ContractValidationOptionsValidator.Validate(options);

if (errors.Any())
{
    // Handle validation errors
}

// Or throw immediately
ContractValidationOptionsValidator.ValidateAndThrow(options);
```

**Validation Rules:**

- ValidationTimeout must be > 0 and <= 5 minutes
- MaxErrorCount must be > 0 and <= 10,000
- ValidationStrategy must be "Strict", "Lenient", or "Custom"
- SchemaCache.MaxCacheSize must be > 0 and <= 100,000
- NamingConvention must contain `{TypeName}` placeholder
- HTTP endpoints must be valid URLs
- Feature flag dependencies are enforced

### ContractValidationStartupValidator

Service that validates configuration during application startup.

**Usage:**

```csharp
// In Startup.cs or Program.cs
services.AddSingleton<ContractValidationStartupValidator>();

// After building the service provider
var serviceProvider = services.BuildServiceProvider();
serviceProvider.ValidateContractValidationConfiguration();
```

## Feature Flags

### ContractValidationFeatureFlags

Helper class for checking feature flag combinations.

**Usage:**

```csharp
var flags = new ContractValidationFeatureFlags(options);

if (flags.IsSchemaDiscoveryEnabled)
{
    // Schema discovery is enabled
}

if (flags.ShouldValidate(isRequest: true))
{
    // Should validate this request
}

if (flags.UseLegacyBehavior)
{
    // Use legacy validation behavior
}
```

**Available Flags:**

- `IsEnhancedValidationEnabled`: Enhanced features enabled
- `IsSchemaDiscoveryEnabled`: Schema discovery enabled
- `IsCustomValidatorsEnabled`: Custom validators enabled
- `IsDetailedErrorsEnabled`: Detailed error reporting enabled
- `IsSuggestedFixesEnabled`: Suggested fixes enabled
- `IsPerformanceMetricsEnabled`: Performance metrics enabled
- `IsValidationResultCachingEnabled`: Result caching enabled
- `IsSchemaCachingEnabled`: Schema caching enabled
- `IsCacheWarmingEnabled`: Cache warming enabled
- `IsFileSystemWatcherEnabled`: File system watching enabled
- `IsHttpSchemasEnabled`: HTTP schema loading enabled
- `IsEmbeddedResourcesEnabled`: Embedded resource schemas enabled
- `ShouldValidate(bool isRequest)`: Should validate request/response
- `UseLegacyBehavior`: Use legacy validation behavior

## Configuration Example

```csharp
services.Configure<ContractValidationOptions>(options =>
{
    // Enable automatic validation
    options.EnableAutomaticContractValidation = true;
    options.ValidateRequests = true;
    options.ValidateResponses = true;

    // Use strict validation strategy
    options.ValidationStrategy = "Strict";
    options.ValidationTimeout = TimeSpan.FromSeconds(5);
    options.MaxErrorCount = 100;

    // Enable enhanced features
    options.EnableEnhancedValidation = true;
    options.EnableDetailedErrors = true;
    options.EnableSuggestedFixes = true;

    // Configure schema discovery
    options.EnableSchemaDiscovery = true;
    options.SchemaDiscovery.SchemaDirectories.Add("./schemas");
    options.SchemaDiscovery.EnableEmbeddedResources = true;
    options.SchemaDiscovery.NamingConvention = "{TypeName}.schema.json";

    // Configure schema caching
    options.SchemaCache.MaxCacheSize = 1000;
    options.SchemaCache.EnableCacheWarming = false;
    options.SchemaCache.EnableMetrics = true;

    // Enable custom validators
    options.EnableCustomValidators = true;
});
```

## Migration from Legacy Configuration

If you're upgrading from a previous version:

1. **Existing properties remain unchanged** - Your current configuration will continue to work
2. **New features are opt-in** - Set `EnableEnhancedValidation = true` to enable new features
3. **Gradual adoption** - Enable features individually using feature flags
4. **Backward compatibility** - When `EnableEnhancedValidation = false`, behavior matches legacy version

## Validation at Startup

To ensure configuration is valid before your application starts:

```csharp
// In Program.cs
var app = builder.Build();

// Validate configuration
app.Services.ValidateContractValidationConfiguration();

app.Run();
```

This will throw an exception if configuration is invalid and `FailFastOnInvalidConfiguration = true`.
