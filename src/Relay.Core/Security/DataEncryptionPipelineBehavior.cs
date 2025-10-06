using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Security
{
    /// <summary>
    /// Pipeline behavior for automatic encryption/decryption of sensitive data.
    /// </summary>
    public class DataEncryptionPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<DataEncryptionPipelineBehavior<TRequest, TResponse>> _logger;
        private readonly IDataEncryptor _encryptor;

        public DataEncryptionPipelineBehavior(
            ILogger<DataEncryptionPipelineBehavior<TRequest, TResponse>> logger,
            IDataEncryptor encryptor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
        }

        public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Decrypt incoming request data
            DecryptSensitiveData(request);

            var response = await next();

            // Encrypt outgoing response data
            if (response is not null)
            {
                EncryptSensitiveData(response);
            }

            return response;
        }

        private void DecryptSensitiveData(object obj)
        {
            if (obj == null) return;

            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var encryptAttribute = property.GetCustomAttribute<EncryptedAttribute>();
                if (encryptAttribute != null && property.PropertyType == typeof(string))
                {
                    var encryptedValue = property.GetValue(obj) as string;
                    if (!string.IsNullOrWhiteSpace(encryptedValue))
                    {
                        try
                        {
                            var decryptedValue = _encryptor.Decrypt(encryptedValue);
                            property.SetValue(obj, decryptedValue);
                            _logger.LogDebug("Decrypted property {PropertyName} on {TypeName}",
                                property.Name, type.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to decrypt property {PropertyName} on {TypeName}",
                                property.Name, type.Name);
                            throw new DataDecryptionException(property.Name, ex);
                        }
                    }
                }
            }
        }

        private void EncryptSensitiveData(object obj)
        {
            if (obj == null) return;

            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var encryptAttribute = property.GetCustomAttribute<EncryptedAttribute>();
                if (encryptAttribute != null && property.PropertyType == typeof(string))
                {
                    var plainValue = property.GetValue(obj) as string;
                    if (!string.IsNullOrWhiteSpace(plainValue))
                    {
                        try
                        {
                            var encryptedValue = _encryptor.Encrypt(plainValue);
                            property.SetValue(obj, encryptedValue);
                            _logger.LogDebug("Encrypted property {PropertyName} on {TypeName}",
                                property.Name, type.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to encrypt property {PropertyName} on {TypeName}",
                                property.Name, type.Name);
                            throw new DataEncryptionException(property.Name, ex);
                        }
                    }
                }
            }
        }
    }

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

    /// <summary>
    /// Interface for data encryption/decryption operations.
    /// </summary>
    public interface IDataEncryptor
    {
        string Encrypt(string plainText, string keyId = "default");
        string Decrypt(string cipherText, string keyId = "default");
    }

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
}