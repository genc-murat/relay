using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Relay.MessageBroker.Security;

/// <summary>
/// Key provider that loads encryption keys from environment variables.
/// </summary>
public class EnvironmentVariableKeyProvider : IKeyProvider
{
    private readonly SecurityOptions _options;
    private readonly ILogger<EnvironmentVariableKeyProvider> _logger;
    private readonly ConcurrentDictionary<string, CachedKey> _keyCache;
    private readonly TimeSpan _cacheRefreshInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableKeyProvider"/> class.
    /// </summary>
    /// <param name="options">The security options.</param>
    /// <param name="logger">The logger.</param>
    public EnvironmentVariableKeyProvider(
        IOptions<SecurityOptions> options,
        ILogger<EnvironmentVariableKeyProvider> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyCache = new ConcurrentDictionary<string, CachedKey>();

        _logger.LogInformation("EnvironmentVariableKeyProvider initialized");
    }

    /// <inheritdoc/>
    public ValueTask<byte[]> GetKeyAsync(string keyVersion, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyVersion);

        // Check cache first
        if (_keyCache.TryGetValue(keyVersion, out var cachedKey))
        {
            if (DateTimeOffset.UtcNow - cachedKey.CachedAt < _cacheRefreshInterval)
            {
                _logger.LogTrace("Retrieved key version {KeyVersion} from cache", keyVersion);
                // Return a copy of the cached key to prevent reference sharing issues
                var cachedKeyCopy = new byte[cachedKey.Key.Length];
                Array.Copy(cachedKey.Key, cachedKeyCopy, cachedKey.Key.Length);
                return ValueTask.FromResult(cachedKeyCopy);
            }

            // Cache expired, remove it
            _keyCache.TryRemove(keyVersion, out _);
        }

        // Load key from environment variable or options
        var key = LoadKeyFromEnvironment(keyVersion);

        // Create a copy of the key to avoid potential reference sharing issues
        var keyCopy = new byte[key.Length];
        Array.Copy(key, keyCopy, key.Length);

        // Cache the key
        _keyCache[keyVersion] = new CachedKey
        {
            Key = keyCopy,
            CachedAt = DateTimeOffset.UtcNow
        };

        _logger.LogDebug("Loaded and cached key version {KeyVersion}", keyVersion);

        return ValueTask.FromResult(keyCopy);
    }

    /// <inheritdoc/>
    public ValueTask<IReadOnlyList<string>> GetPreviousKeyVersionsAsync(
        TimeSpan gracePeriod,
        CancellationToken cancellationToken = default)
    {
        // Look for environment variables with pattern: RELAY_ENCRYPTION_KEY_{VERSION}
        var previousVersions = new List<string>();

        // Check for common previous version patterns
        var currentVersion = _options.KeyVersion;
        var versionPatterns = GeneratePreviousVersionPatterns(currentVersion);

        foreach (var version in versionPatterns)
        {
            var envVarName = $"RELAY_ENCRYPTION_KEY_{version.ToUpperInvariant().Replace(".", "_")}";
            var keyValue = Environment.GetEnvironmentVariable(envVarName);

            if (!string.IsNullOrWhiteSpace(keyValue))
            {
                previousVersions.Add(version);
                _logger.LogDebug("Found previous key version {KeyVersion} in environment", version);
            }
        }

        return ValueTask.FromResult<IReadOnlyList<string>>(previousVersions);
    }

    /// <summary>
    /// Loads the encryption key from environment variables or options.
    /// </summary>
    private byte[] LoadKeyFromEnvironment(string keyVersion)
    {
        // First, try to load from environment variable with version-specific name
        var envVarName = $"RELAY_ENCRYPTION_KEY_{keyVersion.ToUpperInvariant().Replace(".", "_")}";
        var keyValue = Environment.GetEnvironmentVariable(envVarName);

        // If not found and this is the current version, try the generic environment variable
        if (string.IsNullOrWhiteSpace(keyValue) && keyVersion == _options.KeyVersion)
        {
            keyValue = Environment.GetEnvironmentVariable("RELAY_ENCRYPTION_KEY");
        }

        // If still not found and this is the current version, use the key from options
        if (string.IsNullOrWhiteSpace(keyValue) && keyVersion == _options.KeyVersion)
        {
            keyValue = _options.EncryptionKey;
        }

        if (string.IsNullOrWhiteSpace(keyValue))
        {
            throw new EncryptionException(
                $"Encryption key not found for version {keyVersion}. " +
                $"Set environment variable {envVarName} or configure EncryptionKey in options.");
        }

        try
        {
            // Decode base64 key
            var key = Convert.FromBase64String(keyValue);

            // Validate key size (256 bits = 32 bytes for AES-256)
            if (key.Length != 32)
            {
                throw new EncryptionException(
                    $"Invalid key size for version {keyVersion}. Expected 32 bytes (256 bits), got {key.Length} bytes.");
            }

            return key;
        }
        catch (FormatException ex)
        {
            throw new EncryptionException(
                $"Invalid base64 format for encryption key version {keyVersion}", ex);
        }
    }

    /// <summary>
    /// Generates possible previous version patterns based on the current version.
    /// </summary>
    private List<string> GeneratePreviousVersionPatterns(string currentVersion)
    {
        var patterns = new List<string>();

        // Try to parse version number (e.g., "v1", "v2", "1.0", "2.0")
        if (currentVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            var versionNumber = currentVersion[1..];
            if (int.TryParse(versionNumber, out var version) && version > 1)
            {
                // Add previous versions (e.g., v2 -> v1)
                for (int i = version - 1; i >= 1; i--)
                {
                    patterns.Add($"v{i}");
                }
            }
        }
        else if (currentVersion.Contains('.'))
        {
            // Handle semantic versioning (e.g., "2.0" -> "1.0")
            var parts = currentVersion.Split('.');
            if (parts.Length >= 2 && int.TryParse(parts[0], out var major) && major > 1)
            {
                for (int i = major - 1; i >= 1; i--)
                {
                    patterns.Add($"{i}.0");
                }
            }
        }

        return patterns;
    }

    /// <summary>
    /// Represents a cached encryption key.
    /// </summary>
    private class CachedKey
    {
        public byte[] Key { get; set; } = Array.Empty<byte>();
        public DateTimeOffset CachedAt { get; set; }
    }
}
