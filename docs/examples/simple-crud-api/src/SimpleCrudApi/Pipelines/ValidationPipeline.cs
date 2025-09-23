using Relay.Core;
using SimpleCrudApi.Models.Requests;

namespace SimpleCrudApi.Pipelines;

public class ValidationPipeline
{
    private readonly ILogger<ValidationPipeline> _logger;

    public ValidationPipeline(ILogger<ValidationPipeline> logger)
    {
        _logger = logger;
    }

    [Pipeline(Order = -100)] // Execute early
    public async ValueTask<TResponse> ValidateRequests<TRequest, TResponse>(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Basic validation example
        if (request is CreateUserCommand createCmd)
        {
            if (string.IsNullOrWhiteSpace(createCmd.Name))
                throw new ArgumentException("Name is required", nameof(createCmd.Name));

            if (string.IsNullOrWhiteSpace(createCmd.Email))
                throw new ArgumentException("Email is required", nameof(createCmd.Email));

            if (!IsValidEmail(createCmd.Email))
                throw new ArgumentException("Invalid email format", nameof(createCmd.Email));
        }

        if (request is UpdateUserCommand updateCmd)
        {
            if (updateCmd.Id <= 0)
                throw new ArgumentException("Invalid user ID", nameof(updateCmd.Id));

            if (string.IsNullOrWhiteSpace(updateCmd.Name))
                throw new ArgumentException("Name is required", nameof(updateCmd.Name));

            if (!IsValidEmail(updateCmd.Email))
                throw new ArgumentException("Invalid email format", nameof(updateCmd.Email));
        }

        _logger.LogDebug("Validation passed for {RequestType}", typeof(TRequest).Name);
        return await next();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}