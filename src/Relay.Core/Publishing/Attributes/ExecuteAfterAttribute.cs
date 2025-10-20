using System;

namespace Relay.Core.Publishing.Attributes;

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
