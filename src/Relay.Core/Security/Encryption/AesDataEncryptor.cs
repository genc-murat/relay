using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using Relay.Core.Security.Interfaces;

namespace Relay.Core.Security.Encryption;

/// <summary>
/// AES-based data encryptor implementation.
/// </summary>
public class AesDataEncryptor : IDataEncryptor
{
    private readonly byte[] _key;
    private readonly ILogger<AesDataEncryptor> _logger;

    public AesDataEncryptor(ILogger<AesDataEncryptor> logger, string base64Key)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _key = Convert.FromBase64String(base64Key ?? throw new ArgumentNullException(nameof(base64Key)));

        if (_key.Length != 32) // 256 bits
        {
            throw new ArgumentException("Key must be 256 bits (32 bytes) for AES-256");
        }
    }

    public string Encrypt(string plainText, string keyId = "default")
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to cipher text
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
        Array.Copy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText, string keyId = "default")
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return cipherText;

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        // Extract IV from the beginning
        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
