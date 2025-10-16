using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Polly;
using Polly.Retry;
using Relay.MessageBroker.Compression;
using Relay.Core.ContractValidation;

namespace Relay.MessageBroker.RedisStreams;

/// <summary>
/// Redis Streams implementation of message broker with enhanced enterprise features.
/// </summary>
public sealed class RedisStreamsMessageBroker : BaseMessageBroker
{
    private readonly Dictionary<string, CancellationTokenSource> _consumerTasks = new();
    private readonly AsyncRetryPolicy _redisRetryPolicy;
    private ConnectionMultiplexer? _redis;
    private IDatabase? _database;
    private CancellationTokenSource? _pollingCts;
    private Task? _pendingEntriesTask;

    public RedisStreamsMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger<RedisStreamsMessageBroker> logger,
        IMessageCompressor? compressor = null,
        IContractValidator? contractValidator = null)
        : base(options, logger, compressor, contractValidator)
    {
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
                    _logger.LogWarning(outcome, 
                        "Redis operation failed (attempt {RetryAttempt}/{MaxRetries}). Retrying in {Delay}ms...", 
                        retryAttempt, _options.RetryPolicy?.MaxAttempts ?? 3, timespan.TotalMilliseconds);
                });
    }

    protected override async ValueTask PublishInternalAsync<TMessage>(
        TMessage message,
        byte[] serializedMessage,
        PublishOptions? options,
        CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync();

        var streamName = options?.RoutingKey ?? _options.RedisStreams!.DefaultStreamName ?? "relay:stream";
        var messageBody = System.Text.Encoding.UTF8.GetString(serializedMessage);
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

        var maxLength = _options.RedisStreams!.MaxStreamLength > 0
            ? _options.RedisStreams!.MaxStreamLength
            : (int?)null;

        await _redisRetryPolicy.ExecuteAsync(async () =>
        {
            var result = await _database!.StreamAddAsync(
                streamName, 
                entries.ToArray(),
                maxLength: maxLength,
                useApproximateMaxLength: true);

            _logger.LogDebug("Published message {MessageType} with ID {MessageId} to Redis stream {StreamName}", 
                typeof(TMessage).Name, result, streamName);
        });

        // Trim stream if needed (separate operation for better performance)
        if (maxLength.HasValue && maxLength.Value > 0)
        {
            await TrimStreamAsync(streamName, maxLength.Value);
        }
    }

    protected override async ValueTask SubscribeInternalAsync(
        Type messageType,
        SubscriptionInfo subscriptionInfo,
        CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync();
        
        // Start consumer for specific stream if not already running
        var streamName = subscriptionInfo.Options.QueueName ?? _options.RedisStreams!.DefaultStreamName ?? "relay:stream";

        if (!_consumerTasks.ContainsKey(streamName))
        {
            var groupName = subscriptionInfo.Options.ConsumerGroup ?? _options.RedisStreams!.ConsumerGroupName ?? "relay-consumer-group";
            var consumerName = subscriptionInfo.Options.ConsumerGroup != null
                ? $"{subscriptionInfo.Options.ConsumerGroup}-{Environment.MachineName}-{Guid.NewGuid():N}"
                : _options.RedisStreams!.ConsumerName ?? $"relay-consumer-{Environment.MachineName}";

            // Create consumer group if not exists
            if (_options.RedisStreams!.CreateConsumerGroupIfNotExists)
            {
                await CreateConsumerGroupIfNotExistsAsync(streamName, groupName);
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _consumerTasks[streamName] = cts;

            // Start consumer task for this stream
            _ = Task.Run(async () =>
            {
                _logger.LogInformation("Starting Redis Streams consumer for stream {StreamName}, group {GroupName}, consumer {ConsumerName}", 
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
                        _logger.LogError(ex, "Error in Redis Streams consumer for stream {StreamName}", streamName);
                        await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
                    }
                }
            }, cts.Token);
        }
    }

    protected override async ValueTask StartInternalAsync(CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync();
        
        // Note: Pending entries monitoring can be enabled here if needed in the future
        // Currently disabled as MonitorPendingEntries property is not available in RedisStreamsOptions
        
        _logger.LogInformation("Redis Streams message broker started");
    }

    protected override async ValueTask StopInternalAsync(CancellationToken cancellationToken)
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
        
        _logger.LogInformation("Redis Streams message broker stopped");
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

            // Extract message type and data for validation and processing
            var messageType = entries.TryGetValue("type", out var mt) ? mt : null;
            var messageData = entries.TryGetValue("data", out var md) ? md : null;

            // Validate required fields using consolidated validation
            if (_validation != null)
            {
                if (!_validation.ValidateBasicMessageFields(messageType, messageData, customMessageId))
                {
                    await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                    return;
                }
            }
            else
            {
                // Fallback to basic validation
                if (string.IsNullOrEmpty(messageType))
                {
                    _logger?.LogWarning("Message {MessageId} missing 'type' field", customMessageId);
                    await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                    return;
                }

                if (string.IsNullOrEmpty(messageData))
                {
                    _logger?.LogWarning("Message {MessageId} missing 'data' field", customMessageId);
                    await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                    return;
                }
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

            var type = Type.GetType(messageType!);

            if (type == null)
            {
                _logger.LogWarning("Unknown message type {MessageType} for message {MessageId}", 
                    messageType, customMessageId);
                await AcknowledgeMessageAsync(streamName, groupName, message.Id, cancellationToken);
                return;
            }

            // Deserialize message with error handling
            object? deserializedMessage;
            try
            {
                deserializedMessage = JsonSerializer.Deserialize(messageData!, type);
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
                Headers = headers!.Count > 0 ? headers : null,
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

            // Process message using base class
            await ProcessMessageAsync(deserializedMessage, type, context, cancellationToken);

            // Auto-acknowledge if enabled
            if (_options.RedisStreams!.AutoAcknowledge && context.Acknowledge != null)
            {
                await context.Acknowledge();
            }

            var processingTime = DateTimeOffset.UtcNow - startTime;
            _logger.LogDebug("Successfully processed message {MessageId} of type {MessageType} in {ProcessingTime}ms", 
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

    protected override async ValueTask DisposeInternalAsync()
    {
        if (_redis != null)
        {
            await _redis.DisposeAsync();
        }
    }
}
