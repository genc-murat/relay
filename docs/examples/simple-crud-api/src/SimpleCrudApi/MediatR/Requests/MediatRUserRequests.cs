using MediatR;
using SimpleCrudApi.Models;

namespace SimpleCrudApi.MediatR.Requests;

public record MediatRGetUserQuery(int Id) : IRequest<User?>;

public record MediatRGetUsersQuery(int Page = 1, int PageSize = 10) : IRequest<IEnumerable<User>>;

public record MediatRCreateUserCommand(string Name, string Email) : IRequest<User>;

public record MediatRUpdateUserCommand(int Id, string Name, string Email) : IRequest<User?>;

public record MediatRDeleteUserCommand(int Id) : IRequest;

public record MediatRUserCreatedNotification(User User) : INotification;
public record MediatRUserUpdatedNotification(User User) : INotification;
public record MediatRUserDeletedNotification(int UserId) : INotification;