using Relay.Core;
using SimpleCrudApi.Data;
using SimpleCrudApi.Models;
using SimpleCrudApi.Models.Requests;

namespace SimpleCrudApi.Services;

public class UserService
{
    private readonly IUserRepository _repository;
    private readonly IRelay _relay;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repository, IRelay relay, ILogger<UserService> logger)
    {
        _repository = repository;
        _relay = relay;
        _logger = logger;
    }

    [Handle]
    public async ValueTask<User?> GetUser(GetUserQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting user with ID: {UserId}", query.Id);
        return await _repository.GetByIdAsync(query.Id, cancellationToken);
    }

    [Handle]
    public async ValueTask<IEnumerable<User>> GetUsers(GetUsersQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting users - Page: {Page}, PageSize: {PageSize}",
            query.Page, query.PageSize);
        return await _repository.GetPagedAsync(query.Page, query.PageSize, cancellationToken);
    }

    [Handle]
    public async ValueTask<User> CreateUser(CreateUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user: {Name} ({Email})", command.Name, command.Email);

        var user = new User
        {
            Id = 0, // Will be set by repository
            Name = command.Name,
            Email = command.Email,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _repository.CreateAsync(user, cancellationToken);

        // Publish notification (will be handled by notification handlers)
        await _relay.PublishAsync(new UserCreatedNotification(createdUser), cancellationToken);

        return createdUser;
    }

    [Handle]
    public async ValueTask<User?> UpdateUser(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating user: {UserId}", command.Id);

        var existingUser = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (existingUser == null)
            return null;

        var updatedUser = existingUser with
        {
            Name = command.Name,
            Email = command.Email,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _repository.UpdateAsync(updatedUser, cancellationToken);

        if (result != null)
        {
            await _relay.PublishAsync(new UserUpdatedNotification(result), cancellationToken);
        }

        return result;
    }

    [Handle]
    public async ValueTask DeleteUser(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting user: {UserId}", command.Id);

        var deleted = await _repository.DeleteAsync(command.Id, cancellationToken);

        if (deleted)
        {
            await _relay.PublishAsync(new UserDeletedNotification(command.Id), cancellationToken);
        }
    }
}