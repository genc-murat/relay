using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Test implementation of IRelay for unit testing scenarios.
/// </summary>
public class TestRelay : IRelay
{
    private readonly ConcurrentQueue<object> _publishedNotifications = new();
    private readonly ConcurrentQueue<object> _sentRequests = new();
    private readonly Dictionary<Type, Func<object, CancellationToken, ValueTask<object>>> _requestHandlers = new();
    private readonly Dictionary<Type, Func<object, CancellationToken, IAsyncEnumerable<object>>> _streamHandlers = new();
    private readonly Dictionary<Type, Func<object, CancellationToken, ValueTask>> _notificationHandlers = new();

    /// <summary>
    /// Gets all published notifications.
    /// </summary>
    public IReadOnlyCollection<object> PublishedNotifications => _publishedNotifications.ToArray();

    /// <summary>
    /// Gets all sent requests.
    /// </summary>
    public IReadOnlyCollection<object> SentRequests => _sentRequests.ToArray();

    /// <inheritdoc />
    public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _sentRequests.Enqueue(request);

        var requestType = request.GetType();
        if (_requestHandlers.TryGetValue(requestType, out var handler))
        {
            return ExecuteRequestHandler<TResponse>(handler, request, cancellationToken);
        }

        // Return default value if no handler is configured
        return ValueTask.FromResult(default(TResponse)!);
    }

    /// <inheritdoc />
    public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _sentRequests.Enqueue(request);

        var requestType = request.GetType();
        if (_requestHandlers.TryGetValue(requestType, out var handler))
        {
            return ExecuteVoidRequestHandler(handler, request, cancellationToken);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _sentRequests.Enqueue(request);

        var requestType = request.GetType();
        if (_streamHandlers.TryGetValue(requestType, out var handler))
        {
            return ExecuteStreamHandler<TResponse>(handler, request, cancellationToken);
        }

        return AsyncEnumerable.Empty<TResponse>();
    }

    /// <inheritdoc />
    public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        _publishedNotifications.Enqueue(notification);

        var notificationType = typeof(TNotification);
        if (_notificationHandlers.TryGetValue(notificationType, out var handler))
        {
            return handler(notification, cancellationToken);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Sets up a request handler for testing.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="handler">The handler function.</param>
    public void SetupRequestHandler<TRequest, TResponse>(Func<TRequest, CancellationToken, ValueTask<TResponse>> handler)
        where TRequest : IRequest<TResponse>
    {
        _requestHandlers[typeof(TRequest)] = async (request, ct) =>
        {
            var response = await handler((TRequest)request, ct);
            return response!;
        };
    }

    /// <summary>
    /// Sets up a void request handler for testing.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="handler">The handler function.</param>
    public void SetupRequestHandler<TRequest>(Func<TRequest, CancellationToken, ValueTask> handler)
        where TRequest : IRequest
    {
        _requestHandlers[typeof(TRequest)] = async (request, ct) =>
        {
            await handler((TRequest)request, ct);
            return Unit.Value;
        };
    }

    /// <summary>
    /// Sets up a stream handler for testing.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="handler">The handler function.</param>
    public void SetupStreamHandler<TRequest, TResponse>(Func<TRequest, CancellationToken, IAsyncEnumerable<TResponse>> handler)
        where TRequest : IStreamRequest<TResponse>
    {
        _streamHandlers[typeof(TRequest)] = (request, ct) => ConvertAsyncEnumerable(handler((TRequest)request, ct));
    }

    private static async IAsyncEnumerable<object> ConvertAsyncEnumerable<T>(IAsyncEnumerable<T> source)
    {
        await foreach (var item in source)
        {
            yield return (object)item!;
        }
    }

    /// <summary>
    /// Sets up a notification handler for testing.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="handler">The handler function.</param>
    public void SetupNotificationHandler<TNotification>(Func<TNotification, CancellationToken, ValueTask> handler)
        where TNotification : INotification
    {
        _notificationHandlers[typeof(TNotification)] = (notification, ct) =>
            handler((TNotification)notification, ct);
    }

    /// <summary>
    /// Clears all recorded requests and notifications.
    /// </summary>
    public void Clear()
    {
        _publishedNotifications.Clear();
        _sentRequests.Clear();
    }

    /// <summary>
    /// Clears all registered handlers.
    /// </summary>
    public void ClearHandlers()
    {
        _requestHandlers.Clear();
        _streamHandlers.Clear();
        _notificationHandlers.Clear();
    }

    /// <summary>
    /// Gets published notifications of a specific type.
    /// </summary>
    /// <typeparam name="T">The notification type.</typeparam>
    /// <returns>Published notifications of the specified type.</returns>
    public IEnumerable<T> GetPublishedNotifications<T>() where T : INotification
    {
        return _publishedNotifications.OfType<T>();
    }

    /// <summary>
    /// Gets sent requests of a specific type.
    /// </summary>
    /// <typeparam name="T">The request type.</typeparam>
    /// <returns>Sent requests of the specified type.</returns>
    public IEnumerable<T> GetSentRequests<T>()
    {
        return _sentRequests.OfType<T>();
    }

    private async ValueTask<TResponse> ExecuteRequestHandler<TResponse>(
        Func<object, CancellationToken, ValueTask<object>> handler,
        object request,
        CancellationToken cancellationToken)
    {
        var result = await handler(request, cancellationToken);
        return (TResponse)result;
    }

    private async ValueTask ExecuteVoidRequestHandler(
        Func<object, CancellationToken, ValueTask<object>> handler,
        object request,
        CancellationToken cancellationToken)
    {
        await handler(request, cancellationToken);
    }

    private async IAsyncEnumerable<TResponse> ExecuteStreamHandler<TResponse>(
        Func<object, CancellationToken, IAsyncEnumerable<object>> handler,
        object request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in handler(request, cancellationToken))
        {
            yield return (TResponse)item;
        }
    }
}
