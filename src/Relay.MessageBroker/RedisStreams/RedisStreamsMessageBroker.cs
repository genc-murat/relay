using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Polly;
using Polly.Retry;

namespace Relay.MessageBroker.RedisStreams;

/// <summary>
/// Redis Streams implementation of message broker with enhanced enterprise features.
/// </summary>
public sealed class RedisStreamsMessageBroker : IMessageBroker, IAsyncDisposable
{
    private readonly MessageBrokerOptions _options;
    private readonly ILogger<RedisStreamsMessageBroker>? _logger;
    private readonly Dictionary<Type, List<Func<object, MessageContext, CancellationToken, ValueTask>>> _handlers = new();
    private readonly Dictionary<string, CancellationTokenSource> _consumerTasks = new();
    private readonly AsyncRetryPolicy _redisRetryPolicy;
    private ConnectionMultiplexer? _redis;
    private IDatabase? _database;
    private bool _isStarted;
    private CancellationTokenSource? _pollingCts;
    private Task? _pendingEntriesTask;

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

        // Configure retry policy for Redis operations
        _redisRetryPolicy = Policy
            .Handle<RedisConnectionException>()
            .Or<RedisTimeoutException>()
            .Or<RedisServerException>()
            .WaitAndRetryAsync(
                retryCount: _options.RetryPolicy?.MaxAttempts ?? 3,
                sleepDurationProvider: retryAttempt => 
                    TimeSpan.FromMilliseconds(Math.Min(
                        1000 * Math.Pow(2, retryAttempt), 
                        (_options.RetryPolicy?.MaxDelay ?? TimeSpan.FromSeconds(30)).TotalMilliseconds)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    _logger?.LogWarning(outcome, 
                        "Redis operation failed (attempt {RetryAttempt}/{MaxRetries}). Retrying in {Delay}ms...", 
                        retryAttempt, _options.RetryPolicy?.MaxAttempts ?? 3, timespan.TotalMilliseconds);
                });
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
            var messageId = Guid.NewGuid().ToString();

            var entries = new List<NameValueEntry>
            {
                new NameValueEntry("type", messageType),
                new NameValueEntry("data", messageBody),
                new NameValueEntry("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()),
                new NameValueEntry("messageId", messageId)
            };

            // Add correlation ID if provided
            if (options?.Headers?.ContainsKey("CorrelationId") == true)
            {
                entries.Add(new NameValueEntry("correlationId", options.Headers["CorrelationId"].ToString() ?? string.Empty));
            }

            // Add custom headers
            if (options?.Headers != null)
            {
                foreach (var header in options.Headers)
                {
                    if (header.Key != "CorrelationId")
                    {
                        entries.Add(new NameValueEntry($"header:{header.Key}", header.Value?.ToString() ?? string.Empty));
                    }
                }
            }

            // Add priority if specified
            if (options?.Priority.HasValue == true)
            {
                entries.Add(new NameValueEntry("priority", options.Priority.Value.ToString()));
            }

            // Add expiration if specified
            if (options?.Expiration.HasValue == true)
            {
                entries.Add(new NameValueEntry("expiration", options.Expiration.Value.TotalMilliseconds.ToString()));
            }

            var maxLength = _options.RedisStreams.MaxStreamLength > 0 
                ? _options.RedisStreams.MaxStreamLength 
                : (int?)null;

            await _redisRetryPolicy.ExecuteAsync(async () =>
            {
                var result = await _database!.StreamAddAsync(
                    streamName, 
                    entries.ToArray(),
                    maxLength: maxLength,
                    useApproximateMaxLength: true);

                _logger?.LogDebug("Published message {MessageType} with ID {MessageId} to Redis stream {StreamName}", 
                    typeof(TMessage).Name, result, streamName);
            });

            // Trim stream if needed (separate operation for better performance)
            if (maxLength.HasValue && maxLength.Value > 0)
            {
                await TrimStreamAsync(streamName, maxLength.Value);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error publishing message {MessageType}", typeof(TMessage).Name);
            throw;
        }
    }

    public async ValueTask SubscribeAsync<TMessage>(
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
        
        // Start consumer for specific stream if not already running
        var streamName = options?.QueueName ?? _options.RedisStreams!.DefaultStreamName ?? "relay:stream";
        
        if (!_consumerTasks.ContainsKey(streamName))
        {
            await EnsureConnectedAsync();
            
            var groupName = options?.ConsumerGroup ?? _options.RedisStreams.ConsumerGroupName ?? "relay-consumer-group";
            var consumerName = options?.ConsumerGroup != null 
                ? $"{options.ConsumerGroup}-{Environment.MachineName}-{Guid.NewGuid():N}"
                : _options.RedisStreams.ConsumerName ?? $"relay-consumer-{Environment.MachineName}";

            // Create consumer group if not exists
            if (_options.RedisStreams.CreateConsumerGroupIfNotExists)
            {
                await CreateConsumerGroupIfNotExistsAsync(streamName, groupName);
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _consumerTasks[streamName] = cts;

            // Start consumer task for this stream
            _ = Task.Run(async () =>
            {
                _logger?.LogInformation("Starting Redis Streams consumer for stream {StreamName}, group {GroupName}, consumer {ConsumerName}", 
                    streamName, groupName, consumerName);

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await ConsumeMessagesAsync(streamName, groupName, consumerName, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error in Redis Streams consumer for stream {StreamName}", streamName);
                        await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
                    }
                }
            }, cts.Token);
        }
        
        _logger?.LogDebug("Subscribed to message type {MessageType} on stream {StreamName}", typeof(TMessage).Name, streamName);
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted) return;

