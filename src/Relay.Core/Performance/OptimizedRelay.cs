using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core.Performance;

/// <summary>
/// High-performance Relay implementation that uses generated optimized dispatchers
/// </summary>
public class OptimizedRelay : IRelay
{
    private readonly IServiceProvider _serviceProvider;

    public OptimizedRelay(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Sends a request and returns a response using optimized dispatch
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        try
        {
            // TODO: Use generated optimized dispatcher for maximum performance
            // Fallback to regular dispatcher for now
            var dispatcher = _serviceProvider.GetService<IRequestDispatcher>();
            if (dispatcher == null)
                throw new HandlerNotFoundException(request.GetType().Name);
            return await dispatcher.DispatchAsync(request, cancellationToken);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            throw new RelayException(request.GetType().Name, null, "Handler execution failed", ex);
        }
    }

    /// <summary>
    /// Sends a request with no response using optimized dispatch
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        try
        {
            // TODO: Use generated optimized dispatcher for maximum performance
            // Fallback to regular dispatcher for now
            var dispatcher = _serviceProvider.GetService<IRequestDispatcher>();
            if (dispatcher == null)
                throw new HandlerNotFoundException(request.GetType().Name);
            await dispatcher.DispatchAsync(request, cancellationToken);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            throw new RelayException(request.GetType().Name, null, "Handler execution failed", ex);
        }
    }

    /// <summary>
    /// Sends a named request and returns a response using optimized dispatch
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrEmpty(handlerName)) throw new ArgumentException("Handler name cannot be null or empty", nameof(handlerName));

        try
        {
            // TODO: Use generated optimized dispatcher with named handler
            // Fallback to regular dispatcher for now
            var dispatcher = _serviceProvider.GetService<IRequestDispatcher>();
            if (dispatcher == null)
                throw new HandlerNotFoundException(request.GetType().Name, handlerName);
            return await dispatcher.DispatchAsync(request, handlerName, cancellationToken);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            throw new RelayException(request.GetType().Name, handlerName, "Named handler execution failed", ex);
        }
    }

    /// <summary>
    /// Streams responses using optimized dispatch
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        try
        {
            // TODO: Use generated optimized streaming dispatcher
            // Fallback to regular dispatcher for now
            var dispatcher = _serviceProvider.GetService<IStreamDispatcher>();
            if (dispatcher == null)
                throw new HandlerNotFoundException(request.GetType().Name);
            return dispatcher.DispatchAsync(request, cancellationToken);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            throw new RelayException(request.GetType().Name, null, "Streaming handler execution failed", ex);
        }
    }

    /// <summary>
    /// Publishes a notification using optimized dispatch
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) 
        where TNotification : INotification
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));

        try
        {
            // TODO: Use generated optimized notification dispatcher
            // Fallback to regular dispatcher for now
            var dispatcher = _serviceProvider.GetService<INotificationDispatcher>();
            if (dispatcher != null)
                await dispatcher.DispatchAsync(notification, cancellationToken);
            // If no dispatcher, silently complete (notifications are optional)
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            throw new RelayException(notification.GetType().Name, null, "Notification handler execution failed", ex);
        }
    }
}