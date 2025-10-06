using System.Collections.Generic;
using System.Threading;

namespace Relay.Core;

/// <summary>
/// Interface for pipeline behaviors that can intercept and modify streaming request processing.
/// Stream pipeline behaviors are executed in order before the actual streaming handler.
/// </summary>
/// <typeparam name="TRequest">The type of the streaming request.</typeparam>
/// <typeparam name="TResponse">The type of the response items.</typeparam>
public interface IStreamPipelineBehavior<in TRequest, TResponse>
{
    /// <summary>
    /// Handles the streaming request and calls the next behavior or handler in the pipeline.
    /// </summary>
    /// <param name="request">The streaming request being processed.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An async enumerable of response items.</returns>
    IAsyncEnumerable<TResponse> HandleAsync(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
