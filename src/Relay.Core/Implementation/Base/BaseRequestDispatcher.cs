using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    /// <summary>
    /// Base implementation of request dispatcher with common functionality.
    /// Generated dispatchers will inherit from this class.
    /// </summary>
    public abstract class BaseRequestDispatcher : BaseDispatcher, IRequestDispatcher
    {
        /// <summary>
        /// Initializes a new instance of the BaseRequestDispatcher class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        protected BaseRequestDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <inheritdoc />
        public abstract ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a handler not found exception for the specified request type.
        /// </summary>
        /// <param name="requestType">The type of the request.</param>
        /// <returns>A HandlerNotFoundException.</returns>
        protected static HandlerNotFoundException CreateHandlerNotFoundException(Type requestType)
        {
            return new HandlerNotFoundException(requestType.Name);
        }

        /// <summary>
        /// Creates a handler not found exception for the specified request type and handler name.
        /// </summary>
        /// <param name="requestType">The type of the request.</param>
        /// <param name="handlerName">The name of the handler.</param>
        /// <returns>A HandlerNotFoundException.</returns>
        protected static HandlerNotFoundException CreateHandlerNotFoundException(Type requestType, string handlerName)
        {
            return new HandlerNotFoundException(requestType.Name, handlerName);
        }
    }
}