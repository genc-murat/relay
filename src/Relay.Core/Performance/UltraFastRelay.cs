using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
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

    // Optimized type cache using FrozenDictionary for better read performance
    private static readonly Lazy<FrozenDictionary<Type, TypeInfo>> TypeCache = new(CreateTypeCache);
    private static readonly ConcurrentDictionary<object, Type> RuntimeTypeCache = new();

    public UltraFastRelay(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _requestDispatcher = serviceProvider.GetService<IRequestDispatcher>();
        _streamDispatcher = serviceProvider.GetService<IStreamDispatcher>();
        _notificationDispatcher = serviceProvider.GetService<INotificationDispatcher>();
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
    /// Gets cached type for ultra-fast type operations using optimized FrozenDictionary
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type GetCachedType<T>(T obj) where T : class
    {
        var type = obj.GetType();

        // First check the frozen cache for known types (ultra-fast O(1) lookup)
        if (TypeCache.Value.TryGetValue(type, out var typeInfo))
        {
            return typeInfo.Type;
        }

        // Fallback to runtime cache for dynamic types
        return RuntimeTypeCache.GetOrAdd(obj, static o => o.GetType());
    }

    /// <summary>
    /// Creates optimized type cache with commonly used types pre-populated
    /// </summary>
    private static FrozenDictionary<Type, TypeInfo> CreateTypeCache()
    {
        var builder = new Dictionary<Type, TypeInfo>();

        // Pre-populate with common request/response types
        AddCommonTypes(builder);

        return builder.ToFrozenDictionary();
    }

    private static void AddCommonTypes(Dictionary<Type, TypeInfo> builder)
    {
        // Common system types
        builder[typeof(string)] = new TypeInfo(typeof(string), "System.String");
        builder[typeof(int)] = new TypeInfo(typeof(int), "System.Int32");
        builder[typeof(bool)] = new TypeInfo(typeof(bool), "System.Boolean");
        builder[typeof(DateTime)] = new TypeInfo(typeof(DateTime), "System.DateTime");
        builder[typeof(Guid)] = new TypeInfo(typeof(Guid), "System.Guid");

        // Common generic types that are frequently used
        builder[typeof(ValueTask)] = new TypeInfo(typeof(ValueTask), "System.Threading.Tasks.ValueTask");
        builder[typeof(Task)] = new TypeInfo(typeof(Task), "System.Threading.Tasks.Task");
        builder[typeof(CancellationToken)] = new TypeInfo(typeof(CancellationToken), "System.Threading.CancellationToken");

        // Add more types based on usage patterns
    }

    /// <summary>
    /// Optimized type information for better cache performance
    /// </summary>
    public readonly record struct TypeInfo(Type Type, string Name);

    /// <summary>
    /// Gets optimized type info for the given type
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypeInfo GetOptimizedTypeInfo<T>()
    {
        var type = typeof(T);
        return TypeCache.Value.TryGetValue(type, out var info) ? info : new TypeInfo(type, type.Name);
    }
}

/// <summary>
/// Ultra-fast exception helpers with pre-allocated exceptions to minimize allocations
/// </summary>
internal static class UltraFastExceptionHelper
{
    // Pre-allocated exception instances for maximum performance
    private static readonly ArgumentNullException PreallocatedArgumentNull = new("request");
    private static readonly ArgumentNullException PreallocatedHandlerNameNull = new("handlerName");
    private static readonly HandlerNotFoundException PreallocatedHandlerNotFound = new("Handler not found");
    private static readonly InvalidOperationException PreallocatedInvalidOperation = new("Operation not supported");

    // Pre-allocated ValueTasks with exceptions
    private static readonly ValueTask<object> ArgumentNullTask = ValueTask.FromException<object>(PreallocatedArgumentNull);
    private static readonly ValueTask ArgumentNullVoidTask = ValueTask.FromException(PreallocatedArgumentNull);
    private static readonly ValueTask<object> HandlerNotFoundTask = ValueTask.FromException<object>(PreallocatedHandlerNotFound);
    private static readonly ValueTask HandlerNotFoundVoidTask = ValueTask.FromException(PreallocatedHandlerNotFound);

