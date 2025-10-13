using Relay.Core.Contracts.Requests;

namespace Relay.MinimalApiSample.Features.Examples.Notifications;

/// <summary>
/// Notification: Multiple handlers can handle the same event
/// </summary>
public record UserCreatedNotification(
    Guid UserId,
    string Username,
    string Email
) : INotification;
