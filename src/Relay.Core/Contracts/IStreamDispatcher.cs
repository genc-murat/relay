using System.Collections.Generic;
using System.Threading;

namespace Relay.Core;

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
