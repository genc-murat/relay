using Relay.Core.Pipeline.Interfaces;

namespace Relay.MinimalApiSample.Features.Examples.Validation;

/// <summary>
/// Pre-processor for user registration validation.
/// Runs BEFORE the handler to perform additional business rule validation.
/// </summary>
public class ValidationPreProcessor : IRequestPreProcessor<RegisterUserRequest>
{
    private readonly ILogger<ValidationPreProcessor> _logger;

    public ValidationPreProcessor(ILogger<ValidationPreProcessor> logger)
    {
        _logger = logger;
    }

    public ValueTask ProcessAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "PRE-PROCESSING: Validating registration for user {Username}",
            request.Username);

        // Business rule validation examples
        if (request.Username.Contains("admin") || request.Username.Contains("root"))
        {
            _logger.LogWarning("PRE-PROCESSING: Username contains reserved words");
        }

        if (request.Password.Length < 8)
        {
            _logger.LogWarning("PRE-PROCESSING: Password too short");
        }

        if (request.Age < 13)
        {
            _logger.LogWarning("PRE-PROCESSING: User too young for registration");
        }

        _logger.LogInformation("PRE-PROCESSING: Business rule validation completed");

        return ValueTask.CompletedTask;
    }
}