using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Relay.MessageBroker.Security;

/// <summary>
/// Key provider that loads encryption keys from Azure Key Vault.
/// </summary>
/// <remarks>
/// This implementation uses Azure Key Vault to securely store and retrieve encryption keys.
/// It supports DefaultAzureCredential for authentication, which works with:
/// - Managed Identity (recommended for Azure-hosted applications)
/// - Azure CLI credentials (for local development)
/// - Environment variables (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_CLIENT_SECRET)
/// - Visual Studio or VS Code credentials
/// Falls back to environment variables if Key Vault is not accessible.
/// </remarks>
public class AzureKeyVaultKeyProvider : IKeyProvider
{
    private readonly SecurityOptions _options;
    private readonly ILogger<AzureKeyVaultKeyProvider> _logger;
    private readonly ConcurrentDictionary<string, CachedKey> _keyCache;
    private readonly SecretClient? _secretClient;
    private readonly TimeSpan _cacheRefreshInterval = TimeSpan.FromMinutes(5);
    private readonly bool _useKeyVault;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultKeyProvider"/> class.
    /// </summary>
    /// <param name="options">The security options.</param>
    /// <param name="logger">The logger.</param>
    public AzureKeyVaultKeyProvider(
        IOptions<SecurityOptions> options,
        ILogger<AzureKeyVaultKeyProvider> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyCache = new ConcurrentDictionary<string, CachedKey>();

        if (!string.IsNullOrWhiteSpace(_options.KeyVaultUrl))
        {
            try
            {
                var keyVaultUri = new Uri(_options.KeyVaultUrl);
                var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ExcludeInteractiveBrowserCredential = true,
                    Retry =
                    {
                        MaxRetries = 3,
                        Delay = TimeSpan.FromSeconds(2),
                        MaxDelay = TimeSpan.FromSeconds(10),
                        Mode = Azure.Core.RetryMode.Exponential
                    }
                });

                _secretClient = new SecretClient(keyVaultUri, credential);
                _useKeyVault = true;

                _logger.LogInformation(
                    "AzureKeyVaultKeyProvider initialized with Key Vault URL: {KeyVaultUrl}",
                    _options.KeyVaultUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to initialize Azure Key Vault client. Falling back to environment variables. Error: {Error}",
                    ex.Message);
                _useKeyVault = false;
            }
        }
        else
        {
            _logger.LogWarning(
                "KeyVaultUrl not specified. Using environment variables for key management");
            _useKeyVault = false;
        }
    }

    /// <inheritdoc/>
    public async ValueTask<byte[]> GetKeyAsync(string keyVersion, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyVersion);

        // Check cache first
        if (_keyCache.TryGetValue(keyVersion, out var cachedKey))
        {
            if (DateTimeOffset.UtcNow - cachedKey.CachedAt < _cacheRefreshInterval)
            {
                _logger.LogTrace("Retrieved key version {KeyVersion} from cache", keyVersion);
                return cachedKey.Key;
            }

            // Cache expired, remove it
            _keyCache.TryRemove(keyVersion, out _);
        }

        // Load key from Azure Key Vault
        var key = await LoadKeyFromKeyVaultAsync(keyVersion, cancellationToken);

        // Cache the key
        _keyCache[keyVersion] = new CachedKey
        {
            Key = key,
            Version = keyVersion,
            CachedAt = DateTimeOffset.UtcNow
        };

        _logger.LogDebug("Loaded and cached key version {KeyVersion} from Key Vault", keyVersion);

        return key;
    }

    /// <inheritdoc/>
    public async ValueTask<IReadOnlyList<string>> GetPreviousKeyVersionsAsync(
        TimeSpan gracePeriod,
        CancellationToken cancellationToken = default)
    {
        if (_useKeyVault && _secretClient != null)
        {
            try
            {
                var previousVersions = new List<string>();
                var cutoffTime = DateTimeOffset.UtcNow - gracePeriod;
                var secretName = GetSecretName(_options.KeyVersion);

                await foreach (var secretVersion in _secretClient.GetPropertiesOfSecretVersionsAsync(
                    secretName, cancellationToken))
                {
                    if (secretVersion.Enabled == true &&
                        secretVersion.CreatedOn.HasValue &&
                        secretVersion.CreatedOn.Value >= cutoffTime &&
                        !string.IsNullOrEmpty(secretVersion.Version) &&
                        secretVersion.Version != _options.KeyVersion)
                    {
                        previousVersions.Add(secretVersion.Version);
                    }
                }

                _logger.LogDebug(
                    "Found {Count} previous key versions from Key Vault within grace period",
                    previousVersions.Count);

                return previousVersions;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to retrieve previous key versions from Key Vault. Falling back to cache");
            }
        }

        // Fallback to cached versions
        var cutoffTime2 = DateTimeOffset.UtcNow - gracePeriod;
        var cachedVersions = _keyCache
            .Where(kvp => kvp.Value.CachedAt >= cutoffTime2 && kvp.Key != _options.KeyVersion)
            .Select(kvp => kvp.Key)
            .ToList();

        _logger.LogDebug(
            "Found {Count} previous key versions from cache within grace period",
            cachedVersions.Count);

        return await ValueTask.FromResult<IReadOnlyList<string>>(cachedVersions);
    }

    /// <summary>
    /// Loads the encryption key from Azure Key Vault.
    /// </summary>
    private async ValueTask<byte[]> LoadKeyFromKeyVaultAsync(string keyVersion, CancellationToken cancellationToken)
    {
        if (_useKeyVault && _secretClient != null)
        {
            try
            {
                var secretName = GetSecretName(keyVersion);
                _logger.LogDebug("Retrieving secret '{SecretName}' from Key Vault", secretName);

                var secret = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);

                if (string.IsNullOrWhiteSpace(secret?.Value?.Value))
                {
                    throw new EncryptionException(
                        $"Secret '{secretName}' retrieved from Key Vault is empty");
                }

                var key = Convert.FromBase64String(secret.Value.Value);

                if (key.Length != 32)
                {
                    throw new EncryptionException(
                        $"Invalid key size for version {keyVersion}. Expected 32 bytes (256 bits), got {key.Length} bytes.");
                }

                _logger.LogInformation(
                    "Successfully loaded encryption key version {KeyVersion} from Key Vault",
                    keyVersion);

                return key;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning(
                    "Encryption key not found in Key Vault for version {KeyVersion}. Falling back to environment variables",
                    keyVersion);
            }
            catch (Exception ex) when (ex is not EncryptionException)
            {
                _logger.LogWarning(ex,
                    "Failed to load key from Key Vault for version {KeyVersion}. Falling back to environment variables. Error: {Error}",
                    keyVersion, ex.Message);
            }
        }

        // Fallback to environment variables
        return await LoadKeyFromEnvironmentAsync(keyVersion);
    }

    /// <summary>
    /// Loads the encryption key from environment variables as a fallback.
    /// </summary>
    private async ValueTask<byte[]> LoadKeyFromEnvironmentAsync(string keyVersion)
    {
        var envVarName = $"RELAY_ENCRYPTION_KEY_{keyVersion.ToUpperInvariant().Replace(".", "_").Replace("-", "_")}";
        var keyValue = Environment.GetEnvironmentVariable(envVarName);

        if (string.IsNullOrWhiteSpace(keyValue) && keyVersion == _options.KeyVersion)
        {
            keyValue = Environment.GetEnvironmentVariable("RELAY_ENCRYPTION_KEY");
        }

        if (string.IsNullOrWhiteSpace(keyValue) && keyVersion == _options.KeyVersion)
        {
            keyValue = _options.EncryptionKey;
        }

        if (string.IsNullOrWhiteSpace(keyValue))
        {
            throw new EncryptionException(
                $"Encryption key not found for version {keyVersion} in Key Vault or environment variables.");
        }

        try
        {
            var key = Convert.FromBase64String(keyValue);

            if (key.Length != 32)
            {
                throw new EncryptionException(
                    $"Invalid key size for version {keyVersion}. Expected 32 bytes (256 bits), got {key.Length} bytes.");
            }

            _logger.LogDebug(
                "Loaded encryption key version {KeyVersion} from environment variables",
                keyVersion);

            return await ValueTask.FromResult(key);
        }
        catch (FormatException ex)
        {
            throw new EncryptionException(
                $"Invalid base64 format for encryption key version {keyVersion}", ex);
        }
    }

    /// <summary>
    /// Gets the secret name for a given key version.
    /// Convention: relay-encryption-key-{version}
    /// </summary>
    private static string GetSecretName(string keyVersion)
    {
        // Azure Key Vault secret names only allow alphanumeric and hyphens
        var sanitizedVersion = keyVersion.Replace(".", "-").Replace("_", "-");
        return $"relay-encryption-key-{sanitizedVersion}";
    }

    /// <summary>
    /// Represents a cached encryption key.
    /// </summary>
    private class CachedKey
    {
        public byte[] Key { get; set; } = Array.Empty<byte>();
        public string Version { get; set; } = string.Empty;
        public DateTimeOffset CachedAt { get; set; }
    }
}
