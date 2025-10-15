using System.Threading.Tasks;

namespace Relay.MinimalApiSample.Infrastructure;

/// <summary>
/// Console-based email service implementation for demo purposes
/// </summary>
public class ConsoleEmailService : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body)
    {
        Console.WriteLine($"[EMAIL] To: {to}");
        Console.WriteLine($"[EMAIL] Subject: {subject}");
        Console.WriteLine($"[EMAIL] Body: {body}");
        Console.WriteLine();

        return Task.CompletedTask;
    }
}