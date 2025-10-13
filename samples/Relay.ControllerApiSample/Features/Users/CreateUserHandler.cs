using Relay.Core.Contracts.Handlers;
using Relay.ControllerApiSample.Infrastructure;
using Relay.ControllerApiSample.Models;

namespace Relay.ControllerApiSample.Features.Users;

public class CreateUserHandler : IRequestHandler<CreateUserRequest, CreateUserResponse>
{
    private readonly InMemoryDatabase _database;
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(InMemoryDatabase database, ILogger<CreateUserHandler> logger)
    {
        _database = database;
        _logger = logger;
    }

    public ValueTask<CreateUserResponse> HandleAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user with name: {Name}, email: {Email}", request.Name, request.Email);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _database.Users.TryAdd(user.Id, user);

        _logger.LogInformation("User created successfully with ID: {UserId}", user.Id);

        var response = new CreateUserResponse(user.Id, user.Name, user.Email, user.CreatedAt);
        return ValueTask.FromResult(response);
    }
}
