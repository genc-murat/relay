using System;

namespace Relay.Core.Publishing.Attributes;

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
