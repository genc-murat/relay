namespace Relay.MinimalApiSample.Infrastructure;

/// <summary>
/// Email service interface for sending emails
/// </summary>
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}