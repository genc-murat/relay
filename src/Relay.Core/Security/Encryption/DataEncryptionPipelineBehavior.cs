using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Security.Interfaces;

namespace Relay.Core.Security.Encryption;

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
