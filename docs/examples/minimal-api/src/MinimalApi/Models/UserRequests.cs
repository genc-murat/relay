using Relay.Core;
using MinimalApi.Models;

namespace MinimalApi.Models;

public record GetUserQuery(int Id) : IRequest<User?>;

public record GetUsersQuery(int Page = 1, int PageSize = 10) : IRequest<IEnumerable<User>>;

public record CreateUserCommand(string Name, string Email) : IRequest<User>;

public record UpdateUserCommand(int Id, string Name, string Email) : IRequest<User?>;

public record DeleteUserCommand(int Id) : IRequest;

public record UserCreatedNotification(User User) : INotification;
public record UserUpdatedNotification(User User) : INotification;
public record UserDeletedNotification(int UserId) : INotification;