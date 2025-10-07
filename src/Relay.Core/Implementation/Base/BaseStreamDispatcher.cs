using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Implementation.Base
{
    /// <summary>
    /// Base implementation of stream dispatcher with common functionality.
    /// Generated dispatchers will inherit from this class.
    /// </summary>
    public abstract class BaseStreamDispatcher : BaseDispatcher, IStreamDispatcher
    {
        /// <summary>
        /// Initializes a new instance of the BaseStreamDispatcher class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        protected BaseStreamDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <inheritdoc />
        public abstract IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request,
            string handlerName, CancellationToken cancellationToken);

        /// <summary>
        /// Creates an async enumerable that throws a handler not found exception.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response items.</typeparam>
        /// <param name="requestType">The type of the request.</param>
        /// <returns>An async enumerable that throws when enumerated.</returns>
        protected static async IAsyncEnumerable<TResponse> ThrowHandlerNotFound<TResponse>(Type requestType)
        {
            await Task.CompletedTask; // Make compiler happy about async
            throw new HandlerNotFoundException(requestType.Name);
#pragma warning disable CS0162 // Unreachable code detected
            yield break; // Never reached, but required for compiler
#pragma warning restore CS0162 // Unreachable code detected
        }

        /// <summary>
        /// Creates an async enumerable that throws a handler not found exception with handler name.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response items.</typeparam>
        /// <param name="requestType">The type of the request.</param>
        /// <param name="handlerName">The name of the handler.</param>
        /// <returns>An async enumerable that throws when enumerated.</returns>
        protected static async IAsyncEnumerable<TResponse> ThrowHandlerNotFound<TResponse>(Type requestType, string handlerName)
        {
            await Task.CompletedTask; // Make compiler happy about async
            throw new HandlerNotFoundException(requestType.Name, handlerName);
#pragma warning disable CS0162 // Unreachable code detected
            yield break; // Never reached, but required for compiler
#pragma warning restore CS0162 // Unreachable code detected
        }
    }
}