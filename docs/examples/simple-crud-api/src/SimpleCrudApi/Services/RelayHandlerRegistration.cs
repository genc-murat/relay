using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using SimpleCrudApi.Models;
using SimpleCrudApi.Models.Requests;

namespace SimpleCrudApi.Services;

// Manual handler implementations for Relay (when source generator is not available)
public class RelayGetUserHandler : IRequestHandler<GetUserQuery, User?>
{
    private readonly UserService _userService;

    public RelayGetUserHandler(UserService userService)
    {
        _userService = userService;
    }

    public async ValueTask<User?> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        return await _userService.GetUser(request, cancellationToken);
    }
}

public class RelayGetUsersHandler : IRequestHandler<GetUsersQuery, IEnumerable<User>>
{
    private readonly UserService _userService;

    public RelayGetUsersHandler(UserService userService)
    {
        _userService = userService;
    }

    public async ValueTask<IEnumerable<User>> HandleAsync(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await _userService.GetUsers(request, cancellationToken);
    }
}

public class RelayCreateUserHandler : IRequestHandler<CreateUserCommand, User>
{
    private readonly UserService _userService;

    public RelayCreateUserHandler(UserService userService)
    {
        _userService = userService;
    }

    public async ValueTask<User> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        return await _userService.CreateUser(request, cancellationToken);
    }
}

public class RelayUpdateUserHandler : IRequestHandler<UpdateUserCommand, User?>
{
    private readonly UserService _userService;

    public RelayUpdateUserHandler(UserService userService)
    {
        _userService = userService;
    }

    public async ValueTask<User?> HandleAsync(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        return await _userService.UpdateUser(request, cancellationToken);
    }
}

public class RelayDeleteUserHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly UserService _userService;

    public RelayDeleteUserHandler(UserService userService)
    {
        _userService = userService;
    }

    public async ValueTask HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        await _userService.DeleteUser(request, cancellationToken);
    }
}

// Notification handlers
public class RelayUserCreatedNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly UserNotificationHandlers _notificationHandlers;

    public RelayUserCreatedNotificationHandler(UserNotificationHandlers notificationHandlers)
    {
        _notificationHandlers = notificationHandlers;
    }

    public async ValueTask HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _notificationHandlers.OnUserCreated(notification, cancellationToken);
    }
}

public class RelayUserUpdatedNotificationHandler : INotificationHandler<UserUpdatedNotification>
{
    private readonly UserNotificationHandlers _notificationHandlers;

    public RelayUserUpdatedNotificationHandler(UserNotificationHandlers notificationHandlers)
    {
        _notificationHandlers = notificationHandlers;
    }

    public async ValueTask HandleAsync(UserUpdatedNotification notification, CancellationToken cancellationToken)
    {
        await _notificationHandlers.OnUserUpdated(notification, cancellationToken);
    }
}

public class RelayUserDeletedNotificationHandler : INotificationHandler<UserDeletedNotification>
{
    private readonly UserNotificationHandlers _notificationHandlers;

    public RelayUserDeletedNotificationHandler(UserNotificationHandlers notificationHandlers)
    {
        _notificationHandlers = notificationHandlers;
    }

    public async ValueTask HandleAsync(UserDeletedNotification notification, CancellationToken cancellationToken)
    {
        await _notificationHandlers.OnUserDeleted(notification, cancellationToken);
    }
}

public static class RelayHandlerRegistrationExtensions
{
    public static IServiceCollection AddRelayHandlers(this IServiceCollection services)
    {
        // Register request handlers
        services.AddScoped<IRequestHandler<GetUserQuery, User?>, RelayGetUserHandler>();
        services.AddScoped<IRequestHandler<GetUsersQuery, IEnumerable<User>>, RelayGetUsersHandler>();
        services.AddScoped<IRequestHandler<CreateUserCommand, User>, RelayCreateUserHandler>();
        services.AddScoped<IRequestHandler<UpdateUserCommand, User?>, RelayUpdateUserHandler>();
        services.AddScoped<IRequestHandler<DeleteUserCommand>, RelayDeleteUserHandler>();

        // Register notification handlers
        services.AddScoped<INotificationHandler<UserCreatedNotification>, RelayUserCreatedNotificationHandler>();
        services.AddScoped<INotificationHandler<UserUpdatedNotification>, RelayUserUpdatedNotificationHandler>();
        services.AddScoped<INotificationHandler<UserDeletedNotification>, RelayUserDeletedNotificationHandler>();

        return services;
    }
}