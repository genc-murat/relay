using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core;
using Relay.Core.ContractValidation;
using Relay.Core.Telemetry;
using Relay.MessageBroker.Compression;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.MessageBroker;

/// <summary>
/// Base class for message broker implementations providing common functionality.
/// </summary>
public abstract class BaseMessageBroker : IMessageBroker, IAsyncDisposable
{
    protected readonly MessageBrokerOptions _options;
    protected readonly ILogger _logger;
    protected readonly ConcurrentDictionary<Type, List<SubscriptionInfo>> _subscriptions = new();
    protected readonly IMessageCompressor? _compressor;
    protected readonly Relay.Core.Telemetry.MessageBrokerTelemetryAdapter _telemetry;
    protected readonly Relay.Core.Telemetry.MessageBrokerValidationAdapter? _validation;
    
    private bool _isStarted;
    private bool _disposed;

    protected BaseMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger logger,
        IMessageCompressor? compressor = null,
        IContractValidator? contractValidator = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _compressor = compressor;

        // Initialize unified telemetry
        var telemetryOptions = options.Value.Telemetry ?? new RelayTelemetryOptions
        {
            Component = RelayTelemetryConstants.Components.MessageBroker
        };
        var telemetryOptionsWrapper = Options.Create(telemetryOptions);
        _telemetry = new Relay.Core.Telemetry.MessageBrokerTelemetryAdapter(telemetryOptionsWrapper, logger);

