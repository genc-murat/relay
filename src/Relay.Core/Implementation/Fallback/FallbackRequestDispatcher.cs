using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Base;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Implementation.Fallback
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            try
            {
                var requestType = request.GetType();
                var entry = FallbackDispatcherBase.ResponseInvokerCache<TResponse>.Cache.GetOrAdd(requestType, FallbackDispatcherBase.ResponseInvokerCache<TResponse>.Create);

                // Performance optimization: Use hot path for handler resolution
                var handler = ServiceProvider.GetService(entry.HandlerInterfaceType);
                if (handler == null)
                    return ValueTaskExtensions.FromException<TResponse>(CreateHandlerNotFoundException(requestType));

                // Direct invocation for better performance
                return entry.Invoke(handler, request, cancellationToken);
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
                var requestType = request.GetType();
                var entry = FallbackDispatcherBase.VoidInvokerCache.Cache.GetOrAdd(requestType, FallbackDispatcherBase.VoidInvokerCache.Create);
                var handler = ServiceProvider.GetService(entry.HandlerInterfaceType);
                if (handler == null)
                    return ValueTaskExtensions.FromException(CreateHandlerNotFoundException(requestType));
                return entry.Invoke(handler, request, cancellationToken);
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
}