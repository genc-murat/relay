using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Relay.MessageBroker.RedisStreams;

/// <summary>
/// Redis Streams implementation of message broker.
/// </summary>
public sealed class RedisStreamsMessageBroker : IMessageBroker, IAsyncDisposable
{
    private readonly MessageBrokerOptions _options;
    private readonly ILogger<RedisStreamsMessageBroker>? _logger;
    private readonly Dictionary<Type, List<Func<object, MessageContext, CancellationToken, ValueTask>>> _handlers = new();
    private ConnectionMultiplexer? _redis;
    private IDatabase? _database;
    private bool _isStarted;
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;

    public RedisStreamsMessageBroker(
        MessageBrokerOptions options,
        ILogger<RedisStreamsMessageBroker>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        
        if (_options.RedisStreams == null)
            throw new InvalidOperationException("Redis Streams options are required.");
        
        if (string.IsNullOrWhiteSpace(_options.RedisStreams.ConnectionString))
            throw new InvalidOperationException("Redis connection string is required.");
    }

    public async ValueTask PublishAsync<TMessage>(TMessage message, PublishOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        try
        {
            await EnsureConnectedAsync();

            var streamName = options?.RoutingKey ?? _options.RedisStreams!.DefaultStreamName ?? "relay:stream";
            var messageBody = JsonSerializer.Serialize(message);
            var messageType = typeof(TMessage).FullName ?? typeof(TMessage).Name;

            var entries = new List<NameValueEntry>
            {
                new NameValueEntry("type", messageType),
                new NameValueEntry("data", messageBody),
                new NameValueEntry("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString())
            };

            if (options?.Headers != null)
            {
                foreach (var header in options.Headers)
                {
                    entries.Add(new NameValueEntry($"header:{header.Key}", header.Value?.ToString() ?? string.Empty));
                }
            }

            var maxLength = _options.RedisStreams.MaxStreamLength > 0 
                ? _options.RedisStreams.MaxStreamLength 
                : (int?)null;

            await _database!.StreamAddAsync(
                streamName, 
                entries.ToArray(),
                maxLength: maxLength,
                useApproximateMaxLength: true);

            _logger?.LogDebug("Published message {MessageType} to Redis stream {StreamName}", 
                typeof(TMessage).Name, streamName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error publishing message {MessageType}", typeof(TMessage).Name);
            throw;
        }
    }

    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var messageType = typeof(TMessage);
        
        if (!_handlers.ContainsKey(messageType))
        {
            _handlers[messageType] = new List<Func<object, MessageContext, CancellationToken, ValueTask>>();
        }

        _handlers[messageType].Add(async (msg, ctx, ct) => await handler((TMessage)msg, ctx, ct));
        
        _logger?.LogDebug("Subscribed to message type {MessageType}", typeof(TMessage).Name);

        return ValueTask.CompletedTask;
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted) return;

        try
        {
            await EnsureConnectedAsync();

            var streamName = _options.RedisStreams!.DefaultStreamName ?? "relay:stream";
            var groupName = _options.RedisStreams.ConsumerGroupName ?? "relay-consumer-group";
            var consumerName = _options.RedisStreams.ConsumerName ?? $"relay-consumer-{Environment.MachineName}";

            // Create consumer group if not exists
            if (_options.RedisStreams.CreateConsumerGroupIfNotExists)
            {
                try
                {
                    await _database!.StreamCreateConsumerGroupAsync(streamName, groupName, StreamPosition.NewMessages);
                    _logger?.LogInformation("Created Redis consumer group {GroupName} for stream {StreamName}", 
                        groupName, streamName);
                }
                catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
                {
                    _logger?.LogDebug("Redis consumer group {GroupName} already exists", groupName);
                }
            }

            _pollingCts = new CancellationTokenSource();
            
            _pollingTask = Task.Run(async () =>
            {
                _logger?.LogInformation("Starting Redis Streams consumer for stream {StreamName}", streamName);

                while (!_pollingCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var messages = await _database!.StreamReadGroupAsync(
                            streamName,
                            groupName,
                            consumerName,
                            StreamPosition.NewMessages,
                            count: _options.RedisStreams.MaxMessagesToRead);

                        foreach (var message in messages)
                        {
                            await ProcessMessageAsync(streamName, groupName, message, _pollingCts.Token);
                        }

                        if (messages.Length == 0)
                        {
                            await Task.Delay(_options.RedisStreams.ReadTimeout, _pollingCts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error in Redis Streams polling loop");
                        await Task.Delay(TimeSpan.FromSeconds(5), _pollingCts.Token);
                    }
                }
            }, _pollingCts.Token);

            _isStarted = true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting Redis Streams message broker");
            throw;
        }
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted) return;

        try
        {
            _pollingCts?.Cancel();
            
            if (_pollingTask != null)
            {
                await _pollingTask;
            }
            
            _pollingCts?.Dispose();
            _pollingCts = null;
            _pollingTask = null;
            
            _isStarted = false;
            _logger?.LogInformation("Redis Streams message broker stopped");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping Redis Streams message broker");
            throw;
        }
    }

