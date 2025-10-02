using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core.Performance;

/// <summary>
/// Struct-based optimized relay that minimizes heap allocations
/// Uses unsafe code and memory tricks for maximum performance
/// </summary>
public readonly struct StructOptimizedRelay
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRequestDispatcher? _requestDispatcher;
    private readonly IStreamDispatcher? _streamDispatcher;
    private readonly INotificationDispatcher? _notificationDispatcher;

    public StructOptimizedRelay(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _requestDispatcher = serviceProvider.GetService<IRequestDispatcher>();
        _streamDispatcher = serviceProvider.GetService<IStreamDispatcher>();
        _notificationDispatcher = serviceProvider.GetService<INotificationDispatcher>();
    }

    /// <summary>
    /// Ultra-fast request dispatch using struct-based approach
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        // Null check with branch prediction optimization
        if (Unsafe.IsNullRef(ref Unsafe.AsRef(in request)))
            return ValueTask.FromException<TResponse>(new ArgumentNullException(nameof(request)));

        var dispatcher = _requestDispatcher;
        if (Unsafe.IsNullRef(ref Unsafe.AsRef(in dispatcher)))
        {
            return CreateHandlerNotFoundTask<TResponse>();
        }

        // Direct call without try-catch for maximum performance
        return dispatcher!.DispatchAsync(request, cancellationToken);
    }

    /// <summary>
    /// Ultra-fast void request dispatch
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        if (Unsafe.IsNullRef(ref Unsafe.AsRef(in request)))
            return ValueTask.FromException(new ArgumentNullException(nameof(request)));

        var dispatcher = _requestDispatcher;
        if (Unsafe.IsNullRef(ref Unsafe.AsRef(in dispatcher)))
        {
            return CreateHandlerNotFoundVoidTask();
        }

        return dispatcher!.DispatchAsync(request, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ValueTask<TResponse> CreateHandlerNotFoundTask<TResponse>()
    {
        return ValueTask.FromException<TResponse>(new HandlerNotFoundException(typeof(TResponse).Name));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ValueTask CreateHandlerNotFoundVoidTask()
    {
        return ValueTask.FromException(new HandlerNotFoundException("Void handler"));
    }

    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (Unsafe.IsNullRef(ref Unsafe.AsRef(in request)))
            return ThrowArgumentNullAsyncEnumerable<TResponse>();

        var dispatcher = _streamDispatcher;
        if (Unsafe.IsNullRef(ref Unsafe.AsRef(in dispatcher)))
        {
            return ThrowHandlerNotFoundAsyncEnumerable<TResponse>();
        }

        return dispatcher!.DispatchAsync(request, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (Unsafe.IsNullRef(ref Unsafe.AsRef(in notification)))
            return ValueTask.FromException(new ArgumentNullException(nameof(notification)));

        var dispatcher = _notificationDispatcher;
        if (Unsafe.IsNullRef(ref Unsafe.AsRef(in dispatcher)))
        {
            return ValueTask.CompletedTask;
        }

        return dispatcher!.DispatchAsync(notification, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async IAsyncEnumerable<TResponse> ThrowArgumentNullAsyncEnumerable<TResponse>()
    {
        await Task.CompletedTask;
        throw new ArgumentNullException("request");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async IAsyncEnumerable<TResponse> ThrowHandlerNotFoundAsyncEnumerable<TResponse>()
    {
        await Task.CompletedTask;
        throw new HandlerNotFoundException(typeof(TResponse).Name);
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}

/// <summary>
/// Memory-optimized request context that reduces allocations
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct OptimizedRequestContext<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public readonly TRequest Request;
    public readonly CancellationToken CancellationToken;
    public readonly long Timestamp;

    public OptimizedRequestContext(TRequest request, CancellationToken cancellationToken)
    {
        Request = request;
        CancellationToken = cancellationToken;
        Timestamp = Environment.TickCount64;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetElapsedTicks() => Environment.TickCount64 - Timestamp;
}