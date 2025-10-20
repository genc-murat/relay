using System;

namespace Relay.Core.Configuration.Options.Handlers;

/// <summary>
/// Configuration options for request handlers.
/// </summary>
public class HandlerOptions
{
    /// <summary>
    /// Gets or sets the default priority for handlers.
    /// </summary>
    public int DefaultPriority { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to enable caching for handlers.
    /// </summary>
    public bool EnableCaching { get; set; } = false;

    /// <summary>
    /// Gets or sets the default timeout for handler execution.
    /// </summary>
    public TimeSpan? DefaultTimeout { get; set; }

    /// <summary>
    /// Gets or sets whether to enable retry logic for handlers.
    /// </summary>
    public bool EnableRetry { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
}
