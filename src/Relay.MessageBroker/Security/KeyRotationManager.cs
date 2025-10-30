using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Relay.MessageBroker.Security;

/// <summary>
/// Manages key rotation and tracks key version metadata.
/// </summary>
public class KeyRotationManager
{
    private readonly SecurityOptions _options;
    private readonly ILogger<KeyRotationManager> _logger;
    private readonly ConcurrentDictionary<string, KeyVersionMetadata> _keyMetadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyRotationManager"/> class.
    /// </summary>
    /// <param name="options">The security options.</param>
    /// <param name="logger">The logger.</param>
    public KeyRotationManager(
        IOptions<SecurityOptions> options,
        ILogger<KeyRotationManager> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyMetadata = new ConcurrentDictionary<string, KeyVersionMetadata>();

        // Register current key version
        RegisterKeyVersion(_options.KeyVersion, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Registers a key version with its activation timestamp.
    /// </summary>
    /// <param name="keyVersion">The key version to register.</param>
    /// <param name="activatedAt">The timestamp when the key was activated.</param>
    public void RegisterKeyVersion(string keyVersion, DateTimeOffset activatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyVersion);

        var metadata = new KeyVersionMetadata
        {
            Version = keyVersion,
            ActivatedAt = activatedAt,
            IsActive = keyVersion == _options.KeyVersion
        };

        _keyMetadata.AddOrUpdate(keyVersion, metadata, (_, _) => metadata);

        _logger.LogInformation(
            "Registered key version {KeyVersion}, activated at {ActivatedAt}, active: {IsActive}",
            keyVersion,
            activatedAt,
            metadata.IsActive);
    }

    /// <summary>
    /// Checks if a key version is still valid within the grace period.
    /// </summary>
    /// <param name="keyVersion">The key version to check.</param>
    /// <returns>True if the key version is valid, false otherwise.</returns>
    public bool IsKeyVersionValid(string keyVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyVersion);

        // Current key version is always valid
        if (keyVersion == _options.KeyVersion)
        {
            return true;
        }

        // Check if key version exists in metadata
        if (!_keyMetadata.TryGetValue(keyVersion, out var metadata))
        {
            return false;
        }

        // Check if within grace period
        var gracePeriodEnd = metadata.ActivatedAt + _options.KeyRotationGracePeriod;
        return DateTimeOffset.UtcNow <= gracePeriodEnd;
    }

    /// <summary>
    /// Gets all valid key versions within the grace period.
    /// </summary>
    /// <returns>A list of valid key versions.</returns>
    public IReadOnlyList<string> GetValidKeyVersions()
    {
        var validVersions = _keyMetadata
            .Where(kvp => IsKeyVersionValid(kvp.Key))
            .Select(kvp => kvp.Key)
            .OrderByDescending(v => v == _options.KeyVersion) // Current version first
            .ToList();

        return validVersions;
    }

    /// <summary>
    /// Gets metadata for a specific key version.
    /// </summary>
    /// <param name="keyVersion">The key version.</param>
    /// <returns>The key version metadata, or null if not found.</returns>
    public KeyVersionMetadata? GetKeyVersionMetadata(string keyVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyVersion);

        return _keyMetadata.TryGetValue(keyVersion, out var metadata) ? metadata : null;
    }

    /// <summary>
    /// Cleans up expired key versions that are outside the grace period.
    /// </summary>
    /// <returns>The number of expired key versions removed.</returns>
    public int CleanupExpiredKeyVersions()
    {
        var expiredVersions = _keyMetadata
            .Where(kvp => !IsKeyVersionValid(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToList();

        var removedCount = 0;
        foreach (var version in expiredVersions)
        {
            if (_keyMetadata.TryRemove(version, out _))
            {
                removedCount++;
                _logger.LogInformation(
                    "Removed expired key version {KeyVersion} from metadata",
                    version);
            }
        }

        return removedCount;
    }
}

/// <summary>
/// Metadata about a key version.
/// </summary>
public class KeyVersionMetadata
{
    /// <summary>
    /// Gets or sets the key version identifier.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the key was activated.
    /// </summary>
    public DateTimeOffset ActivatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the currently active key version.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets the expiration timestamp based on the grace period.
    /// </summary>
    public DateTimeOffset GetExpirationTime(TimeSpan gracePeriod)
    {
        return ActivatedAt + gracePeriod;
    }

    /// <summary>
    /// Checks if the key version has expired based on the grace period.
    /// </summary>
    public bool IsExpired(TimeSpan gracePeriod)
    {
        return DateTimeOffset.UtcNow > GetExpirationTime(gracePeriod);
    }
}
