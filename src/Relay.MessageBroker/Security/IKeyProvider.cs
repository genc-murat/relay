namespace Relay.MessageBroker.Security;

/// <summary>
/// Interface for providing encryption keys.
/// </summary>
public interface IKeyProvider
{
    /// <summary>
    /// Gets the encryption key for the specified version.
    /// </summary>
    /// <param name="keyVersion">The key version to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The encryption key as a byte array.</returns>
    ValueTask<byte[]> GetKeyAsync(string keyVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of previous key versions that are still valid within the grace period.
    /// </summary>
    /// <param name="gracePeriod">The grace period for key rotation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of previous key versions.</returns>
    ValueTask<IReadOnlyList<string>> GetPreviousKeyVersionsAsync(
        TimeSpan gracePeriod,
        CancellationToken cancellationToken = default);
}
