using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using SimpleCrudApi.Models;
using SimpleCrudApi.Models.Requests;
using SimpleCrudApi.Services;

namespace SimpleCrudApi.Optimizations;

/// <summary>
/// Ultra-optimized dispatcher that bypasses reflection and provides direct calls
/// </summary>
public class OptimizedRelayDispatcher : IRequestDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private UserService? _cachedUserService;

    public OptimizedRelayDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private UserService GetUserService()
    {
        return _cachedUserService ??= _serviceProvider.GetRequiredService<UserService>();
    }

    public async ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        switch (request)
        {
            case GetUserQuery getUserQuery when typeof(TResponse) == typeof(User):
                var user = await GetUserService().GetUser(getUserQuery, cancellationToken);
                return (TResponse)(object)user!;

            case GetUsersQuery getUsersQuery when typeof(TResponse) == typeof(IEnumerable<User>):
                var users = await GetUserService().GetUsers(getUsersQuery, cancellationToken);
                return (TResponse)(object)users;

            case CreateUserCommand createCommand when typeof(TResponse) == typeof(User):
                var createdUser = await GetUserService().CreateUser(createCommand, cancellationToken);
                return (TResponse)(object)createdUser;

            case UpdateUserCommand updateCommand when typeof(TResponse) == typeof(User):
                var updatedUser = await GetUserService().UpdateUser(updateCommand, cancellationToken);
                return (TResponse)(object)updatedUser!;

            default:
                throw new InvalidOperationException($"Unknown request type: {request.GetType().Name}");
        }
    }

    public ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)
    {
        return request switch
        {
            DeleteUserCommand deleteCommand =>
                GetUserService().DeleteUser(deleteCommand, cancellationToken),

            _ => ValueTask.FromException(new InvalidOperationException($"Unknown request type: {request.GetType().Name}"))
        };
    }

    public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
    {
        // Named handlers not supported in this optimized version
        throw new NotSupportedException("Named handlers not supported in optimized dispatcher");
    }

    public ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)
    {
        // Named handlers not supported in this optimized version
        throw new NotSupportedException("Named handlers not supported in optimized dispatcher");
    }
}

/// <summary>
/// Ultra-optimized Relay implementation using direct dispatch
/// </summary>
public class UltraOptimizedRelay : IRelay
{
    private readonly OptimizedRelayDispatcher _dispatcher;
    private readonly INotificationDispatcher? _notificationDispatcher;

    public UltraOptimizedRelay(IServiceProvider serviceProvider)
    {
        _dispatcher = new OptimizedRelayDispatcher(serviceProvider);
        _notificationDispatcher = serviceProvider.GetService<INotificationDispatcher>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _dispatcher.DispatchAsync(request, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _dispatcher.DispatchAsync(request, cancellationToken);
    }

    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Streaming not supported in this optimized version");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (_notificationDispatcher == null)
            return ValueTask.CompletedTask;

        return _notificationDispatcher.DispatchAsync(notification, cancellationToken);
    }
}