using System;

namespace Relay.Core.Publishing.Attributes
{
    /// <summary>
    /// Specifies the execution order of a notification handler.
    /// Use this attribute to control the sequence in which notification handlers are executed.
    /// </summary>
    /// <remarks>
    /// This attribute provides MediatR-compatible handler ordering functionality.
    /// Handlers with lower Order values execute first (ascending order).
    /// Multiple handlers can have the same order value - in that case, they may execute in any order.
    /// 
    /// Example:
    /// <code>
    /// [NotificationHandlerOrder(1)]
    /// public class FirstHandler : INotificationHandler&lt;MyNotification&gt; { }
    /// 
    /// [NotificationHandlerOrder(2)]
    /// public class SecondHandler : INotificationHandler&lt;MyNotification&gt; { }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class NotificationHandlerOrderAttribute : Attribute
    {
        /// <summary>
        /// Gets the execution order of the handler.
        /// Lower values execute first.
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Initializes a new instance of the NotificationHandlerOrderAttribute class.
        /// </summary>
        /// <param name="order">The execution order. Lower values execute first.</param>
        public NotificationHandlerOrderAttribute(int order)
        {
            Order = order;
        }
    }

    /// <summary>
    /// Specifies that a notification handler should execute before another handler.
    /// This creates an explicit dependency relationship between handlers.
    /// </summary>
    /// <remarks>
    /// This attribute is useful when you need to ensure one handler completes before another starts,
    /// regardless of their Order values.
    /// 
    /// Example:
    /// <code>
    /// public class LoggingHandler : INotificationHandler&lt;OrderCreated&gt; { }
    /// 
    /// [ExecuteAfter(typeof(LoggingHandler))]
    /// public class EmailHandler : INotificationHandler&lt;OrderCreated&gt; { }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ExecuteAfterAttribute : Attribute
    {
        /// <summary>
        /// Gets the handler type that must execute before this handler.
        /// </summary>
        public Type HandlerType { get; }

        /// <summary>
        /// Initializes a new instance of the ExecuteAfterAttribute class.
        /// </summary>
        /// <param name="handlerType">The handler type that must execute first.</param>
        public ExecuteAfterAttribute(Type handlerType)
        {
            HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
        }
    }

    /// <summary>
    /// Specifies that a notification handler should execute before another handler.
    /// This creates an explicit dependency relationship between handlers.
    /// </summary>
    /// <remarks>
    /// This attribute is useful when you need to ensure this handler completes before another starts.
    /// 
    /// Example:
    /// <code>
    /// [ExecuteBefore(typeof(EmailHandler))]
    /// public class ValidationHandler : INotificationHandler&lt;OrderCreated&gt; { }
    /// 
    /// public class EmailHandler : INotificationHandler&lt;OrderCreated&gt; { }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ExecuteBeforeAttribute : Attribute
    {
        /// <summary>
        /// Gets the handler type that must execute after this handler.
        /// </summary>
        public Type HandlerType { get; }

        /// <summary>
        /// Initializes a new instance of the ExecuteBeforeAttribute class.
        /// </summary>
        /// <param name="handlerType">The handler type that must execute later.</param>
        public ExecuteBeforeAttribute(Type handlerType)
        {
            HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
        }
    }

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
}
