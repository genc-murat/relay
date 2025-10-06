namespace Relay.Core.Configuration;

/// <summary>
/// Configuration options for retry behavior.
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Gets or sets whether to enable automatic retry for all requests.
    /// </summary>
    public bool EnableAutomaticRetry { get; set; } = false;

    /// <summary>
    /// Gets or sets the default maximum number of retry attempts.
    /// </summary>
    public int DefaultMaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the default retry delay in milliseconds.
    /// </summary>
    public int DefaultRetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the retry strategy type.
    /// </summary>
    public string DefaultRetryStrategy { get; set; } = "Linear";

    /// <summary>
    /// Gets or sets whether to throw an exception when all retry attempts are exhausted.
    /// </summary>
    public bool ThrowOnRetryExhausted { get; set; } = true;

    /// <summary>
    /// Gets or sets the default order for retry pipeline behaviors.
    /// </summary>
    public int DefaultOrder { get; set; } = -500; // Run early in the pipeline
}