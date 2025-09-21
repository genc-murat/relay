using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    /// <summary>
    /// Interface for dispatching requests to their handlers.
    /// This interface will be implemented by generated code.
    /// </summary>
    public interface IRequestDispatcher
    {
        /// <summary>
        /// Dispatches a request with a response to its handler.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The request to dispatch.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask containing the response.</returns>
        ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken);

        /// <summary>
        /// Dispatches a request without a response to its handler.
        /// </summary>
        /// <param name="request">The request to dispatch.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the operation.</returns>
        ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Dispatches a named request with a response to its handler.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The request to dispatch.</param>
        /// <param name="handlerName">The name of the handler to use.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask containing the response.</returns>
        ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken);

        /// <summary>
        /// Dispatches a named request without a response to its handler.
        /// </summary>
        /// <param name="request">The request to dispatch.</param>
        /// <param name="handlerName">The name of the handler to use.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the operation.</returns>
        ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Interface for dispatching streaming requests to their handlers.
    /// This interface will be implemented by generated code.
    /// </summary>
    public interface IStreamDispatcher
    {
        /// <summary>
        /// Dispatches a streaming request to its handler.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response items.</typeparam>
        /// <param name="request">The streaming request to dispatch.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>An async enumerable of response items.</returns>
        IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken);

        /// <summary>
        /// Dispatches a named streaming request to its handler.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response items.</typeparam>
        /// <param name="request">The streaming request to dispatch.</param>
        /// <param name="handlerName">The name of the handler to use.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>An async enumerable of response items.</returns>
        IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken);
    }

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
}