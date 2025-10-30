namespace Relay.MessageBroker.Security;

/// <summary>
/// Interface for encrypting and decrypting message payloads.
/// </summary>
public interface IMessageEncryptor
{
    /// <summary>
    /// Encrypts the specified data.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The encrypted data.</returns>
    ValueTask<byte[]> EncryptAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts the specified encrypted data.
    /// </summary>
    /// <param name="encryptedData">The encrypted data to decrypt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decrypted data.</returns>
    ValueTask<byte[]> DecryptAsync(byte[] encryptedData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current key version being used for encryption.
    /// </summary>
    /// <returns>The key version identifier.</returns>
    string GetKeyVersion();
}
