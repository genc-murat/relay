using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core
{
    /// <summary>
    /// Default implementation of the IRelay interface.
    /// Uses generated dispatchers for high-performance request routing.
    /// </summary>
    public class RelayImplementation : IRelay
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ServiceFactory _serviceFactory;
        private readonly IRequestDispatcher? _requestDispatcher;
        private readonly IStreamDispatcher? _streamDispatcher;
        private readonly INotificationDispatcher? _notificationDispatcher;

        // Performance optimization: Create exception with proper type information
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ValueTask<T> CreateHandlerNotFoundTask<T>() =>
            ValueTask.FromException<T>(new HandlerNotFoundException(typeof(T).Name));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ValueTask CreateHandlerNotFoundVoidTask(Type requestType) =>
            ValueTask.FromException(new HandlerNotFoundException(requestType.Name));

        /// <summary>
        /// Initializes a new instance of the RelayImplementation class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        public RelayImplementation(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            // Create ServiceFactory delegate from service provider
            _serviceFactory = serviceProvider.GetService;

            // Try to resolve generated dispatchers - they may not be available if no handlers are registered
            _requestDispatcher = _serviceProvider.GetService<IRequestDispatcher>();
            _streamDispatcher = _serviceProvider.GetService<IStreamDispatcher>();
            _notificationDispatcher = _serviceProvider.GetService<INotificationDispatcher>();
        }

        /// <summary>
        /// Initializes a new instance of the RelayImplementation class with an explicit ServiceFactory.
        /// This constructor is useful for advanced scenarios or testing.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        /// <param name="serviceFactory">The service factory for flexible service resolution.</param>
        public RelayImplementation(IServiceProvider serviceProvider, ServiceFactory serviceFactory)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));

            // Try to resolve generated dispatchers - they may not be available if no handlers are registered
            _requestDispatcher = _serviceProvider.GetService<IRequestDispatcher>();
            _streamDispatcher = _serviceProvider.GetService<IStreamDispatcher>();
            _notificationDispatcher = _serviceProvider.GetService<INotificationDispatcher>();
        }

        /// <summary>
        /// Gets the ServiceFactory for this Relay instance.
        /// Allows external code to use the same service resolution mechanism.
        /// </summary>
        public ServiceFactory ServiceFactory => _serviceFactory;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Performance optimization: Fast-path for common case
            var dispatcher = _requestDispatcher;
            if (dispatcher == null)
            {
                return ValueTaskExtensions.FromException<TResponse>(
                    new HandlerNotFoundException(typeof(IRequest<TResponse>).Name));
            }

            try
            {
                return dispatcher.DispatchAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                return ValueTaskExtensions.FromException<TResponse>(ex);
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Performance optimization: Fast-path for common case
            var dispatcher = _requestDispatcher;
            if (dispatcher == null)
            {
                return CreateHandlerNotFoundVoidTask(typeof(IRequest));
            }

            try
            {
                return dispatcher.DispatchAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                return ValueTaskExtensions.FromException(ex);
            }
        }

        /// <inheritdoc />
        public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (_streamDispatcher == null)
            {
                return ThrowHandlerNotFoundAsyncEnumerable<TResponse>(typeof(IStreamRequest<TResponse>).Name);
            }

            try
            {
                return _streamDispatcher.DispatchAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                return ThrowExceptionAsyncEnumerable<TResponse>(ex);
            }
        }

        /// <inheritdoc />
        public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            // If no notification dispatcher is available, complete successfully (no handlers registered)
            if (_notificationDispatcher == null)
            {
                return ValueTaskExtensions.CompletedTask;
            }

            try
            {
                return _notificationDispatcher.DispatchAsync(notification, cancellationToken);
            }
            catch (Exception ex)
            {
                return ValueTaskExtensions.FromException(ex);
            }
        }

        /// <summary>
        /// Helper method to create an async enumerable that throws a HandlerNotFoundException.
        /// </summary>
        private static async IAsyncEnumerable<T> ThrowHandlerNotFoundAsyncEnumerable<T>(string requestType)
        {
            await Task.CompletedTask; // Make compiler happy about async
            throw new HandlerNotFoundException(requestType);
#pragma warning disable CS0162 // Unreachable code detected
            yield break; // Never reached, but required for compiler
#pragma warning restore CS0162 // Unreachable code detected
        }

        /// <summary>
        /// Helper method to create an async enumerable that throws an exception.
        /// </summary>
        private static async IAsyncEnumerable<T> ThrowExceptionAsyncEnumerable<T>(Exception exception)
        {
            await Task.CompletedTask; // Make compiler happy about async
            throw exception;
#pragma warning disable CS0162 // Unreachable code detected
            yield break; // Never reached, but required for compiler
#pragma warning restore CS0162 // Unreachable code detected
        }
    }

    /// <summary>
    /// Named relay implementation that supports handler name resolution.
    /// </summary>
    public class NamedRelay
    {
        private readonly RelayImplementation _relay;
        private readonly IRequestDispatcher? _requestDispatcher;
        private readonly IStreamDispatcher? _streamDispatcher;

        /// <summary>
        /// Initializes a new instance of the NamedRelay class.
        /// </summary>
        /// <param name="relay">The underlying relay implementation.</param>
        /// <param name="serviceProvider">The service provider for dispatcher resolution.</param>
        public NamedRelay(RelayImplementation relay, IServiceProvider serviceProvider)
        {
            _relay = relay ?? throw new ArgumentNullException(nameof(relay));
            _requestDispatcher = serviceProvider?.GetService<IRequestDispatcher>();
            _streamDispatcher = serviceProvider?.GetService<IStreamDispatcher>();
        }

        /// <summary>
        /// Sends a request to a named handler and returns a response asynchronously.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The request to send.</param>
        /// <param name="handlerName">The name of the handler to use.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask containing the response.</returns>
        public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(handlerName))
                throw new ArgumentException("Handler name cannot be null or empty.", nameof(handlerName));

            if (_requestDispatcher == null)
            {
                return ValueTaskExtensions.FromException<TResponse>(
                    new HandlerNotFoundException(typeof(IRequest<TResponse>).Name, handlerName));
            }

            try
            {
                return _requestDispatcher.DispatchAsync(request, handlerName, cancellationToken);
            }
            catch (Exception ex)
            {
                return ValueTaskExtensions.FromException<TResponse>(ex);
            }
        }

        /// <summary>
        /// Sends a request to a named handler without expecting a response.
        /// </summary>
        /// <param name="request">The request to send.</param>
        /// <param name="handlerName">The name of the handler to use.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the operation.</returns>
        public ValueTask SendAsync(IRequest request, string handlerName, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(handlerName))
                throw new ArgumentException("Handler name cannot be null or empty.", nameof(handlerName));

            if (_requestDispatcher == null)
            {
                return ValueTaskExtensions.FromException(
                    new HandlerNotFoundException(typeof(IRequest).Name, handlerName));
            }

            try
            {
                return _requestDispatcher.DispatchAsync(request, handlerName, cancellationToken);
            }
            catch (Exception ex)
            {
                return ValueTaskExtensions.FromException(ex);
            }
        }

        /// <summary>
        /// Sends a streaming request to a named handler and returns an async enumerable of responses.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response items.</typeparam>
        /// <param name="request">The streaming request to send.</param>
        /// <param name="handlerName">The name of the handler to use.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>An async enumerable of response items.</returns>
        public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(handlerName))
                throw new ArgumentException("Handler name cannot be null or empty.", nameof(handlerName));

            if (_streamDispatcher == null)
            {
                return ThrowHandlerNotFoundAsyncEnumerable<TResponse>(typeof(IStreamRequest<TResponse>).Name, handlerName);
            }

            try
            {
                return _streamDispatcher.DispatchAsync(request, handlerName, cancellationToken);
            }
            catch (Exception ex)
            {
                return ThrowExceptionAsyncEnumerable<TResponse>(ex);
            }
        }

        /// <summary>
        /// Helper method to create an async enumerable that throws a HandlerNotFoundException.
        /// </summary>
        private static async IAsyncEnumerable<T> ThrowHandlerNotFoundAsyncEnumerable<T>(string requestType, string handlerName)
        {
            await Task.CompletedTask; // Make compiler happy about async
            throw new HandlerNotFoundException(requestType, handlerName);
#pragma warning disable CS0162 // Unreachable code detected
            yield break; // Never reached, but required for compiler
#pragma warning restore CS0162 // Unreachable code detected
        }

        /// <summary>
        /// Helper method to create an async enumerable that throws an exception.
        /// </summary>
        private static async IAsyncEnumerable<T> ThrowExceptionAsyncEnumerable<T>(Exception exception)
        {
            await Task.CompletedTask; // Make compiler happy about async
            throw exception;
#pragma warning disable CS0162 // Unreachable code detected
            yield break; // Never reached, but required for compiler
#pragma warning restore CS0162 // Unreachable code detected
        }
    }
}