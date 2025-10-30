using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Relay.MessageBroker.Security;

/// <summary>
/// Message encryptor using AES-256-GCM encryption.
/// </summary>
public class AesMessageEncryptor : IMessageEncryptor, IAsyncDisposable
{
    private readonly SecurityOptions _options;
    private readonly ILogger<AesMessageEncryptor> _logger;
    private readonly IKeyProvider _keyProvider;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesMessageEncryptor"/> class.
    /// </summary>
    /// <param name="options">The security options.</param>
    /// <param name="keyProvider">The key provider for loading encryption keys.</param>
    /// <param name="logger">The logger.</param>
    public AesMessageEncryptor(
        IOptions<SecurityOptions> options,
        IKeyProvider keyProvider,
        ILogger<AesMessageEncryptor> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options.Validate();

        _logger.LogInformation(
            "AesMessageEncryptor initialized with algorithm {Algorithm}, key version {KeyVersion}",
            _options.EncryptionAlgorithm,
            _options.KeyVersion);
    }

    /// <inheritdoc/>
    public async ValueTask<byte[]> EncryptAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            // Get the current encryption key
            var key = await _keyProvider.GetKeyAsync(_options.KeyVersion, cancellationToken);

            // Generate a random nonce (12 bytes for GCM)
            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            RandomNumberGenerator.Fill(nonce);

            // Prepare tag buffer (16 bytes for GCM)
            var tag = new byte[AesGcm.TagByteSizes.MaxSize];

            // Prepare ciphertext buffer
            var ciphertext = new byte[data.Length];

            // Encrypt using AES-GCM
            using var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
            aesGcm.Encrypt(nonce, data, ciphertext, tag);

            // Combine nonce + tag + ciphertext
            var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

            _logger.LogTrace(
                "Encrypted {DataSize} bytes to {EncryptedSize} bytes using key version {KeyVersion}",
                data.Length,
                result.Length,
                _options.KeyVersion);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt message data");
            throw new EncryptionException("Failed to encrypt message data", ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<byte[]> DecryptAsync(byte[] encryptedData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            // Extract nonce, tag, and ciphertext
            var nonceSize = AesGcm.NonceByteSizes.MaxSize;
            var tagSize = AesGcm.TagByteSizes.MaxSize;

            if (encryptedData.Length < nonceSize + tagSize)
            {
                throw new EncryptionException("Encrypted data is too short to contain nonce and tag");
            }

            var nonce = new byte[nonceSize];
            var tag = new byte[tagSize];
            var ciphertext = new byte[encryptedData.Length - nonceSize - tagSize];

            Buffer.BlockCopy(encryptedData, 0, nonce, 0, nonceSize);
            Buffer.BlockCopy(encryptedData, nonceSize, tag, 0, tagSize);
            Buffer.BlockCopy(encryptedData, nonceSize + tagSize, ciphertext, 0, ciphertext.Length);

            // Try to decrypt with current key version first
            var plaintext = await TryDecryptWithKeyAsync(
                _options.KeyVersion,
                nonce,
                tag,
                ciphertext,
                cancellationToken);

            if (plaintext != null)
            {
                _logger.LogTrace(
                    "Decrypted {EncryptedSize} bytes to {DataSize} bytes using key version {KeyVersion}",
                    encryptedData.Length,
                    plaintext.Length,
                    _options.KeyVersion);

                return plaintext;
            }

            // If current key fails, try previous key versions (key rotation support)
            var previousVersions = await _keyProvider.GetPreviousKeyVersionsAsync(
                _options.KeyRotationGracePeriod,
                cancellationToken);

            foreach (var keyVersion in previousVersions)
            {
                plaintext = await TryDecryptWithKeyAsync(
                    keyVersion,
                    nonce,
                    tag,
                    ciphertext,
                    cancellationToken);

                if (plaintext != null)
                {
                    _logger.LogWarning(
                        "Decrypted message using old key version {KeyVersion}. Current version is {CurrentVersion}",
                        keyVersion,
                        _options.KeyVersion);

                    return plaintext;
                }
            }

            throw new EncryptionException("Failed to decrypt message with any available key version");
        }
        catch (EncryptionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt message data");
            throw new EncryptionException("Failed to decrypt message data", ex);
        }
    }

    /// <inheritdoc/>
    public string GetKeyVersion()
    {
        return _options.KeyVersion;
    }

    /// <summary>
    /// Attempts to decrypt data with a specific key version.
    /// </summary>
    /// <returns>The decrypted data if successful, null otherwise.</returns>
    private async ValueTask<byte[]?> TryDecryptWithKeyAsync(
        string keyVersion,
        byte[] nonce,
        byte[] tag,
        byte[] ciphertext,
        CancellationToken cancellationToken)
    {
        try
        {
            var key = await _keyProvider.GetKeyAsync(keyVersion, cancellationToken);
            var plaintext = new byte[ciphertext.Length];

            using var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

            return plaintext;
        }
        catch (CryptographicException)
        {
            // Decryption failed with this key, return null to try next key
            return null;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_keyProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_keyProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
