namespace Relay.MessageBroker.Security;

/// <summary>
/// Metadata about message encryption stored in message headers.
/// </summary>
public class EncryptionMetadata
{
    /// <summary>
    /// Gets or sets the key version used for encryption.
    /// </summary>
    public string KeyVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encryption algorithm used.
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the message was encrypted.
    /// </summary>
    public DateTimeOffset EncryptedAt { get; set; }

    /// <summary>
    /// Header key for encryption metadata.
    /// </summary>
    public const string HeaderKey = "X-Encryption-Metadata";

    /// <summary>
    /// Header key for key version.
    /// </summary>
    public const string KeyVersionHeaderKey = "X-Encryption-KeyVersion";

    /// <summary>
    /// Header key for algorithm.
    /// </summary>
    public const string AlgorithmHeaderKey = "X-Encryption-Algorithm";

    /// <summary>
    /// Header key for encrypted timestamp.
    /// </summary>
    public const string EncryptedAtHeaderKey = "X-Encryption-EncryptedAt";
}