    // Cached exception tasks for different parameter names
    private static readonly FrozenDictionary<string, ValueTask> CachedVoidExceptionTasks = CreateVoidExceptionCache();
    private static readonly FrozenDictionary<string, ValueTask<object>> CachedObjectExceptionTasks = CreateObjectExceptionCache();

    private static FrozenDictionary<string, ValueTask> CreateVoidExceptionCache()
    {
        return new Dictionary<string, ValueTask>
        {
            ["request"] = ValueTask.FromException(new ArgumentNullException("request")),
            ["handlerName"] = ValueTask.FromException(new ArgumentNullException("handlerName")),
            ["notification"] = ValueTask.FromException(new ArgumentNullException("notification")),
            ["cancellationToken"] = ValueTask.FromException(new ArgumentNullException("cancellationToken"))
        }.ToFrozenDictionary();
    }

    private static FrozenDictionary<string, ValueTask<object>> CreateObjectExceptionCache()
    {
        return new Dictionary<string, ValueTask<object>>
        {
            ["request"] = ValueTask.FromException<object>(new ArgumentNullException("request")),
            ["handlerName"] = ValueTask.FromException<object>(new ArgumentNullException("handlerName")),
            ["handler"] = ValueTask.FromException<object>(new HandlerNotFoundException("Handler not found"))
        }.ToFrozenDictionary();
    }

    /// <summary>
    /// Ultra-fast argument null exception with pre-allocated instances
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<T> ThrowArgumentNull<T>(string? paramName = "request")
    {
        // Use pre-cached exception tasks when possible
        if (paramName != null && CachedObjectExceptionTasks.TryGetValue(paramName, out var cachedTask))
        {
            return Unsafe.As<ValueTask<object>, ValueTask<T>>(ref Unsafe.AsRef(in cachedTask));
        }

        // Fallback to creating new exception (rare path)
        return ValueTask.FromException<T>(new ArgumentNullException(paramName));
    }

    /// <summary>
    /// Ultra-fast void argument null exception
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask ThrowArgumentNullVoid(string? paramName = "request")
    {
        // Use pre-cached void exception tasks
        if (paramName != null && CachedVoidExceptionTasks.TryGetValue(paramName, out var cachedTask))
        {
            return cachedTask;
        }

        // Fallback to pre-allocated default
        return ArgumentNullVoidTask;
    }

    /// <summary>
    /// Ultra-fast handler not found exception
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<T> ThrowHandlerNotFound<T>()
    {
        // Always use pre-allocated instance
        return Unsafe.As<ValueTask<object>, ValueTask<T>>(ref Unsafe.AsRef(in HandlerNotFoundTask));
    }

    /// <summary>
    /// Ultra-fast handler not found void exception
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask ThrowHandlerNotFoundVoid()
    {
        return HandlerNotFoundVoidTask;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IAsyncEnumerable<T> ThrowArgumentNullStream<T>()
    {
        return ThrowArgumentNullStreamImpl();
        
        static async IAsyncEnumerable<T> ThrowArgumentNullStreamImpl()
        {
            await Task.CompletedTask;
            throw new ArgumentNullException();
#pragma warning disable CS0162 // Unreachable code detected
            yield break; // Required for async iterator
#pragma warning restore CS0162
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IAsyncEnumerable<T> ThrowHandlerNotFoundStream<T>()
    {
        return ThrowHandlerNotFoundStreamImpl();
        
        static async IAsyncEnumerable<T> ThrowHandlerNotFoundStreamImpl()
        {
            await Task.CompletedTask;
            throw new HandlerNotFoundException("Handler not found");
#pragma warning disable CS0162 // Unreachable code detected
            yield break; // Required for async iterator
#pragma warning restore CS0162
        }
    }
}