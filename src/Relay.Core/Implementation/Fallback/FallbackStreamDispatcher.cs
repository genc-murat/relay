using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Base;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Implementation.Fallback
{
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
                var requestType = request.GetType();
                var entry = FallbackDispatcherBase.StreamInvokerCache<TResponse>.GetOrCreate(requestType);
                var handler = ServiceProvider.GetService(entry.HandlerInterfaceType);
                if (handler == null)
                {
                    return ThrowHandlerNotFound<TResponse>(requestType);
                }
                return entry.Invoke(handler, request, cancellationToken);
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
}