        // Initialize validation adapter if contract validator is available
        if (contractValidator != null)
        {
            var validationLogger = logger as ILogger<MessageBrokerValidationAdapter> ?? 
                Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageBrokerValidationAdapter>.Instance;
            _validation = new Relay.Core.Telemetry.MessageBrokerValidationAdapter(
                contractValidator, 
                validationLogger, 
                telemetryOptionsWrapper);
        }
    }

    /// <summary>
    /// Gets whether the message broker has been started.
    /// </summary>
    protected bool IsStarted => _isStarted;

    /// <summary>
    /// Gets whether the message broker has been disposed.
    /// </summary>
    protected bool IsDisposed => _disposed;

    /// <summary>
    /// Publishes a message to the message broker.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">Optional publishing options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public virtual async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Activity tracking will be handled by the adapter internally

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await EnsureStartedAsync(cancellationToken);
            
            // Validate message if validation adapter is available
            if (_validation != null && options?.Validator != null)
            {
                var isValid = await _validation.ValidateMessageAsync(message, options.Validator, cancellationToken);
                if (!isValid)
                {
                    throw new InvalidOperationException($"Message validation failed for type {typeof(TMessage).Name}");
                }
            }
            
            // Validate against schema if provided
            if (_validation != null && options?.Schema != null)
            {
                // Convert schema object to JsonSchemaContract if needed
                JsonSchemaContract? schemaContract = options.Schema as JsonSchemaContract;

                if (schemaContract == null && options.Schema is string schemaString)
                {
                    schemaContract = new JsonSchemaContract { Schema = schemaString };
                }

                if (schemaContract != null && !string.IsNullOrWhiteSpace(schemaContract.Schema))
                {
                    var schemaErrors = await _validation.ValidateMessageSchemaAsync(message, schemaContract, cancellationToken);
                    if (schemaErrors.Any())
                    {
                        var errorMessage = $"Message schema validation failed: {string.Join(", ", schemaErrors)}";
                        throw new InvalidOperationException(errorMessage);
                    }
                }
            }
            
            var serializedMessage = SerializeMessage(message);
            var compressedMessage = await CompressMessageAsync(serializedMessage, cancellationToken);
            
            await PublishInternalAsync(message, compressedMessage, options, cancellationToken);
            
            _telemetry.RecordMessagePublished(typeof(TMessage).Name, compressedMessage.Length, true);
            
            _logger.LogDebug(
                "Published message of type {MessageType} via {BrokerType}",
                typeof(TMessage).Name,
                GetType().Name);
        }
        catch (Exception ex)
        {
            _telemetry.RecordError(ex.GetType().Name, ex.Message);
            _logger.LogError(ex, "Failed to publish message of type {MessageType}", typeof(TMessage).Name);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Subscribes to messages of a specific type.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="handler">The handler to process messages.</param>
    /// <param name="options">Optional subscription options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public virtual async ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var messageType = typeof(TMessage);
        var subscriptionInfo = new SubscriptionInfo
        {
            MessageType = messageType,
            Handler = async (msg, ctx, ct) => await handler((TMessage)msg, ctx, ct),
            Options = options ?? new SubscriptionOptions()
        };

        _subscriptions.AddOrUpdate(
            messageType,
            _ => new List<SubscriptionInfo> { subscriptionInfo },
            (_, list) =>
            {
                list.Add(subscriptionInfo);
                return list;
            });

        await EnsureStartedAsync(cancellationToken);
        await SubscribeInternalAsync(messageType, subscriptionInfo, cancellationToken);

        _logger.LogDebug(
            "Subscribed to messages of type {MessageType} via {BrokerType}",
            messageType.Name,
            GetType().Name);
    }

    /// <summary>
    /// Starts consuming messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public virtual async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted)
            return;

        _logger.LogInformation("Starting {BrokerType} message broker", GetType().Name);
        
        await StartInternalAsync(cancellationToken);
        _isStarted = true;
        
        _logger.LogInformation("{BrokerType} message broker started successfully", GetType().Name);
    }

    /// <summary>
    /// Stops consuming messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public virtual async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted)
            return;

        _logger.LogInformation("Stopping {BrokerType} message broker", GetType().Name);
        
        await StopInternalAsync(cancellationToken);
        _isStarted = false;
        
        _logger.LogInformation("{BrokerType} message broker stopped successfully", GetType().Name);
    }

    /// <summary>
    /// Serializes a message to JSON.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to serialize.</param>
    /// <returns>The serialized message as a byte array.</returns>
    protected virtual byte[] SerializeMessage<TMessage>(TMessage message)
    {
        return JsonSerializer.SerializeToUtf8Bytes(message);
    }

    /// <summary>
    /// Deserializes a message from JSON.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="data">The message data to deserialize.</param>
    /// <returns>The deserialized message.</returns>
    protected virtual TMessage? DeserializeMessage<TMessage>(byte[] data)
    {
        return JsonSerializer.Deserialize<TMessage>(data);
    }

    /// <summary>
    /// Compresses a message if compression is enabled.
    /// </summary>
    /// <param name="data">The message data to compress.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The compressed message data.</returns>
    protected virtual async ValueTask<byte[]> CompressMessageAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (_compressor == null || !_options.Compression?.Enabled == true)
            return data;

        return await _compressor.CompressAsync(data, cancellationToken) ?? data;
    }

    /// <summary>
    /// Decompresses a message if compression is enabled.
    /// </summary>
    /// <param name="data">The message data to decompress.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decompressed message data.</returns>
    protected virtual async ValueTask<byte[]> DecompressMessageAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (_compressor == null || !_options.Compression?.Enabled == true)
            return data;

        return await _compressor.DecompressAsync(data, cancellationToken) ?? data;
    }

    /// <summary>
    /// Processes a received message with the appropriate handlers.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <param name="messageType">The type of the message.</param>
    /// <param name="context">The message context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the processing operation.</returns>
    protected virtual async ValueTask ProcessMessageAsync(
        object message,
        Type messageType,
        MessageContext context,
        CancellationToken cancellationToken = default)
    {
        // Activity tracking will be handled by the adapter internally

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (_subscriptions.TryGetValue(messageType, out var subscriptions))
            {
                 var tasks = subscriptions.Select(async subscription =>
                 {
                     try
                     {
                         await subscription.Handler(message, context, cancellationToken);
                     }
                     catch (Exception ex)
                     {
                         _logger.LogError(ex,
                             "Handler failed to process message of type {MessageType}",
                             messageType.Name);
                         // Don't re-throw to allow other handlers to continue processing
                     }
                 });

                await Task.WhenAll(tasks);
            }

            _telemetry.RecordProcessingDuration(messageType.Name, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _telemetry.RecordError(ex.GetType().Name, ex.Message);
            _logger.LogError(ex, "Failed to process message of type {MessageType}", messageType.Name);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Ensures the message broker is started before performing operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async ValueTask EnsureStartedAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted)
        {
            await StartAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Implementation-specific publish logic.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="serializedMessage">The serialized message data.</param>
    /// <param name="options">Publishing options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the publish operation.</returns>
    protected abstract ValueTask PublishInternalAsync<TMessage>(
        TMessage message,
        byte[] serializedMessage,
        PublishOptions? options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Implementation-specific subscribe logic.
    /// </summary>
    /// <param name="messageType">The message type to subscribe to.</param>
    /// <param name="subscriptionInfo">The subscription information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the subscribe operation.</returns>
    protected abstract ValueTask SubscribeInternalAsync(
        Type messageType,
        SubscriptionInfo subscriptionInfo,
        CancellationToken cancellationToken);

    /// <summary>
    /// Implementation-specific start logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the start operation.</returns>
    protected abstract ValueTask StartInternalAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Implementation-specific stop logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the stop operation.</returns>
    protected abstract ValueTask StopInternalAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Implementation-specific dispose logic.
    /// </summary>
    /// <returns>A task representing the dispose operation.</returns>
    protected abstract ValueTask DisposeInternalAsync();

    /// <summary>
    /// Disposes the message broker.
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await StopAsync();
        await DisposeInternalAsync();
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
