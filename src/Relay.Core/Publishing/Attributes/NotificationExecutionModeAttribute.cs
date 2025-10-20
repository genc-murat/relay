using System;

namespace Relay.Core.Publishing.Attributes;

/// <summary>
/// Specifies the execution mode for a notification handler.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class NotificationExecutionModeAttribute : Attribute
{
    /// <summary>
    /// Gets the execution mode for the handler.
    /// </summary>
    public NotificationExecutionMode Mode { get; }

    /// <summary>
    /// Gets a value indicating whether the handler can execute in parallel with others in the same group.
    /// </summary>
    public bool AllowParallelExecution { get; set; } = true;

    /// <summary>
    /// Gets a value indicating whether exceptions from this handler should be suppressed.
    /// </summary>
    public bool SuppressExceptions { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the NotificationExecutionModeAttribute class.
    /// </summary>
    /// <param name="mode">The execution mode.</param>
    public NotificationExecutionModeAttribute(NotificationExecutionMode mode = NotificationExecutionMode.Default)
    {
        Mode = mode;
    }
}
