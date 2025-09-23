using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    /// <summary>
    /// Interface for handling requests that return a response.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public interface IRequestHandler<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        /// <summary>
        /// Handles the request asynchronously.
        /// </summary>
        /// <param name="request">The request to handle.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask containing the response.</returns>
        ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Interface for handling requests that do not return a response.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    public interface IRequestHandler<in TRequest>
        where TRequest : IRequest
    {
        /// <summary>
        /// Handles the request asynchronously.
        /// </summary>
        /// <param name="request">The request to handle.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the operation.</returns>
        ValueTask HandleAsync(TRequest request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Interface for handling streaming requests.
    /// </summary>
    /// <typeparam name="TRequest">The type of the streaming request.</typeparam>
    /// <typeparam name="TResponse">The type of the response items.</typeparam>
    public interface IStreamHandler<in TRequest, out TResponse>
        where TRequest : IStreamRequest<TResponse>
    {
        /// <summary>
        /// Handles the streaming request asynchronously.
        /// </summary>
        /// <param name="request">The streaming request to handle.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>An async enumerable of response items.</returns>
        IAsyncEnumerable<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Interface for handling notifications.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification.</typeparam>
    public interface INotificationHandler<in TNotification>
        where TNotification : INotification
    {
        /// <summary>
        /// Handles the notification asynchronously.
        /// </summary>
        /// <param name="notification">The notification to handle.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the operation.</returns>
        ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken);
    }
}