    private async Task ProcessMessageAsync(
        string streamName, 
        string groupName, 
        StreamEntry message, 
        CancellationToken cancellationToken)
    {
        try
        {
            var entries = message.Values.ToDictionary(nv => nv.Name.ToString(), nv => nv.Value.ToString());

            if (!entries.TryGetValue("type", out var messageType))
            {
                _logger?.LogWarning("Message missing 'type' field, message ID: {MessageId}", message.Id);
                await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                return;
            }

            var type = Type.GetType(messageType);

            if (type == null || !_handlers.ContainsKey(type))
            {
                _logger?.LogWarning("No handler found for message type {MessageType}", messageType);
                await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                return;
            }

            if (!entries.TryGetValue("data", out var messageData))
            {
                _logger?.LogWarning("Message missing 'data' field, message ID: {MessageId}", message.Id);
                await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                return;
            }

            var deserializedMessage = JsonSerializer.Deserialize(messageData, type);

            if (deserializedMessage == null)
            {
                _logger?.LogWarning("Failed to deserialize message of type {MessageType}", messageType);
                await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                return;
            }

            var headers = entries
                .Where(kvp => kvp.Key.StartsWith("header:"))
                .ToDictionary(
                    kvp => kvp.Key.Substring(7), 
                    kvp => (object)kvp.Value);

            var timestamp = entries.TryGetValue("timestamp", out var timestampStr) && 
                           long.TryParse(timestampStr, out var timestampMs)
                ? DateTimeOffset.FromUnixTimeMilliseconds(timestampMs)
                : DateTimeOffset.UtcNow;

            var context = new MessageContext
            {
                MessageId = message.Id.ToString(),
                Timestamp = timestamp,
                Headers = headers.Count > 0 ? headers : null,
                Acknowledge = async () => 
                    await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken),
                Reject = async (requeue) =>
                {
                    // Redis Streams doesn't support rejection with requeue
                    // We simply acknowledge to remove from pending
                    await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                }
            };

            var handlers = _handlers[type];
            foreach (var handler in handlers)
            {
                await handler(deserializedMessage, context, cancellationToken);
            }

            if (_options.RedisStreams!.AutoAcknowledge && context.Acknowledge != null)
            {
                await context.Acknowledge();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing Redis Streams message {MessageId}", message.Id);
        }
    }

    private async Task AcknowledgeMessageAsync(
        string streamName, 
        string groupName, 
        RedisValue messageId, 
        CancellationToken cancellationToken)
    {
        try
        {
            await _database!.StreamAcknowledgeAsync(streamName, groupName, messageId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error acknowledging Redis Streams message {MessageId}", messageId);
        }
    }

    private async Task EnsureConnectedAsync()
    {
        if (_redis == null || !_redis.IsConnected)
        {
            var configOptions = ConfigurationOptions.Parse(_options.RedisStreams!.ConnectionString!);
            
            if (_options.RedisStreams.ConnectTimeout.HasValue)
            {
                configOptions.ConnectTimeout = (int)_options.RedisStreams.ConnectTimeout.Value.TotalMilliseconds;
            }

            if (_options.RedisStreams.SyncTimeout.HasValue)
            {
                configOptions.SyncTimeout = (int)_options.RedisStreams.SyncTimeout.Value.TotalMilliseconds;
            }

            _redis = await ConnectionMultiplexer.ConnectAsync(configOptions);
            _database = _redis.GetDatabase(_options.RedisStreams.Database);
            
            _logger?.LogInformation("Connected to Redis server");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();

        if (_redis != null)
        {
            await _redis.DisposeAsync();
        }
    }
}
