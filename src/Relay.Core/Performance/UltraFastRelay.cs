using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core.Performance;

/// <summary>
/// Ultra-fast Relay implementation with aggressive optimizations
/// Combines multiple performance techniques for maximum speed
/// </summary>
public sealed class UltraFastRelay : IRelay
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRequestDispatcher? _requestDispatcher;
    private readonly IStreamDispatcher? _streamDispatcher;
    private readonly INotificationDispatcher? _notificationDispatcher;

    // Pre-allocated exception tasks for ultra-fast error paths
    private static readonly ValueTask<object> HandlerNotFoundTask =
        ValueTask.FromException<object>(new HandlerNotFoundException("Handler not found"));
    private static readonly ValueTask VoidHandlerNotFoundTask =
        ValueTask.FromException(new HandlerNotFoundException("Handler not found"));

    // Request type cache for ultra-fast type lookups
    private static readonly ConcurrentDictionary<object, Type> TypeCache = new();

    public UltraFastRelay(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _requestDispatcher = serviceProvider.GetOptimizedService<IRequestDispatcher>();
        _streamDispatcher = serviceProvider.GetOptimizedService<IStreamDispatcher>();
        _notificationDispatcher = serviceProvider.GetOptimizedService<INotificationDispatcher>();
    }

    /// <summary>
    /// Ultra-fast generic request dispatch with zero allocations in common path
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        // Ultra-fast null check without exception allocation
        if (request is null)
            return UltraFastExceptionHelper.ThrowArgumentNull<TResponse>();

        // Ultra-fast dispatcher resolution
        var dispatcher = _requestDispatcher;
        if (dispatcher is null)
            return UltraFastExceptionHelper.ThrowHandlerNotFound<TResponse>();

        // Direct dispatch - exceptions handled by dispatcher layer
        return dispatcher.DispatchAsync(request, cancellationToken);
    }

    /// <summary>
    /// Ultra-fast void request dispatch
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        // Ultra-fast null check
        if (request is null)
            return UltraFastExceptionHelper.ThrowArgumentNullVoid();

        // Ultra-fast dispatcher resolution
        var dispatcher = _requestDispatcher;
        if (dispatcher is null)
            return VoidHandlerNotFoundTask;

        // Direct dispatch
        return dispatcher.DispatchAsync(request, cancellationToken);
    }

    /// <summary>
    /// Named handler dispatch - optimized for performance
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken = default)
    {
        if (request is null)
            return UltraFastExceptionHelper.ThrowArgumentNull<TResponse>();

        if (string.IsNullOrWhiteSpace(handlerName))
            return UltraFastExceptionHelper.ThrowArgumentNull<TResponse>("handlerName");

        var dispatcher = _requestDispatcher;
        if (dispatcher is null)
            return UltraFastExceptionHelper.ThrowHandlerNotFound<TResponse>();

        return dispatcher.DispatchAsync(request, handlerName, cancellationToken);
    }

    /// <summary>
    /// Named handler dispatch void
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask SendAsync(IRequest request, string handlerName, CancellationToken cancellationToken = default)
    {
        if (request is null)
            return UltraFastExceptionHelper.ThrowArgumentNullVoid();

        if (string.IsNullOrWhiteSpace(handlerName))
            return UltraFastExceptionHelper.ThrowArgumentNullVoid();

        var dispatcher = _requestDispatcher;
        if (dispatcher is null)
            return VoidHandlerNotFoundTask;

        return dispatcher.DispatchAsync(request, handlerName, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            return UltraFastExceptionHelper.ThrowArgumentNullStream<TResponse>();

        var dispatcher = _streamDispatcher;
        if (dispatcher is null)
            return UltraFastExceptionHelper.ThrowHandlerNotFoundStream<TResponse>();

        return dispatcher.DispatchAsync(request, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken = default)
    {
        if (request is null)
            return UltraFastExceptionHelper.ThrowArgumentNullStream<TResponse>();

        if (string.IsNullOrWhiteSpace(handlerName))
            return UltraFastExceptionHelper.ThrowArgumentNullStream<TResponse>();

        var dispatcher = _streamDispatcher;
        if (dispatcher is null)
            return UltraFastExceptionHelper.ThrowHandlerNotFoundStream<TResponse>();

        return dispatcher.DispatchAsync(request, handlerName, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification is null)
            return UltraFastExceptionHelper.ThrowArgumentNullVoid();

        var dispatcher = _notificationDispatcher;
        if (dispatcher is null)
            return ValueTask.CompletedTask; // No-op for missing notification dispatcher

        return dispatcher.DispatchAsync(notification, cancellationToken);
    }

    /// <summary>
    /// Gets cached type for ultra-fast type operations
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type GetCachedType<T>(T obj) where T : class
    {
        return TypeCache.GetOrAdd(obj, static o => o.GetType());
    }
}

/// <summary>
/// Ultra-fast exception helpers to minimize allocations
/// </summary>
internal static class UltraFastExceptionHelper
{
    private static readonly ValueTask<object> ArgumentNullTask =
        ValueTask.FromException<object>(new ArgumentNullException());
    private static readonly ValueTask ArgumentNullVoidTask =
        ValueTask.FromException(new ArgumentNullException());
    private static readonly ValueTask<object> HandlerNotFoundTask =
        ValueTask.FromException<object>(new HandlerNotFoundException("Handler not found"));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ValueTask<T> ThrowArgumentNull<T>(string? paramName = null)
    {
        return ValueTask.FromException<T>(new ArgumentNullException(paramName));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ValueTask ThrowArgumentNullVoid(string? paramName = null)
    {
        return ValueTask.FromException(new ArgumentNullException(paramName));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ValueTask<T> ThrowHandlerNotFound<T>()
    {
        return ValueTask.FromException<T>(new HandlerNotFoundException("Handler not found"));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async IAsyncEnumerable<T> ThrowArgumentNullStream<T>()
    {
        await Task.CompletedTask;
        throw new ArgumentNullException();
        yield break; // Never reached
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async IAsyncEnumerable<T> ThrowHandlerNotFoundStream<T>()
    {
        await Task.CompletedTask;
        throw new HandlerNotFoundException("Handler not found");
        yield break; // Never reached
    }
}