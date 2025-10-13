using Relay.Core.Contracts.Handlers;

namespace Relay.MinimalApiSample.Features.Examples.Validation;

public class RegisterUserHandler : IRequestHandler<RegisterUserRequest, RegisterUserResponse>
{
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(ILogger<RegisterUserHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<RegisterUserResponse> HandleAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Registering user: {Username} with email: {Email}",
            request.Username,
            request.Email);

        // User registration logic here
        var userId = Guid.NewGuid();

        _logger.LogInformation("User registered successfully with ID: {UserId}", userId);

        var response = new RegisterUserResponse(
            userId,
            request.Username,
            "User registered successfully!");

        return ValueTask.FromResult(response);
    }
}
