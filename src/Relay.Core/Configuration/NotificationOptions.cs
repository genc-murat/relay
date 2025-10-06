using System;

namespace Relay.Core.Configuration;

/// <summary>
/// Configuration options for notification handlers.
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// Gets or sets the default dispatch mode for notifications.
    /// </summary>
    public NotificationDispatchMode DefaultDispatchMode { get; set; } = NotificationDispatchMode.Parallel;

    /// <summary>
    /// Gets or sets the default priority for notification handlers.
    /// </summary>
    public int DefaultPriority { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to continue execution if a notification handler fails.
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// Gets or sets the default timeout for notification handler execution.
    /// </summary>
    public TimeSpan? DefaultTimeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum degree of parallelism for parallel notification dispatch.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}
