using System;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Contracts.Dispatchers
{
    /// <summary>
    /// Base class for request dispatchers providing common functionality.
    /// Used by source-generated optimized dispatchers.
    /// </summary>
    public abstract class BaseRequestDispatcher : IRequestDispatcher
    {
        protected IServiceProvider ServiceProvider { get; }

        protected BaseRequestDispatcher(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Dispatches a request with a response.
        /// </summary>
        public abstract ValueTask<TResponse> DispatchAsync<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Dispatches a request without a response.
        /// </summary>
        public abstract ValueTask DispatchAsync(
            IRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Dispatches a named request with a response.
        /// </summary>
        public abstract ValueTask<TResponse> DispatchAsync<TResponse>(
            IRequest<TResponse> request,
            string handlerName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Dispatches a named request without a response.
        /// </summary>
        public abstract ValueTask DispatchAsync(
            IRequest request,
            string handlerName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that a request is not null.
        /// </summary>
        protected void ValidateRequest<T>(T request) where T : class
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
        }

        /// <summary>
        /// Validates that a handler name is not null or empty.
        /// </summary>
        protected void ValidateHandlerName(string handlerName)
        {
            if (string.IsNullOrWhiteSpace(handlerName))
                throw new ArgumentException("Handler name cannot be null or empty.", nameof(handlerName));
        }

        /// <summary>
        /// Creates a handler not found exception.
        /// </summary>
        protected Exception CreateHandlerNotFoundException(Type requestType, string? handlerName = null)
        {
            var message = handlerName == null
                ? $"No handler found for request type '{requestType.Name}'"
                : $"No handler named '{handlerName}' found for request type '{requestType.Name}'";

            return new HandlerNotFoundException(message);
        }
    }
}
