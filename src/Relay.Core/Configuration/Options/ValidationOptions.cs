namespace Relay.Core.Configuration.Options;

/// <summary>
/// Configuration options for validation.
/// </summary>
public class ValidationOptions
{
    /// <summary>
    /// Gets or sets whether to enable automatic validation for all requests.
    /// </summary>
    public bool EnableAutomaticValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to throw an exception when validation fails.
    /// If false, validation errors will be collected and passed to handlers.
    /// </summary>
    public bool ThrowOnValidationFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to continue executing validation rules after a rule fails.
    /// </summary>
    public bool ContinueOnFailure { get; set; } = false;

    /// <summary>
    /// Gets or sets the default order for validation pipeline behaviors.
    /// </summary>
    public int DefaultOrder { get; set; } = -1000; // Run early in the pipeline
}
