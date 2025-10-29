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
    /// Gets or sets whether to enable performance metrics tracking for validation operations.
    /// When enabled, validation duration and other metrics will be logged.
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;
}
