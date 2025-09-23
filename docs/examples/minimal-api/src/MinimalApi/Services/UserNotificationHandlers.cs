using Relay.Core;
using MinimalApi.Models;

namespace MinimalApi.Services;

public class UserNotificationHandlers
{
    private readonly ILogger<UserNotificationHandlers> _logger;

    public UserNotificationHandlers(ILogger<UserNotificationHandlers> logger)
    {
        _logger = logger;
    }

    [Notification]
    public async ValueTask OnUserCreated(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User created: {UserId} - {Name}",
            notification.User.Id, notification.User.Name);

        await Task.Delay(10, cancellationToken);
    }

    [Notification]
    public async ValueTask OnUserUpdated(UserUpdatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User updated: {UserId} - {Name}",
            notification.User.Id, notification.User.Name);

        await Task.Delay(10, cancellationToken);
    }

    [Notification]
    public async ValueTask OnUserDeleted(UserDeletedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User deleted: {UserId}", notification.UserId);

        await Task.Delay(10, cancellationToken);
    }
}