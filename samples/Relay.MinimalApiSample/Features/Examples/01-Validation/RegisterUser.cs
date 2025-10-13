using Relay.Core.Contracts.Requests;

namespace Relay.MinimalApiSample.Features.Examples.Validation;

/// <summary>
/// Example: User registration with validation
/// Demonstrates automatic request validation
/// </summary>
public record RegisterUserRequest(
    string Username,
    string Email,
    string Password,
    int Age
) : IRequest<RegisterUserResponse>;

public record RegisterUserResponse(
    Guid UserId,
    string Username,
    string Message
);
