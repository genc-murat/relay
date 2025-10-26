using Relay.Core.Contracts.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Implementation.Fallback;

/// <summary>
/// Base class for fallback dispatchers that provides common expression tree caching functionality.
/// This unifies the reflection-based handler invocation patterns used across fallback dispatchers.
/// </summary>
public static class FallbackDispatcherBase
{
    /// <summary>
    /// Cache for expression tree invokers that return ValueTask&lt;TResponse&gt;.
    /// </summary>
    public static class ResponseInvokerCache<TResponse>
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

    /// <summary>
    /// Cache for expression tree invokers that return ValueTask (void).
    /// </summary>
    public static class VoidInvokerCache
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
    /// Cache for expression tree invokers that return IAsyncEnumerable&lt;TResponse&gt;.
    /// </summary>
    public static class StreamInvokerCache<TResponse>
    {
        public sealed class Entry
        {
            public required Type HandlerInterfaceType { get; init; }
            public required Func<object, object, CancellationToken, IAsyncEnumerable<TResponse>> Invoke { get; init; }
        }

        private static readonly ConcurrentDictionary<Type, Entry> Cache = new();

        public static Entry GetOrCreate(Type requestType)
        {
            return Cache.GetOrAdd(requestType, static rt =>
            {
                var handlerInterface = typeof(IStreamHandler<,>).MakeGenericType(rt, typeof(TResponse));

                var handlerParam = Expression.Parameter(typeof(object), "handler");
                var requestParam = Expression.Parameter(typeof(object), "request");
                var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

                var castedHandler = Expression.Convert(handlerParam, handlerInterface);
                var castedRequest = Expression.Convert(requestParam, rt);

                var method = handlerInterface.GetMethod("HandleAsync");
                if (method == null)
                    throw new MissingMethodException(handlerInterface.FullName, "HandleAsync");

                var call = Expression.Call(castedHandler, method, castedRequest, ctParam);
                var lambda = Expression.Lambda<Func<object, object, CancellationToken, IAsyncEnumerable<TResponse>>>(
                    call, handlerParam, requestParam, ctParam);

                return new Entry
                {
                    HandlerInterfaceType = handlerInterface,
                    Invoke = lambda.Compile()
                };
            });
        }
    }

    /// <summary>
    /// Executes a handler with response using the cached invoker.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<TResponse> ExecuteWithCache<TResponse>(
        object request,
        IServiceProvider serviceProvider,
        Func<Type, ResponseInvokerCache<TResponse>.Entry> cacheFactory)
    {
        try
        {
            var requestType = request.GetType();
            var entry = ResponseInvokerCache<TResponse>.Cache.GetOrAdd(requestType, cacheFactory);

            // Performance optimization: Use hot path for handler resolution
            var handler = serviceProvider.GetService(entry.HandlerInterfaceType);
            if (handler == null)
                return ValueTaskExtensions.FromException<TResponse>(CreateHandlerNotFoundException(requestType));

            // Direct invocation for better performance
            return entry.Invoke(handler, request, CancellationToken.None);
        }
        catch (Exception ex)
        {
            return ValueTaskExtensions.FromException<TResponse>(HandleException(ex, request.GetType().Name));
        }
    }

    /// <summary>
    /// Executes a void handler using the cached invoker.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask ExecuteVoidWithCache(
        object request,
        IServiceProvider serviceProvider,
        Func<Type, VoidInvokerCache.Entry> cacheFactory)
    {
        try
        {
            var requestType = request.GetType();
            var entry = VoidInvokerCache.Cache.GetOrAdd(requestType, cacheFactory);
            var handler = serviceProvider.GetService(entry.HandlerInterfaceType);
            if (handler == null)
                return ValueTaskExtensions.FromException(CreateHandlerNotFoundException(requestType));
            return entry.Invoke(handler, request, CancellationToken.None);
        }
        catch (Exception ex)
        {
            return ValueTaskExtensions.FromException(HandleException(ex, request.GetType().Name));
        }
    }

    /// <summary>
    /// Creates a handler not found exception for the specified request type.
    /// </summary>
    public static HandlerNotFoundException CreateHandlerNotFoundException(Type requestType)
    {
        return new HandlerNotFoundException(requestType.Name);
    }

    /// <summary>
    /// Creates a handler not found exception for the specified request type and handler name.
    /// </summary>
    public static HandlerNotFoundException CreateHandlerNotFoundException(Type requestType, string handlerName)
    {
        return new HandlerNotFoundException(requestType.Name, handlerName);
    }

    /// <summary>
    /// Handles exceptions that occur during handler execution.
    /// </summary>
    public static RelayException HandleException(Exception exception, string requestType, string? handlerName = null)
    {
        if (exception is RelayException relayException)
        {
            return relayException;
        }

        return new RelayException(requestType, handlerName,
            $"An error occurred while processing request of type '{requestType}': {exception.Message}", exception);
    }
}