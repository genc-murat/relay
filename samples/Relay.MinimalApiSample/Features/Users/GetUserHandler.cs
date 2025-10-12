using Relay.Core.Contracts.Handlers;
using Relay.MinimalApiSample.Infrastructure;

namespace Relay.MinimalApiSample.Features.Users;

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
        _logger.LogInformation("Getting user with ID: {UserId}", request.Id);

        if (_database.Users.TryGetValue(request.Id, out var user))
        {
            var response = new GetUserResponse(user.Id, user.Name, user.Email, user.IsActive, user.CreatedAt);
            return ValueTask.FromResult<GetUserResponse?>(response);
        }

        _logger.LogWarning("User with ID: {UserId} not found", request.Id);
        return ValueTask.FromResult<GetUserResponse?>(null);
    }
}