        try
        {
            await EnsureConnectedAsync();

            // Start pending entries monitoring task
            _pollingCts = new CancellationTokenSource();
            _pendingEntriesTask = Task.Run(async () =>
            {
                await MonitorPendingEntriesAsync(_pollingCts.Token);
            }, _pollingCts.Token);

            _isStarted = true;
            _logger?.LogInformation("Redis Streams message broker started successfully");
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
            // Cancel all consumer tasks
            foreach (var kvp in _consumerTasks)
            {
                kvp.Value.Cancel();
                try
                {
                    kvp.Value.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            _consumerTasks.Clear();

            // Cancel pending entries monitoring
            _pollingCts?.Cancel();
            
            if (_pendingEntriesTask != null)
            {
                await _pendingEntriesTask;
            }
            
            _pollingCts?.Dispose();
            _pollingCts = null;
            _pendingEntriesTask = null;
            
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
        var messageId = message.Id.ToString();
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var entries = message.Values.ToDictionary(nv => nv.Name.ToString(), nv => nv.Value.ToString());

            // Extract message ID if available
            var customMessageId = entries.TryGetValue("messageId", out var msgId) ? msgId : messageId;

            // Validate required fields
            if (!entries.TryGetValue("type", out var messageType))
            {
                _logger?.LogWarning("Message {MessageId} missing 'type' field", customMessageId);
                await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                return;
            }

            if (!entries.TryGetValue("data", out var messageData))
            {
                _logger?.LogWarning("Message {MessageId} missing 'data' field", customMessageId);
                await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                return;
            }

            // Check for message expiration
            if (entries.TryGetValue("expiration", out var expirationStr) && 
                long.TryParse(expirationStr, out var expirationMs))
            {
                var messageAge = startTime - DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(entries["timestamp"]));
                if (messageAge > TimeSpan.FromMilliseconds(expirationMs))
                {
                    _logger?.LogDebug("Message {MessageId} has expired, skipping", customMessageId);
                    await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                    return;
                }
            }

            var type = Type.GetType(messageType);

            if (type == null || !_handlers.ContainsKey(type))
            {
                _logger?.LogWarning("No handler found for message type {MessageType} for message {MessageId}", 
                    messageType, customMessageId);
                await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                return;
            }

            // Deserialize message with error handling
            object? deserializedMessage;
            try
            {
                deserializedMessage = JsonSerializer.Deserialize(messageData, type);
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "Failed to deserialize message {MessageId} of type {MessageType}", 
                    customMessageId, messageType);
                await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                return;
            }

            if (deserializedMessage == null)
            {
                _logger?.LogWarning("Deserialized message {MessageId} of type {MessageType} is null", 
                    customMessageId, messageType);
                await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                return;
            }

            // Extract headers
            var headers = entries
                .Where(kvp => kvp.Key.StartsWith("header:"))
                .ToDictionary(
                    kvp => kvp.Key.Substring(7), 
                    kvp => (object)kvp.Value);

            // Extract correlation ID
            if (entries.TryGetValue("correlationId", out var correlationId))
            {
                headers["CorrelationId"] = correlationId;
            }

            // Extract priority
            byte? priority = null;
            if (entries.TryGetValue("priority", out var priorityStr) && byte.TryParse(priorityStr, out var priorityValue))
            {
                priority = priorityValue;
            }

            var timestamp = entries.TryGetValue("timestamp", out var timestampStr) && 
                           long.TryParse(timestampStr, out var timestampMs)
                ? DateTimeOffset.FromUnixTimeMilliseconds(timestampMs)
                : startTime;

            var context = new MessageContext
            {
                MessageId = customMessageId,
                CorrelationId = headers?.ContainsKey("CorrelationId") == true ? headers["CorrelationId"].ToString() : null,
                Timestamp = timestamp,
                Headers = headers.Count > 0 ? headers : null,
                RoutingKey = streamName,
                Acknowledge = async () => 
                    await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken),
                Reject = async (requeue) =>
                {
                    if (requeue)
                    {
                        // For requeue, we don't acknowledge and let it become pending again
                        _logger?.LogDebug("Message {MessageId} rejected with requeue", customMessageId);
                    }
                    else
                    {
                        await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                    }
                }
            };

            // Process message with all handlers
            var handlers = _handlers[type];
            var processingTasks = handlers.Select(handler => 
                ProcessWithRetryAsync(handler, deserializedMessage, context, cancellationToken));

