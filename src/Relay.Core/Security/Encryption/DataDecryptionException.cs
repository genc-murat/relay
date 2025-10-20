using System;

namespace Relay.Core.Security.Encryption;

/// <summary>
/// Exception thrown when data decryption fails.
/// </summary>
public class DataDecryptionException : Exception
{
    public string PropertyName { get; }

    public DataDecryptionException(string propertyName, Exception innerException)
        : base($"Failed to decrypt property '{propertyName}'", innerException)
    {
        PropertyName = propertyName;
    }
}