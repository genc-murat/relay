using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core.Performance;

/// <summary>
/// AOT-optimized Relay implementation designed for Native AOT compilation
/// Uses compile-time known types and avoids runtime reflection
/// </summary>
[RequiresUnreferencedCode("This type uses reflection which is incompatible with trimming.")]
[RequiresDynamicCode("This type might require dynamic code generation.")]
public sealed class AOTOptimizedRelay : IRelay
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRequestDispatcher? _requestDispatcher;
    private readonly IStreamDispatcher? _streamDispatcher;
    private readonly INotificationDispatcher? _notificationDispatcher;

    public AOTOptimizedRelay(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _requestDispatcher = serviceProvider.GetService<IRequestDispatcher>();
        _streamDispatcher = serviceProvider.GetService<IStreamDispatcher>();
        _notificationDispatcher = serviceProvider.GetService<INotificationDispatcher>();
    }

    /// <summary>
    /// AOT-friendly request dispatch with compile-time type information
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // AOT-optimized path: use compile-time known dispatchers
        var dispatcher = _requestDispatcher;
        if (dispatcher == null)
        {
            return ValueTask.FromException<TResponse>(new HandlerNotFoundException(typeof(TResponse).Name));
        }

        return dispatcher.DispatchAsync(request, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispatcher = _requestDispatcher;
        if (dispatcher == null)
        {
            return ValueTask.FromException(new HandlerNotFoundException(request.GetType().Name));
        }

        return dispatcher.DispatchAsync(request, cancellationToken);
    }

    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispatcher = _streamDispatcher;
        if (dispatcher == null)
        {
            return CreateHandlerNotFoundAsyncEnumerable<TResponse>();
        }

        return dispatcher.DispatchAsync(request, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var dispatcher = _notificationDispatcher;
        if (dispatcher == null)
        {
            return ValueTask.CompletedTask;
        }

        return dispatcher.DispatchAsync(notification, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async IAsyncEnumerable<TResponse> CreateHandlerNotFoundAsyncEnumerable<TResponse>()
    {
        await Task.CompletedTask;
        throw new HandlerNotFoundException(typeof(TResponse).Name);
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}

/// <summary>
/// AOT-compatible source generator attributes
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AOTGeneratedAttribute : Attribute
{
    public string HandlerType { get; }
    public string RequestType { get; }

    public AOTGeneratedAttribute(string handlerType, string requestType)
    {
        HandlerType = handlerType;
        RequestType = requestType;
    }
}

/// <summary>
/// AOT-compatible handler registry that avoids runtime type discovery
/// </summary>
public sealed class AOTHandlerRegistry
{
    private readonly Dictionary<Type, Type> _handlerMap = new();
    private readonly Dictionary<Type, Func<IServiceProvider, object>> _factoryMap = new();

    /// <summary>
    /// Registers a handler type at compile time
    /// </summary>
    public void RegisterHandler<TRequest, THandler>()
        where TRequest : class
        where THandler : class
    {
        _handlerMap[typeof(TRequest)] = typeof(THandler);
        _factoryMap[typeof(TRequest)] = sp => sp.GetRequiredService<THandler>();
    }

    /// <summary>
    /// Registers a handler factory for AOT scenarios
    /// </summary>
    public void RegisterHandlerFactory<TRequest>(Func<IServiceProvider, object> factory)
        where TRequest : class
    {
        _factoryMap[typeof(TRequest)] = factory;
    }

    /// <summary>
    /// Gets handler factory in AOT-safe manner
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetHandlerFactory<TRequest>(out Func<IServiceProvider, object>? factory)
        where TRequest : class
    {
        return _factoryMap.TryGetValue(typeof(TRequest), out factory);
    }

    /// <summary>
    /// Pre-compile all handler factories for better startup performance
    /// </summary>
    public void WarmupHandlers(IServiceProvider serviceProvider)
    {
        foreach (var factory in _factoryMap.Values)
        {
            try
            {
                factory(serviceProvider);
            }
            catch
            {
                // Ignore warmup failures
            }
        }
    }
}

/// <summary>
/// Static AOT handler registry for compile-time registration
/// </summary>
public static class AOTHandlerConfiguration
{
    private static readonly AOTHandlerRegistry Registry = new();

    public static AOTHandlerRegistry GetRegistry() => Registry;

    /// <summary>
    /// Configure handlers at application startup for AOT scenarios
    /// </summary>
    public static void ConfigureHandlers()
    {
        // This would be populated by source generators or manual registration
        // Registry.RegisterHandler<MyRequest, MyHandler>();
    }

    /// <summary>
    /// Create AOT-optimized relay with pre-configured handlers
    /// </summary>
    public static AOTOptimizedRelay CreateRelay(IServiceProvider serviceProvider)
    {
        Registry.WarmupHandlers(serviceProvider);
        return new AOTOptimizedRelay(serviceProvider);
    }
}