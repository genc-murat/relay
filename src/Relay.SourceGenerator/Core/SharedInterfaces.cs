// Shared interface definitions for source generator
// These are minimal definitions to avoid project reference conflicts

namespace Relay.Core
{
    /// <summary>
    /// Marker interface for requests that return void.
    /// </summary>
    public interface IRequest
    {
    }

    /// <summary>
    /// Marker interface for requests that return a response.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    public interface IRequest<out TResponse>
    {
    }

    /// <summary>
    /// Marker interface for streaming requests that return multiple responses.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response items.</typeparam>
    public interface IStreamRequest<out TResponse>
    {
    }

    /// <summary>
    /// Marker interface for notifications.
    /// </summary>
    public interface INotification
    {
    }

    /// <summary>
    /// Interface for the main Relay mediator.
    /// </summary>
    public interface IRelay
    {
    }

    /// <summary>
    /// Interface for notification dispatcher.
    /// </summary>
    public interface INotificationDispatcher
    {
    }

    /// <summary>
    /// Notification dispatch mode enumeration.
    /// </summary>
    public enum NotificationDispatchMode
    {
        /// <summary>
        /// Dispatch notifications sequentially.
        /// </summary>
        Sequential,

        /// <summary>
        /// Dispatch notifications in parallel.
        /// </summary>
        Parallel
    }
}