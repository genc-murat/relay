using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core
{
    /// <summary>
    /// Fallback request dispatcher that uses reflection when no generated dispatcher is available.
    /// This provides basic functionality but with lower performance than generated dispatchers.
    /// </summary>
    public class FallbackRequestDispatcher : BaseRequestDispatcher
    {
        /// <summary>
        /// Initializes a new instance of the FallbackRequestDispatcher class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        public FallbackRequestDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <inheritdoc />
        public override ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            try
            {
                // Use the concrete request type to find the handler
                var requestType = request.GetType();
                var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
                var handler = ServiceProvider.GetService(handlerType);

                if (handler == null)
                {
                    return ValueTaskExtensions.FromException<TResponse>(CreateHandlerNotFoundException(requestType));
                }

                // Use reflection to call HandleAsync
                var method = handlerType.GetMethod("HandleAsync");
                if (method == null)
                {
                    return ValueTaskExtensions.FromException<TResponse>(CreateHandlerNotFoundException(requestType));
                }

                var result = method.Invoke(handler, new object[] { request, cancellationToken });
                return (ValueTask<TResponse>)result!;
            }
            catch (Exception ex)
            {
                return ValueTaskExtensions.FromException<TResponse>(HandleException(ex, request.GetType().Name));
            }
        }

        /// <inheritdoc />
        public override ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            try
            {
                // Use the concrete request type to find the handler
                var requestType = request.GetType();
                var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);
                var handler = ServiceProvider.GetService(handlerType);

                if (handler == null)
                {
                    return ValueTaskExtensions.FromException(CreateHandlerNotFoundException(requestType));
                }

                // Use reflection to call HandleAsync
                var method = handlerType.GetMethod("HandleAsync");
                if (method == null)
                {
                    return ValueTaskExtensions.FromException(CreateHandlerNotFoundException(requestType));
                }

                var result = method.Invoke(handler, new object[] { request, cancellationToken });
                return (ValueTask)result!;
            }
            catch (Exception ex)
            {
                return ValueTaskExtensions.FromException(HandleException(ex, request.GetType().Name));
            }
        }

        /// <inheritdoc />
        public override ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
        {
            ValidateRequest(request);
            ValidateHandlerName(handlerName);

            // Fallback dispatcher doesn't support named handlers
            return ValueTaskExtensions.FromException<TResponse>(CreateHandlerNotFoundException(request.GetType(), handlerName));
        }

        /// <inheritdoc />
        public override ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)
        {
            ValidateRequest(request);
            ValidateHandlerName(handlerName);

            // Fallback dispatcher doesn't support named handlers
            return ValueTaskExtensions.FromException(CreateHandlerNotFoundException(request.GetType(), handlerName));
        }
    }

    /// <summary>
    /// Fallback stream dispatcher that uses reflection when no generated dispatcher is available.
    /// This provides basic functionality but with lower performance than generated dispatchers.
    /// </summary>
    public class FallbackStreamDispatcher : BaseStreamDispatcher
    {
        /// <summary>
        /// Initializes a new instance of the FallbackStreamDispatcher class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        public FallbackStreamDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <inheritdoc />
        public override IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            try
            {
                // Use the concrete request type to find the handler
                var requestType = request.GetType();
                var handlerType = typeof(IStreamHandler<,>).MakeGenericType(requestType, typeof(TResponse));
                var handler = ServiceProvider.GetService(handlerType);

                if (handler == null)
                {
                    return ThrowHandlerNotFound<TResponse>(requestType);
                }

                // Use reflection to call HandleAsync
                var method = handlerType.GetMethod("HandleAsync");
                if (method == null)
                {
                    return ThrowHandlerNotFound<TResponse>(requestType);
                }

                var result = method.Invoke(handler, new object[] { request, cancellationToken });
                return (IAsyncEnumerable<TResponse>)result!;
            }
            catch (Exception ex)
            {
                return ThrowException<TResponse>(ex, request.GetType().Name);
            }
        }

        /// <inheritdoc />
        public override IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
        {
            ValidateRequest(request);
            ValidateHandlerName(handlerName);

            // Fallback dispatcher doesn't support named handlers
            return ThrowHandlerNotFound<TResponse>(request.GetType(), handlerName);
        }

        /// <summary>
        /// Creates an async enumerable that throws an exception.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response items.</typeparam>
        /// <param name="exception">The exception to throw.</param>
        /// <param name="requestType">The type of the request.</param>
        /// <returns>An async enumerable that throws when enumerated.</returns>
        private static async IAsyncEnumerable<TResponse> ThrowException<TResponse>(Exception exception, string requestType)
        {
            await Task.CompletedTask; // Make compiler happy about async
            throw HandleException(exception, requestType);
#pragma warning disable CS0162 // Unreachable code detected
            yield break; // Never reached, but required for compiler
#pragma warning restore CS0162 // Unreachable code detected
        }
    }

    /// <summary>
    /// Fallback notification dispatcher that uses reflection when no generated dispatcher is available.
    /// This provides basic functionality but with lower performance than generated dispatchers.
    /// </summary>
    public class FallbackNotificationDispatcher : BaseNotificationDispatcher
    {
        /// <summary>
        /// Initializes a new instance of the FallbackNotificationDispatcher class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        public FallbackNotificationDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <inheritdoc />
        public override async ValueTask DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
        {
            ValidateRequest(notification);

            try
            {
                var handlers = ServiceProvider.GetServices<INotificationHandler<TNotification>>();
                var handlerTasks = handlers.Select(h => h.HandleAsync(notification, cancellationToken));

                // Execute handlers in parallel by default
                await ExecuteHandlersParallel(handlerTasks, cancellationToken);
            }
            catch (Exception ex)
            {
                throw HandleException(ex, typeof(TNotification).Name);
            }
        }
    }
}