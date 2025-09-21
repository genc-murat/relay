using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core
{
    /// <summary>
    /// Base class for generated dispatchers providing common functionality.
    /// </summary>
    public abstract class BaseDispatcher
    {
        /// <summary>
        /// Gets the service provider for dependency resolution.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Initializes a new instance of the BaseDispatcher class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        protected BaseDispatcher(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Resolves a service from the service provider.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Tries to resolve a service from the service provider.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service instance, or null if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T? GetServiceOrNull<T>() where T : class
        {
            return ServiceProvider.GetService<T>();
        }

        /// <summary>
        /// Creates a scoped service provider for handler execution.
        /// </summary>
        /// <returns>A scoped service provider.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected IServiceScope CreateScope()
        {
            return ServiceProvider.CreateScope();
        }

        /// <summary>
        /// Handles exceptions that occur during handler execution.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="requestType">The type of the request being processed.</param>
        /// <param name="handlerName">The name of the handler, if applicable.</param>
        /// <returns>A RelayException wrapping the original exception.</returns>
        protected static RelayException HandleException(Exception exception, string requestType, string? handlerName = null)
        {
            if (exception is RelayException relayException)
            {
                return relayException;
            }

            return new RelayException(requestType, handlerName, 
                $"An error occurred while processing request of type '{requestType}'", exception);
        }

        /// <summary>
        /// Validates that a request is not null.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <param name="parameterName">The name of the parameter for exception reporting.</param>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ValidateRequest(object? request, string parameterName = "request")
        {
            if (request == null)
                throw new ArgumentNullException(parameterName);
        }

        /// <summary>
        /// Validates that a handler name is not null or empty.
        /// </summary>
        /// <param name="handlerName">The handler name to validate.</param>
        /// <param name="parameterName">The name of the parameter for exception reporting.</param>
        /// <exception cref="ArgumentException">Thrown when the handler name is null or empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ValidateHandlerName(string? handlerName, string parameterName = "handlerName")
        {
            if (string.IsNullOrEmpty(handlerName))
                throw new ArgumentException("Handler name cannot be null or empty.", parameterName);
        }
    }

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
        public abstract IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken);

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

    /// <summary>
    /// Base implementation of notification dispatcher with common functionality.
    /// Generated dispatchers will inherit from this class.
    /// </summary>
    public abstract class BaseNotificationDispatcher : BaseDispatcher, INotificationDispatcher
    {
        /// <summary>
        /// Initializes a new instance of the BaseNotificationDispatcher class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        protected BaseNotificationDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <inheritdoc />
        public abstract ValueTask DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken) 
            where TNotification : INotification;

        /// <summary>
        /// Executes multiple notification handlers in parallel.
        /// </summary>
        /// <param name="handlers">The handlers to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A ValueTask representing the completion of all handlers.</returns>
        protected static async ValueTask ExecuteHandlersParallel(IEnumerable<ValueTask> handlers, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();
            
            foreach (var handler in handlers)
            {
                tasks.Add(handler.AsTask());
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// Executes multiple notification handlers sequentially.
        /// </summary>
        /// <param name="handlers">The handlers to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A ValueTask representing the completion of all handlers.</returns>
        protected static async ValueTask ExecuteHandlersSequential(IEnumerable<ValueTask> handlers, CancellationToken cancellationToken)
        {
            foreach (var handler in handlers)
            {
                await handler;
            }
        }
    }
}