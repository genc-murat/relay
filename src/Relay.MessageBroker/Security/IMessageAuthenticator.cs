namespace Relay.MessageBroker.Security;

/// <summary>
/// Interface for authenticating and authorizing message operations.
/// </summary>
public interface IMessageAuthenticator
{
    /// <summary>
    /// Validates an authentication token.
    /// </summary>
    /// <param name="token">The authentication token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the token is valid, false otherwise.</returns>
    ValueTask<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authorizes a token for a specific operation.
    /// </summary>
    /// <param name="token">The authentication token.</param>
    /// <param name="operation">The operation to authorize (e.g., "publish", "subscribe").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the operation is authorized, false otherwise.</returns>
    ValueTask<bool> AuthorizeAsync(string token, string operation, CancellationToken cancellationToken = default);
}
