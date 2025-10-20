using System;

namespace Relay.Core.Publishing.Attributes;

/// <summary>
/// Specifies the execution group for a notification handler.
/// Handlers in the same group can execute in parallel, while different groups execute sequentially.
/// </summary>
/// <remarks>
/// This attribute allows you to create groups of handlers that can execute concurrently,
/// while maintaining sequential execution between groups.
/// 
/// Example:
/// <code>
/// // Group 1 - Logging handlers (can execute in parallel)
/// [NotificationHandlerGroup("Logging", 1)]
/// public class FileLogger : INotificationHandler&lt;OrderCreated&gt; { }
/// 
/// [NotificationHandlerGroup("Logging", 1)]
/// public class DatabaseLogger : INotificationHandler&lt;OrderCreated&gt; { }
/// 
/// // Group 2 - Notification handlers (execute after logging completes)
/// [NotificationHandlerGroup("Notifications", 2)]
/// public class EmailSender : INotificationHandler&lt;OrderCreated&gt; { }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class NotificationHandlerGroupAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the execution group.
    /// </summary>
    public string GroupName { get; }

    /// <summary>
    /// Gets the execution order of the group. Lower values execute first.
    /// </summary>
    public int GroupOrder { get; }

    /// <summary>
    /// Initializes a new instance of the NotificationHandlerGroupAttribute class.
    /// </summary>
    /// <param name="groupName">The name of the execution group.</param>
    /// <param name="groupOrder">The execution order of the group. Lower values execute first.</param>
    public NotificationHandlerGroupAttribute(string groupName, int groupOrder = 0)
    {
        GroupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
        GroupOrder = groupOrder;
    }
}
