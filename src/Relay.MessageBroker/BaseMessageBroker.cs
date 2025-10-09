using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.MessageBroker.Compression;
using Relay.MessageBroker.Telemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

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
    protected readonly ActivitySource _activitySource;
    protected readonly Counter<long> _messagesPublishedCounter;
    protected readonly Counter<long> _messagesReceivedCounter;
    protected readonly Counter<long> _messagesProcessedCounter;
    protected readonly Counter<long> _messagesFailedCounter;
    protected readonly Histogram<double> _publishDurationHistogram;
    protected readonly Histogram<double> _processDurationHistogram;
    protected readonly Histogram<long> _messagePayloadSizeHistogram;
    
    private bool _isStarted;
    private bool _disposed;

    protected BaseMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger logger,
        IMessageCompressor? compressor = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _compressor = compressor;

        // Initialize telemetry
        _activitySource = MessageBrokerTelemetry.ActivitySource;
        var meter = new Meter(MessageBrokerTelemetry.MeterName, MessageBrokerTelemetry.MeterVersion);
        
        _messagesPublishedCounter = meter.CreateCounter<long>(
            MessageBrokerTelemetry.Metrics.MessagesPublished,
            description: "Number of messages published");
        
        _messagesReceivedCounter = meter.CreateCounter<long>(
            MessageBrokerTelemetry.Metrics.MessagesReceived,
            description: "Number of messages received");
        
        _messagesProcessedCounter = meter.CreateCounter<long>(
            MessageBrokerTelemetry.Metrics.MessagesProcessed,
            description: "Number of messages processed");
        
        _messagesFailedCounter = meter.CreateCounter<long>(
            MessageBrokerTelemetry.Metrics.MessagesFailed,
            description: "Number of messages failed to process");
        
        _publishDurationHistogram = meter.CreateHistogram<double>(
            MessageBrokerTelemetry.Metrics.MessagePublishDuration,
            description: "Duration of message publish operations");
        
        _processDurationHistogram = meter.CreateHistogram<double>(
            MessageBrokerTelemetry.Metrics.MessageProcessDuration,
            description: "Duration of message processing operations");
        
        _messagePayloadSizeHistogram = meter.CreateHistogram<long>(
            MessageBrokerTelemetry.Metrics.MessagePayloadSize,
            description: "Size of message payloads in bytes");
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

        using var activity = _activitySource.StartActivity("PublishMessage");
        activity?.SetTag("message.type", typeof(TMessage).Name);
        activity?.SetTag("message.broker", GetType().Name);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await EnsureStartedAsync(cancellationToken);
            
            var serializedMessage = SerializeMessage(message);
            var compressedMessage = await CompressMessageAsync(serializedMessage, cancellationToken);
            
            await PublishInternalAsync(message, compressedMessage, options, cancellationToken);
            
            _messagesPublishedCounter.Add(1, new KeyValuePair<string, object?>("message.type", typeof(TMessage).Name), new KeyValuePair<string, object?>("message.broker", GetType().Name));
            _messagePayloadSizeHistogram.Record(compressedMessage.Length, new KeyValuePair<string, object?>("message.type", typeof(TMessage).Name));
            
            _logger.LogDebug(
                "Published message of type {MessageType} via {BrokerType}",
                typeof(TMessage).Name,
                GetType().Name);
        }
        catch (Exception ex)
        {
            _messagesFailedCounter.Add(1, new KeyValuePair<string, object?>("message.type", typeof(TMessage).Name), new KeyValuePair<string, object?>("message.broker", GetType().Name));
            _logger.LogError(ex, "Failed to publish message of type {MessageType}", typeof(TMessage).Name);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _publishDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("message.type", typeof(TMessage).Name));
            activity?.SetTag("publish.duration_ms", stopwatch.ElapsedMilliseconds);
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

        return await _compressor.CompressAsync(data, cancellationToken);
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

        return await _compressor.DecompressAsync(data, cancellationToken);
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
        using var activity = _activitySource.StartActivity("ProcessMessage");
        activity?.SetTag("message.type", messageType.Name);
        activity?.SetTag("message.broker", GetType().Name);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _messagesReceivedCounter.Add(1, new KeyValuePair<string, object?>("message.type", messageType.Name), new KeyValuePair<string, object?>("message.broker", GetType().Name));

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
                        throw;
                    }
                });

                await Task.WhenAll(tasks);
            }

            _messagesProcessedCounter.Add(1, new KeyValuePair<string, object?>("message.type", messageType.Name), new KeyValuePair<string, object?>("message.broker", GetType().Name));
        }
        catch (Exception ex)
        {
            _messagesFailedCounter.Add(1, new KeyValuePair<string, object?>("message.type", messageType.Name), new KeyValuePair<string, object?>("message.broker", GetType().Name));
            _logger.LogError(ex, "Failed to process message of type {MessageType}", messageType.Name);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _processDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("message.type", messageType.Name));
            activity?.SetTag("process.duration_ms", stopwatch.ElapsedMilliseconds);
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

/// <summary>
/// Information about a message subscription.
/// </summary>
public sealed class SubscriptionInfo
{
    public Type MessageType { get; set; } = null!;
    public Func<object, MessageContext, CancellationToken, ValueTask> Handler { get; set; } = null!;
    public SubscriptionOptions Options { get; set; } = null!;
}