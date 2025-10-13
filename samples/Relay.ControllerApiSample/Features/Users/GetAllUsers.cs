using Relay.Core.Contracts.Requests;

namespace Relay.ControllerApiSample.Features.Users;

// Request
public record GetAllUsersRequest : IRequest<GetAllUsersResponse>;

// Response
public record GetAllUsersResponse(List<UserDto> Users);

public record UserDto(Guid Id, string Name, string Email, bool IsActive);
