using Relay.Core.Contracts.Handlers;

namespace Relay.MinimalApiSample.Features.Examples.Notifications;

public class TrackAnalyticsHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<TrackAnalyticsHandler> _logger;

    public TrackAnalyticsHandler(ILogger<TrackAnalyticsHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask HandleAsync(
        UserCreatedNotification notification,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "NOTIFICATION HANDLER 2: Tracking analytics for user {UserId}",
            notification.UserId);

        // Simulate analytics tracking
        await Task.Delay(300, cancellationToken);

        _logger.LogInformation(
            "NOTIFICATION HANDLER 2: Analytics tracked for {Username}",
            notification.Username);
    }
}
