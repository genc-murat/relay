using System;

namespace Relay.Core.Configuration;

/// <summary>
/// Resolved configuration for a notification handler.
/// </summary>
public class ResolvedNotificationConfiguration
{
    /// <summary>
    /// Gets the dispatch mode for the notification.
    /// </summary>
    public NotificationDispatchMode DispatchMode { get; set; }

    /// <summary>
    /// Gets the notification handler priority.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets whether to continue execution if this handler fails.
    /// </summary>
    public bool ContinueOnError { get; set; }

    /// <summary>
    /// Gets the timeout for notification handler execution.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets the maximum degree of parallelism for parallel dispatch.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; }
}
