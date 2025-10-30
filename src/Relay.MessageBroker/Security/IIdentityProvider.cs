namespace Relay.MessageBroker.Security;

/// <summary>
/// Interface for external identity providers.
/// </summary>
public interface IIdentityProvider
{
    /// <summary>
    /// Validates a token with the external identity provider.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the token is valid, false otherwise.</returns>
    ValueTask<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the token validation parameters from the identity provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token validation parameters.</returns>
    ValueTask<TokenValidationInfo> GetValidationInfoAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Token validation information from an identity provider.
/// </summary>
public class TokenValidationInfo
{
    /// <summary>
    /// Gets or sets the issuer.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audience.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signing keys.
    /// </summary>
    public List<string> SigningKeys { get; set; } = new();

    /// <summary>
    /// Gets or sets the JWKS (JSON Web Key Set) URI.
    /// </summary>
    public string? JwksUri { get; set; }
}
