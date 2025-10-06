using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Pipeline;

namespace Relay.Core.Contracts.Core;

/// <summary>
/// Interface for system modules that execute before user-defined pipeline behaviors.
/// System modules have priority execution and are typically used for framework-level concerns
/// like logging, telemetry, and error handling.
/// </summary>
public interface ISystemModule
{
    /// <summary>
    /// Gets the execution order of this system module.
    /// Lower values execute earlier in the pipeline.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Executes the system module logic for request processing.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ValueTask containing the response.</returns>
    ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);

    /// <summary>
    /// Executes the system module logic for streaming request processing.
    /// </summary>
    /// <typeparam name="TRequest">The type of the streaming request.</typeparam>
    /// <typeparam name="TResponse">The type of the response items.</typeparam>
    /// <param name="request">The streaming request being processed.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An async enumerable of response items.</returns>
    IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}