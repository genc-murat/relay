using Relay.Core.Contracts.Requests;

namespace Relay.ControllerApiSample.Features.Users;

// Request
public record CreateUserRequest(string Name, string Email) : IRequest<CreateUserResponse>;

// Response
public record CreateUserResponse(Guid Id, string Name, string Email, DateTime CreatedAt);
