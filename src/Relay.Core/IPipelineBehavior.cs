using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
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
}