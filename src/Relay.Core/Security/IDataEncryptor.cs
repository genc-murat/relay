namespace Relay.Core.Security;

/// <summary>
/// Interface for data encryption/decryption operations.
/// </summary>
public interface IDataEncryptor
{
    string Encrypt(string plainText, string keyId = "default");
    string Decrypt(string cipherText, string keyId = "default");
}
