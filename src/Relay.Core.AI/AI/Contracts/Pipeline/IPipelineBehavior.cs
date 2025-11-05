using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Contracts.Pipeline;

/// <summary>
/// Delegate for the next handler in the request pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <returns>A ValueTask containing the response.</returns>
public delegate ValueTask<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Delegate for the next handler in the streaming pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of the response items.</typeparam>
/// <returns>An async enumerable of response items.</returns>
public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<TResponse>();

/// <summary>
/// Interface for pipeline behaviors that can intercept and modify request processing.
/// Pipeline behaviors are executed in order before the actual handler.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
{
    /// <summary>
    /// Handles the request and calls the next behavior or handler in the pipeline.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ValueTask containing the response.</returns>
    ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}