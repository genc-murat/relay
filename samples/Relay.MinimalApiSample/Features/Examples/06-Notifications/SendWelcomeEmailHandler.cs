using Relay.Core.Contracts.Handlers;

namespace Relay.MinimalApiSample.Features.Examples.Notifications;

public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<SendWelcomeEmailHandler> _logger;

    public SendWelcomeEmailHandler(ILogger<SendWelcomeEmailHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask HandleAsync(
        UserCreatedNotification notification,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "NOTIFICATION HANDLER 1: Sending welcome email to {Email}",
            notification.Email);

        // Simulate email sending
        await Task.Delay(500, cancellationToken);

        _logger.LogInformation(
            "NOTIFICATION HANDLER 1: Welcome email sent successfully to {Username}",
            notification.Username);
    }
}
