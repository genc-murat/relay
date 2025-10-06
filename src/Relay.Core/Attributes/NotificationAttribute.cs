using System;

namespace Relay.Core
{
    /// <summary>
    /// Attribute to mark methods as notification handlers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class NotificationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the dispatch mode for notification handling.
        /// </summary>
        public NotificationDispatchMode DispatchMode { get; set; } = NotificationDispatchMode.Parallel;

        /// <summary>
        /// Gets or sets the priority of the notification handler. Higher values indicate higher priority.
        /// </summary>
        public int Priority { get; set; } = 0;
    }
}