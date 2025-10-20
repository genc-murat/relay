namespace Relay.Core.Publishing.Attributes;

/// <summary>
/// Defines execution modes for notification handlers.
/// </summary>
public enum NotificationExecutionMode
{
    /// <summary>
    /// Use the default execution mode defined by the publisher strategy.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Always execute this handler sequentially, even if parallel mode is enabled.
    /// </summary>
    Sequential = 1,

    /// <summary>
    /// Execute this handler in parallel when possible.
    /// </summary>
    Parallel = 2,

    /// <summary>
    /// Execute this handler asynchronously without waiting for completion (fire-and-forget).
    /// Use with caution - errors may not be caught.
    /// </summary>
    FireAndForget = 3,

    /// <summary>
    /// Execute this handler with high priority - it will run before handlers with normal priority.
    /// </summary>
    HighPriority = 4,

    /// <summary>
    /// Execute this handler with low priority - it will run after handlers with normal priority.
    /// </summary>
    LowPriority = 5
}
