using Relay.Core.Contracts.Handlers;
using Relay.ControllerApiSample.Infrastructure;

namespace Relay.ControllerApiSample.Features.Users;

public class GetUserHandler : IRequestHandler<GetUserRequest, GetUserResponse?>
{
    private readonly InMemoryDatabase _database;
    private readonly ILogger<GetUserHandler> _logger;

    public GetUserHandler(InMemoryDatabase database, ILogger<GetUserHandler> logger)
    {
        _database = database;
        _logger = logger;
    }

    public ValueTask<GetUserResponse?> HandleAsync(GetUserRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving user with ID: {UserId}", request.Id);

        if (_database.Users.TryGetValue(request.Id, out var user))
        {
            _logger.LogInformation("User found: {UserName}", user.Name);
            var response = new GetUserResponse(user.Id, user.Name, user.Email, user.IsActive, user.CreatedAt);
            return ValueTask.FromResult<GetUserResponse?>(response);
        }

        _logger.LogWarning("User not found with ID: {UserId}", request.Id);
        return ValueTask.FromResult<GetUserResponse?>(null);
    }
}
