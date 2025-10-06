using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    /// <summary>
    /// Fallback request dispatcher that uses reflection when no generated dispatcher is available.
    /// This provides basic functionality but with lower performance than generated dispatchers.
    /// </summary>
    public class FallbackRequestDispatcher : BaseRequestDispatcher
    {
        private static class ResponseInvokerCache<TResponse>
        {
            public sealed class Entry
            {
                public required Type HandlerInterfaceType { get; init; }
                public required Func<object, object, CancellationToken, ValueTask<TResponse>> Invoke { get; init; }
            }

            public static readonly ConcurrentDictionary<Type, Entry> Cache = new();

            public static Entry Create(Type requestType)
            {
                var handlerInterface = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

                var handlerParam = Expression.Parameter(typeof(object), "handler");
                var requestParam = Expression.Parameter(typeof(object), "request");
                var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

                var castedHandler = Expression.Convert(handlerParam, handlerInterface);
                var castedRequest = Expression.Convert(requestParam, requestType);

                var method = handlerInterface.GetMethod("HandleAsync");
                if (method == null)
                    throw new MissingMethodException(handlerInterface.FullName, "HandleAsync");

                var call = Expression.Call(castedHandler, method, castedRequest, ctParam);
                var lambda = Expression.Lambda<Func<object, object, CancellationToken, ValueTask<TResponse>>>(call, handlerParam, requestParam, ctParam);
                return new Entry { HandlerInterfaceType = handlerInterface, Invoke = lambda.Compile() };
            }
        }

        private static class VoidInvokerCache
        {
            public sealed class Entry
            {
                public required Type HandlerInterfaceType { get; init; }
                public required Func<object, object, CancellationToken, ValueTask> Invoke { get; init; }
            }

            public static readonly ConcurrentDictionary<Type, Entry> Cache = new();

            public static Entry Create(Type requestType)
            {
                var handlerInterface = typeof(IRequestHandler<>).MakeGenericType(requestType);

                var handlerParam = Expression.Parameter(typeof(object), "handler");
                var requestParam = Expression.Parameter(typeof(object), "request");
                var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

                var castedHandler = Expression.Convert(handlerParam, handlerInterface);
                var castedRequest = Expression.Convert(requestParam, requestType);

                var method = handlerInterface.GetMethod("HandleAsync");
                if (method == null)
                    throw new MissingMethodException(handlerInterface.FullName, "HandleAsync");

                var call = Expression.Call(castedHandler, method, castedRequest, ctParam);
                var lambda = Expression.Lambda<Func<object, object, CancellationToken, ValueTask>>(call, handlerParam, requestParam, ctParam);
                return new Entry { HandlerInterfaceType = handlerInterface, Invoke = lambda.Compile() };
            }
        }
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
                var entry = ResponseInvokerCache<TResponse>.Cache.GetOrAdd(requestType, ResponseInvokerCache<TResponse>.Create);

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
                var entry = VoidInvokerCache.Cache.GetOrAdd(requestType, VoidInvokerCache.Create);
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