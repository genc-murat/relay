using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.MessageBroker.Security;

/// <summary>
/// Decorator that adds message encryption capabilities to an IMessageBroker implementation.
/// </summary>
public sealed class EncryptionMessageBrokerDecorator : IMessageBroker, IAsyncDisposable
{
    private readonly IMessageBroker _innerBroker;
    private readonly IMessageEncryptor _encryptor;
    private readonly SecurityOptions _options;
    private readonly ILogger<EncryptionMessageBrokerDecorator> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionMessageBrokerDecorator"/> class.
    /// </summary>
    /// <param name="innerBroker">The inner message broker to decorate.</param>
    /// <param name="encryptor">The message encryptor.</param>
    /// <param name="options">The security options.</param>
    /// <param name="logger">The logger.</param>
    public EncryptionMessageBrokerDecorator(
        IMessageBroker innerBroker,
        IMessageEncryptor encryptor,
        IOptions<SecurityOptions> options,
        ILogger<EncryptionMessageBrokerDecorator> logger)
    {
        _innerBroker = innerBroker ?? throw new ArgumentNullException(nameof(innerBroker));
        _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options.Validate();

        _logger.LogInformation(
            "EncryptionMessageBrokerDecorator initialized. Encryption enabled: {Enabled}, Algorithm: {Algorithm}",
            _options.EnableEncryption,
            _options.EncryptionAlgorithm);
    }

    /// <inheritdoc/>
    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // If encryption is disabled, publish directly
        if (!_options.EnableEncryption)
        {
            await _innerBroker.PublishAsync(message, options, cancellationToken);
            return;
        }

        try
        {
            // Serialize the message to bytes
            var messageBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message);

            _logger.LogTrace(
                "Encrypting message of type {MessageType}, size: {Size} bytes",
                typeof(TMessage).Name,
                messageBytes.Length);

            // Encrypt the message
            var encryptedBytes = await _encryptor.EncryptAsync(messageBytes, cancellationToken);

            _logger.LogTrace(
                "Message encrypted. Original size: {OriginalSize} bytes, Encrypted size: {EncryptedSize} bytes",
                messageBytes.Length,
                encryptedBytes.Length);

            // Add encryption metadata to headers
            var publishOptions = options ?? new PublishOptions();
            publishOptions.Headers ??= new Dictionary<string, object>();

            var metadata = new EncryptionMetadata
            {
                KeyVersion = _encryptor.GetKeyVersion(),
                Algorithm = _options.EncryptionAlgorithm,
                EncryptedAt = DateTimeOffset.UtcNow
            };

            publishOptions.Headers[EncryptionMetadata.KeyVersionHeaderKey] = metadata.KeyVersion;
            publishOptions.Headers[EncryptionMetadata.AlgorithmHeaderKey] = metadata.Algorithm;
            publishOptions.Headers[EncryptionMetadata.EncryptedAtHeaderKey] = metadata.EncryptedAt.ToString("O");

            // Create a wrapper message that contains the encrypted bytes
            var encryptedMessage = new EncryptedMessageWrapper
            {
                EncryptedPayload = encryptedBytes,
                MessageType = typeof(TMessage).FullName ?? typeof(TMessage).Name
            };

            // Publish the encrypted message
            await _innerBroker.PublishAsync(encryptedMessage, publishOptions, cancellationToken);

            _logger.LogDebug(
                "Published encrypted message of type {MessageType} using key version {KeyVersion}",
                typeof(TMessage).Name,
                metadata.KeyVersion);
        }
        catch (EncryptionException ex)
        {
            _logger.LogError(
                ex,
                "Failed to encrypt message of type {MessageType}",
                typeof(TMessage).Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while encrypting and publishing message of type {MessageType}",
                typeof(TMessage).Name);
            throw new EncryptionException(
                $"Failed to encrypt and publish message of type {typeof(TMessage).Name}",
                ex);
        }
    }

    /// <inheritdoc/>
    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // If encryption is disabled, subscribe directly
        if (!_options.EnableEncryption)
        {
            return _innerBroker.SubscribeAsync(handler, options, cancellationToken);
        }

        // Wrap the handler to decrypt messages before processing
        async ValueTask DecryptingHandler(
            EncryptedMessageWrapper encryptedWrapper,
            MessageContext context,
            CancellationToken ct)
        {
            try
            {
                _logger.LogTrace(
                    "Decrypting message of type {MessageType}, size: {Size} bytes",
                    encryptedWrapper.MessageType,
                    encryptedWrapper.EncryptedPayload.Length);

                // Extract encryption metadata from headers
                var keyVersion = context.Headers?.TryGetValue(EncryptionMetadata.KeyVersionHeaderKey, out var kv) == true
                    ? kv?.ToString()
                    : _encryptor.GetKeyVersion();

                // Decrypt the message
                var decryptedBytes = await _encryptor.DecryptAsync(
                    encryptedWrapper.EncryptedPayload,
                    ct);

                _logger.LogTrace(
                    "Message decrypted. Encrypted size: {EncryptedSize} bytes, Decrypted size: {DecryptedSize} bytes",
                    encryptedWrapper.EncryptedPayload.Length,
                    decryptedBytes.Length);

                // Deserialize the decrypted message
                var message = System.Text.Json.JsonSerializer.Deserialize<TMessage>(decryptedBytes);

                if (message == null)
                {
                    throw new EncryptionException(
                        $"Failed to deserialize decrypted message of type {typeof(TMessage).Name}");
                }

                _logger.LogDebug(
                    "Successfully decrypted message of type {MessageType} using key version {KeyVersion}",
                    typeof(TMessage).Name,
                    keyVersion);

                // Call the original handler with the decrypted message
                await handler(message, context, ct);
            }
            catch (EncryptionException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to decrypt message of type {MessageType}",
                    encryptedWrapper.MessageType);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while decrypting message of type {MessageType}",
                    encryptedWrapper.MessageType);
                throw new EncryptionException(
                    $"Failed to decrypt message of type {encryptedWrapper.MessageType}",
                    ex);
            }
        }

        // Subscribe to encrypted messages
        return _innerBroker.SubscribeAsync<EncryptedMessageWrapper>(
            DecryptingHandler,
            options,
            cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        return _innerBroker.StartAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        return _innerBroker.StopAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _logger.LogInformation("Disposing EncryptionMessageBrokerDecorator");

        // Dispose encryptor if it's disposable
        if (_encryptor is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_encryptor is IDisposable disposable)
        {
            disposable.Dispose();
        }

        // Dispose inner broker if it's disposable
        if (_innerBroker is IAsyncDisposable innerAsyncDisposable)
        {
            await innerAsyncDisposable.DisposeAsync();
        }

        _logger.LogInformation("EncryptionMessageBrokerDecorator disposed");
    }
}

/// <summary>
/// Wrapper for encrypted message payloads.
/// </summary>
internal class EncryptedMessageWrapper
{
    /// <summary>
    /// Gets or sets the encrypted payload.
    /// </summary>
    public byte[] EncryptedPayload { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the original message type name.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;
}
