using System;

namespace Relay.Core.Publishing.Attributes;

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
