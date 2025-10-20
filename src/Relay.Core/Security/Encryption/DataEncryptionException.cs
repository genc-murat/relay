using System;

namespace Relay.Core.Security.Encryption;

/// <summary>
/// Exception thrown when data encryption fails.
/// </summary>
public class DataEncryptionException : Exception
{
    public string PropertyName { get; }

    public DataEncryptionException(string propertyName, Exception innerException)
        : base($"Failed to encrypt property '{propertyName}'", innerException)
    {
        PropertyName = propertyName;
    }
}
