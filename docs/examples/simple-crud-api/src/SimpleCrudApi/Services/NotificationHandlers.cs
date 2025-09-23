using Relay.Core;
using SimpleCrudApi.Models.Requests;

namespace SimpleCrudApi.Services;

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

        // Could send welcome email, update analytics, etc.
        await Task.Delay(10, cancellationToken); // Simulate work
    }

    [Notification]
    public async ValueTask OnUserUpdated(UserUpdatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User updated: {UserId} - {Name}",
            notification.User.Id, notification.User.Name);

        // Could invalidate cache, update search index, etc.
        await Task.Delay(10, cancellationToken); // Simulate work
    }

    [Notification]
    public async ValueTask OnUserDeleted(UserDeletedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User deleted: {UserId}", notification.UserId);

        // Could clean up related data, update analytics, etc.
        await Task.Delay(10, cancellationToken); // Simulate work
    }
}