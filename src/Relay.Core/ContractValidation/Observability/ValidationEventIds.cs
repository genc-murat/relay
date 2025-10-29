using Microsoft.Extensions.Logging;

namespace Relay.Core.ContractValidation.Observability;

/// <summary>
/// Defines event IDs for structured logging in contract validation.
/// </summary>
public static class ValidationEventIds
{
    /// <summary>
    /// Validation started event.
    /// </summary>
    public static readonly EventId ValidationStarted = new(1000, nameof(ValidationStarted));

    /// <summary>
    /// Validation completed event.
    /// </summary>
    public static readonly EventId ValidationCompleted = new(1001, nameof(ValidationCompleted));

    /// <summary>
    /// Validation failed event.
    /// </summary>
    public static readonly EventId ValidationFailed = new(1002, nameof(ValidationFailed));

    /// <summary>
    /// Schema resolution started event.
    /// </summary>
    public static readonly EventId SchemaResolutionStarted = new(2000, nameof(SchemaResolutionStarted));

    /// <summary>
    /// Schema resolution completed event.
    /// </summary>
    public static readonly EventId SchemaResolutionCompleted = new(2001, nameof(SchemaResolutionCompleted));

    /// <summary>
    /// Schema resolution failed event.
    /// </summary>
    public static readonly EventId SchemaResolutionFailed = new(2002, nameof(SchemaResolutionFailed));

    /// <summary>
    /// Schema cache hit event.
    /// </summary>
    public static readonly EventId SchemaCacheHit = new(2100, nameof(SchemaCacheHit));

    /// <summary>
    /// Schema cache miss event.
    /// </summary>
    public static readonly EventId SchemaCacheMiss = new(2101, nameof(SchemaCacheMiss));

    /// <summary>
    /// Schema discovery started event.
    /// </summary>
    public static readonly EventId SchemaDiscoveryStarted = new(2200, nameof(SchemaDiscoveryStarted));

    /// <summary>
    /// Schema discovery completed event.
    /// </summary>
    public static readonly EventId SchemaDiscoveryCompleted = new(2201, nameof(SchemaDiscoveryCompleted));

    /// <summary>
    /// Custom validator execution started event.
    /// </summary>
    public static readonly EventId CustomValidatorStarted = new(3000, nameof(CustomValidatorStarted));

    /// <summary>
    /// Custom validator execution completed event.
    /// </summary>
    public static readonly EventId CustomValidatorCompleted = new(3001, nameof(CustomValidatorCompleted));

    /// <summary>
    /// Custom validator execution failed event.
    /// </summary>
    public static readonly EventId CustomValidatorFailed = new(3002, nameof(CustomValidatorFailed));

    /// <summary>
    /// Validation timeout event.
    /// </summary>
    public static readonly EventId ValidationTimeout = new(4000, nameof(ValidationTimeout));

    /// <summary>
    /// Performance warning event.
    /// </summary>
    public static readonly EventId PerformanceWarning = new(5000, nameof(PerformanceWarning));

    /// <summary>
    /// Configuration error event.
    /// </summary>
    public static readonly EventId ConfigurationError = new(6000, nameof(ConfigurationError));
}
