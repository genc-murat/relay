using Relay.Core.Pipeline.Interfaces;
using Relay.MinimalApiSample.Infrastructure;

namespace Relay.MinimalApiSample.Features.Examples.Validation;

/// <summary>
/// Post-processor for user registration.
/// Runs AFTER successful handler execution for audit logging and notifications.
/// </summary>
public class ValidationPostProcessor : IRequestPostProcessor<RegisterUserRequest, RegisterUserResponse>
{
    private readonly ILogger<ValidationPostProcessor> _logger;
    private readonly IEmailService _emailService;

    public ValidationPostProcessor(
        ILogger<ValidationPostProcessor> logger,
        IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async ValueTask ProcessAsync(
        RegisterUserRequest request,
        RegisterUserResponse response,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "POST-PROCESSING: User {Username} registered successfully with ID {UserId}",
            request.Username,
            response.UserId);

        // Simulate audit log
        _logger.LogInformation(
            "POST-PROCESSING: Audit log created for user registration {UserId}",
            response.UserId);

        // Send welcome email
        try
        {
            await _emailService.SendEmailAsync(
                request.Email,
                "Welcome to our platform!",
                $"Dear {request.Username},\n\nWelcome to our platform! Your account has been created successfully.\n\nBest regards,\nThe Team");

            _logger.LogInformation(
                "POST-PROCESSING: Welcome email sent to {Email}",
                request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST-PROCESSING: Failed to send welcome email to {Email}", request.Email);
        }

        // Simulate analytics tracking
        _logger.LogInformation(
            "POST-PROCESSING: Analytics event tracked for user registration {UserId}",
            response.UserId);
    }
}