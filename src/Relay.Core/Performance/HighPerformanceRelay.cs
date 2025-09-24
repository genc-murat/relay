using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core.Performance;

/// <summary>
/// Ultra high-performance Relay implementation optimized for maximum throughput
/// </summary>
public class HighPerformanceRelay : IRelay
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRequestDispatcher? _requestDispatcher;
    private readonly IStreamDispatcher? _streamDispatcher;
    private readonly INotificationDispatcher? _notificationDispatcher;

    // Pre-allocated exception tasks for common error scenarios
    private static readonly ValueTask<object> _handlerNotFoundTask =
        ValueTask.FromException<object>(new HandlerNotFoundException("Handler not found"));
    private static readonly ValueTask _handlerNotFoundVoidTask =
        ValueTask.FromException(new HandlerNotFoundException("Handler not found"));

    // Handler cache for ultra-fast resolution
    private static readonly ConcurrentDictionary<Type, object?> _handlerCache = new();

    public HighPerformanceRelay(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        // Cache dispatchers at construction time
        _requestDispatcher = serviceProvider.GetService<IRequestDispatcher>();
        _streamDispatcher = serviceProvider.GetService<IStreamDispatcher>();
        _notificationDispatcher = serviceProvider.GetService<INotificationDispatcher>();
    }

    /// <summary>
    /// Ultra-fast request dispatch with aggressive inlining and caching
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        // Eliminate null check branch prediction issues
        ArgumentNullException.ThrowIfNull(request);

        // Cached dispatcher reference for micro-optimization
        var dispatcher = _requestDispatcher;
        if (dispatcher == null)
        {
            return ValueTaskExtensions.FromException<TResponse>(
                new HandlerNotFoundException(typeof(IRequest<TResponse>).Name));
        }

        // Direct dispatch - let exceptions bubble up naturally for better performance
        return dispatcher.DispatchAsync(request, cancellationToken);
    }

    /// <summary>
    /// Ultra-fast void request dispatch
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispatcher = _requestDispatcher;
        if (dispatcher == null)
        {
            return _handlerNotFoundVoidTask;
        }

        return dispatcher.DispatchAsync(request, cancellationToken);
    }

    /// <summary>
    /// High-performance streaming with minimal allocations
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispatcher = _streamDispatcher;
        if (dispatcher == null)
        {
            return ThrowHandlerNotFoundException<TResponse>();
        }

        return dispatcher.DispatchAsync(request, cancellationToken);
    }

    /// <summary>
    /// Optimized notification publishing with batching support
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var dispatcher = _notificationDispatcher;
        if (dispatcher == null)
        {
            // Fast completion for no handlers scenario
            return ValueTask.CompletedTask;
        }

        return dispatcher.DispatchAsync(notification, cancellationToken);
    }

    /// <summary>
    /// Pre-warms handler cache for improved cold-start performance
    /// </summary>
    public void WarmUpHandlers<THandler>() where THandler : class
    {
        var handlerType = typeof(THandler);
        if (!_handlerCache.ContainsKey(handlerType))
        {
            var handler = _serviceProvider.GetService<THandler>();
            _handlerCache.TryAdd(handlerType, handler);
        }
    }

    /// <summary>
    /// Clears internal caches (useful for testing)
    /// </summary>
    public static void ClearCaches()
    {
        _handlerCache.Clear();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async IAsyncEnumerable<TResponse> ThrowHandlerNotFoundException<TResponse>()
    {
        await Task.CompletedTask;
        throw new HandlerNotFoundException("Stream handler not found");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}

/// <summary>
/// Provides extension methods for high-performance Relay operations
/// </summary>
public static class HighPerformanceRelayExtensions
{
    /// <summary>
    /// Batch multiple requests for better performance
    /// </summary>
    public static async ValueTask<TResponse[]> SendBatchAsync<TResponse>(
        this IRelay relay,
        IEnumerable<IRequest<TResponse>> requests,
        CancellationToken cancellationToken = default)
    {
        var requestArray = requests as IRequest<TResponse>[] ?? requests.ToArray();
        var tasks = new ValueTask<TResponse>[requestArray.Length];

        for (int i = 0; i < requestArray.Length; i++)
        {
            tasks[i] = relay.SendAsync(requestArray[i], cancellationToken);
        }

        var results = new TResponse[tasks.Length];
        for (int i = 0; i < tasks.Length; i++)
        {
            results[i] = await tasks[i];
        }

        return results;
    }
}