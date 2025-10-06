using System;

namespace Relay.Core.Configuration;

/// <summary>
/// Resolved configuration for a request handler.
/// </summary>
public class ResolvedHandlerConfiguration
{
    /// <summary>
    /// Gets the handler name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets the handler priority.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets whether caching is enabled for this handler.
    /// </summary>
    public bool EnableCaching { get; set; }

    /// <summary>
    /// Gets the timeout for handler execution.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets whether retry logic is enabled for this handler.
    /// </summary>
    public bool EnableRetry { get; set; }

    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; set; }
}
