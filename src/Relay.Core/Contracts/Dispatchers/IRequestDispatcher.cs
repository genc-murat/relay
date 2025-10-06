using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Contracts.Dispatchers;

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
