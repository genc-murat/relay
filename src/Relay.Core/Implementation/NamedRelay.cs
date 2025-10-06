using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;

namespace Relay.Core
{
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
                    new HandlerNotFoundException(request.GetType().Name, handlerName));
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
                    new HandlerNotFoundException(request.GetType().Name, handlerName));
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