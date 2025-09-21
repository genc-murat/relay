using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    /// <summary>
    /// Primary interface for the Relay mediator framework.
    /// Provides methods for sending requests, streaming data, and publishing notifications.
    /// </summary>
    public interface IRelay
    {
        /// <summary>
        /// Sends a request and returns a response asynchronously.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The request to send.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask containing the response.</returns>
        ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a request without expecting a response.
        /// </summary>
        /// <param name="request">The request to send.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the operation.</returns>
        ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a streaming request and returns an async enumerable of responses.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response items.</typeparam>
        /// <param name="request">The streaming request to send.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>An async enumerable of response items.</returns>
        IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a notification to all registered handlers.
        /// </summary>
        /// <typeparam name="TNotification">The type of the notification.</typeparam>
        /// <param name="notification">The notification to publish.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the operation.</returns>
        ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) 
            where TNotification : INotification;
    }
}