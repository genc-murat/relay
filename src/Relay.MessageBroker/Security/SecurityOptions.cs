namespace Relay.MessageBroker.Security;

/// <summary>
/// Configuration options for message security features.
/// </summary>
public class SecurityOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether encryption is enabled.
    /// </summary>
    public bool EnableEncryption { get; set; }

    /// <summary>
    /// Gets or sets the encryption algorithm to use.
    /// Default is AES256-GCM.
    /// </summary>
    public string EncryptionAlgorithm { get; set; } = "AES256-GCM";

    /// <summary>
    /// Gets or sets the Azure Key Vault URL for key management.
    /// If not specified, keys will be loaded from environment variables.
    /// </summary>
    public string? KeyVaultUrl { get; set; }

    /// <summary>
    /// Gets or sets the encryption key as a base64-encoded string.
    /// This is used when KeyVaultUrl is not specified.
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// Gets or sets the key version identifier.
    /// </summary>
    public string KeyVersion { get; set; } = "v1";

    /// <summary>
    /// Gets or sets the grace period for decrypting messages with old keys.
    /// Default is 24 hours.
    /// </summary>
    public TimeSpan KeyRotationGracePeriod { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Validates the security options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when options are invalid.</exception>
    public void Validate()
    {
        if (EnableEncryption)
        {
            if (string.IsNullOrWhiteSpace(KeyVaultUrl) && string.IsNullOrWhiteSpace(EncryptionKey))
            {
                throw new InvalidOperationException(
                    "Either KeyVaultUrl or EncryptionKey must be specified when encryption is enabled.");
            }

            if (string.IsNullOrWhiteSpace(KeyVersion))
            {
                throw new InvalidOperationException("KeyVersion must be specified when encryption is enabled.");
            }

            if (KeyRotationGracePeriod < TimeSpan.Zero)
            {
                throw new InvalidOperationException("KeyRotationGracePeriod must be a positive value.");
            }
        }
    }
}
