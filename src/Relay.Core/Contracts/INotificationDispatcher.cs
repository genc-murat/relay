using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core;

/// <summary>
/// Interface for dispatching notifications to their handlers.
/// This interface will be implemented by generated code.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Dispatches a notification to all its handlers.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification.</typeparam>
    /// <param name="notification">The notification to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ValueTask representing the completion of the operation.</returns>
    ValueTask DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification;
}