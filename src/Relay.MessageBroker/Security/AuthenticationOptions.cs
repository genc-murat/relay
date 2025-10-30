namespace Relay.MessageBroker.Security;

/// <summary>
/// Configuration options for message authentication.
/// </summary>
public class AuthenticationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether authentication is enabled.
    /// </summary>
    public bool EnableAuthentication { get; set; }

    /// <summary>
    /// Gets or sets the JWT issuer for token validation.
    /// </summary>
    public string? JwtIssuer { get; set; }

    /// <summary>
    /// Gets or sets the JWT audience for token validation.
    /// </summary>
    public string? JwtAudience { get; set; }

    /// <summary>
    /// Gets or sets the identity provider URL for token validation.
    /// </summary>
    public string? IdentityProviderUrl { get; set; }

    /// <summary>
    /// Gets or sets the JWT signing key as a base64-encoded string.
    /// Used for symmetric key validation.
    /// </summary>
    public string? JwtSigningKey { get; set; }

    /// <summary>
    /// Gets or sets the public key for JWT signature verification.
    /// Used for asymmetric key validation (RSA, ECDSA).
    /// </summary>
    public string? JwtPublicKey { get; set; }

    /// <summary>
    /// Gets or sets the token cache TTL (time-to-live).
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan TokenCacheTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Validates the authentication options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when options are invalid.</exception>
    public void Validate()
    {
        if (EnableAuthentication)
        {
            if (string.IsNullOrWhiteSpace(JwtIssuer))
            {
                throw new InvalidOperationException("JwtIssuer must be specified when authentication is enabled.");
            }

            if (string.IsNullOrWhiteSpace(JwtAudience))
            {
                throw new InvalidOperationException("JwtAudience must be specified when authentication is enabled.");
            }

            if (string.IsNullOrWhiteSpace(JwtSigningKey) && string.IsNullOrWhiteSpace(JwtPublicKey))
            {
                throw new InvalidOperationException(
                    "Either JwtSigningKey or JwtPublicKey must be specified when authentication is enabled.");
            }

            if (TokenCacheTtl < TimeSpan.Zero)
            {
                throw new InvalidOperationException("TokenCacheTtl must be a positive value.");
            }
        }
    }
}
