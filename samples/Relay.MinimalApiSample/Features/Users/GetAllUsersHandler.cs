using Relay.Core.Contracts.Handlers;
using Relay.MinimalApiSample.Infrastructure;

namespace Relay.MinimalApiSample.Features.Users;

public class GetAllUsersHandler : IRequestHandler<GetAllUsersRequest, GetAllUsersResponse>
{
    private readonly InMemoryDatabase _database;
    private readonly ILogger<GetAllUsersHandler> _logger;

    public GetAllUsersHandler(InMemoryDatabase database, ILogger<GetAllUsersHandler> logger)
    {
        _database = database;
        _logger = logger;
    }

    public ValueTask<GetAllUsersResponse> HandleAsync(GetAllUsersRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all users");

        var users = _database.Users.Values
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.IsActive))
            .ToList();

        _logger.LogInformation("Retrieved {Count} users", users.Count);

        var response = new GetAllUsersResponse(users);
        return ValueTask.FromResult(response);
    }
}