            await Task.WhenAll(processingTasks);

            // Auto-acknowledge if enabled
            if (_options.RedisStreams!.AutoAcknowledge && context.Acknowledge != null)
            {
                await context.Acknowledge();
            }

            var processingTime = DateTimeOffset.UtcNow - startTime;
            _logger?.LogDebug("Successfully processed message {MessageId} of type {MessageType} in {ProcessingTime}ms", 
                customMessageId, messageType, processingTime.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var processingTime = DateTimeOffset.UtcNow - startTime;
            _logger?.LogError(ex, "Error processing Redis Streams message {MessageId} after {ProcessingTime}ms", 
                messageId, processingTime.TotalMilliseconds);
            
            // Don't acknowledge on error - let it become pending for retry
        }
    }

    private async Task ProcessWithRetryAsync(
        Func<object, MessageContext, CancellationToken, ValueTask> handler,
        object message,
        MessageContext context,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;
        var maxRetries = _options.RetryPolicy?.MaxAttempts ?? 3;

        while (retryCount <= maxRetries)
        {
            try
            {
                await handler(message, context, cancellationToken);
                return; // Success, exit retry loop
            }
            catch (Exception ex) when (retryCount < maxRetries)
            {
                retryCount++;
                var delay = _options.RetryPolicy?.UseExponentialBackoff == true
                    ? TimeSpan.FromMilliseconds((_options.RetryPolicy.InitialDelay.TotalMilliseconds * Math.Pow(_options.RetryPolicy.BackoffMultiplier, retryCount - 1)))
                    : _options.RetryPolicy?.InitialDelay ?? TimeSpan.FromSeconds(1);

                _logger?.LogWarning(ex, "Handler failed for message {MessageId} (attempt {RetryAttempt}/{MaxRetries}). Retrying in {Delay}ms...", 
                    context.MessageId, retryCount, maxRetries, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }

        // Final attempt - let it throw if it fails
        await handler(message, context, cancellationToken);
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

            // Configure connection pooling and resilience
            configOptions.ConnectRetry = 3;
            configOptions.ReconnectRetryPolicy = new ExponentialRetry(1000);
            configOptions.AbortOnConnectFail = false;
            configOptions.KeepAlive = 60;

            _redis = await ConnectionMultiplexer.ConnectAsync(configOptions);
            _database = _redis.GetDatabase(_options.RedisStreams.Database);
            
            _logger?.LogInformation("Connected to Redis server (Database: {Database})", _options.RedisStreams.Database);
        }
    }

    private async Task CreateConsumerGroupIfNotExistsAsync(string streamName, string groupName)
    {
        try
        {
            await _redisRetryPolicy.ExecuteAsync(async () =>
            {
                await _database!.StreamCreateConsumerGroupAsync(streamName, groupName, StreamPosition.NewMessages);
            });
            
            _logger?.LogInformation("Created Redis consumer group {GroupName} for stream {StreamName}", 
                groupName, streamName);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            _logger?.LogDebug("Redis consumer group {GroupName} already exists for stream {StreamName}", 
                groupName, streamName);
        }
    }

    private async Task ConsumeMessagesAsync(string streamName, string groupName, string consumerName, CancellationToken cancellationToken)
    {
        var messages = await _redisRetryPolicy.ExecuteAsync(async () =>
        {
            return await _database!.StreamReadGroupAsync(
                streamName,
                groupName,
                consumerName,
                StreamPosition.NewMessages,
                count: _options.RedisStreams!.MaxMessagesToRead);
        });

        foreach (var message in messages)
        {
            await ProcessMessageAsync(streamName, groupName, message, cancellationToken);
        }

        if (messages.Length == 0)
        {
            await Task.Delay(_options.RedisStreams!.ReadTimeout, cancellationToken);
        }
    }

    private async Task MonitorPendingEntriesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken); // Check every 5 minutes

                foreach (var streamName in _consumerTasks.Keys)
                {
                    await ProcessPendingEntriesAsync(streamName);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error monitoring pending entries");
            }
        }
    }

    private async Task ProcessPendingEntriesAsync(string streamName)
    {
        try
        {
            var groupName = _options.RedisStreams!.ConsumerGroupName ?? "relay-consumer-group";
            var pendingInfo = await _database!.StreamPendingAsync(streamName, groupName);

            if (pendingInfo.PendingMessageCount > 0)
            {
                _logger?.LogWarning("Found {PendingCount} pending messages in stream {StreamName}, group {GroupName}", 
                    pendingInfo.PendingMessageCount, streamName, groupName);

                // Simple pending entries processing - just log for now
                // In a production environment, you would implement more sophisticated handling
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing pending entries for stream {StreamName}", streamName);
        }
    }

    private async Task TrimStreamAsync(string streamName, int maxLength)
    {
        try
        {
            await _redisRetryPolicy.ExecuteAsync(async () =>
            {
                await _database!.StreamTrimAsync(streamName, maxLength);
            });
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to trim stream {StreamName} to max length {MaxLength}", 
                streamName, maxLength);
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
