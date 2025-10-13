using Relay.Core.Contracts.Requests;

namespace Relay.ControllerApiSample.Features.Users;

// Request
public record GetUserRequest(Guid Id) : IRequest<GetUserResponse?>;

// Response
public record GetUserResponse(Guid Id, string Name, string Email, bool IsActive, DateTime CreatedAt);
