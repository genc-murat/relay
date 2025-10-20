using System;

namespace Relay.Core.Security.Encryption;

/// <summary>
/// Attribute to mark properties for automatic encryption/decryption.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class EncryptedAttribute : Attribute
{
    /// <summary>
    /// Encryption algorithm to use (optional).
    /// </summary>
    public string Algorithm { get; set; } = "AES256";

    /// <summary>
    /// Key identifier for encryption (optional).
    /// </summary>
    public string KeyId { get; set; } = "default";
}
