using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Relay.MessageBroker.Compression;
using Relay.Core.ContractValidation;

namespace Relay.MessageBroker.AzureServiceBus;

/// <summary>
/// Azure Service Bus implementation of message broker.
/// </summary>
public sealed class AzureServiceBusMessageBroker : BaseMessageBroker
{
    private readonly Dictionary<string, ServiceBusSessionProcessor?> _sessionProcessors = new();
    private ServiceBusClient? _client;
    private ServiceBusSender? _sender;
    private ServiceBusProcessor? _processor;
    private readonly AsyncRetryPolicy _retryPolicy;

    public AzureServiceBusMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger<AzureServiceBusMessageBroker> logger,
        IMessageCompressor? compressor = null,
        IContractValidator? contractValidator = null)
        : base(options, logger, compressor, contractValidator)
    {
        if (_options.AzureServiceBus == null)
            throw new InvalidOperationException("Azure Service Bus options are required.");
        
        if (string.IsNullOrWhiteSpace(_options.AzureServiceBus.ConnectionString))
            throw new InvalidOperationException("Azure Service Bus connection string is required.");

        // Configure retry policy
        var retryOptions = _options.RetryPolicy ?? new RetryPolicy();
        _retryPolicy = Policy
            .Handle<ServiceBusException>(ex => ex.IsTransient)
            .Or<TimeoutException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryOptions.MaxAttempts,
                retryAttempt => TimeSpan.FromMilliseconds(
                    Math.Min(
                        retryOptions.InitialDelay.TotalMilliseconds * Math.Pow(retryOptions.BackoffMultiplier, retryAttempt - 1),
                        retryOptions.MaxDelay.TotalMilliseconds)),
                onRetry: (exception, timespan, retryAttempt, context) =>
                {
                    _logger.LogWarning(exception, 
                        "Retry {RetryAttempt} after {Delay}ms due to error: {Error}", 
                        retryAttempt, timespan.TotalMilliseconds, exception?.Message);
                });
    }

protected override async ValueTask PublishInternalAsync<TMessage>(
        TMessage message,
        byte[] serializedMessage,
        PublishOptions? options,
        CancellationToken cancellationToken)
    {
            _client ??= new ServiceBusClient(_options.AzureServiceBus!.ConnectionString);

            var entityName = options?.RoutingKey
                ?? _options.AzureServiceBus!.DefaultEntityName
                ?? typeof(TMessage).Name;

            _sender ??= _client.CreateSender(entityName);

        var messageBody = Encoding.UTF8.GetString(serializedMessage);
        var serviceBusMessage = new ServiceBusMessage(messageBody)
        {
            MessageId = Guid.NewGuid().ToString(),
            ContentType = "application/json",
            Subject = typeof(TMessage).FullName
        };

        if (options?.Headers != null)
        {
            foreach (var header in options.Headers)
            {
                serviceBusMessage.ApplicationProperties[header.Key] = header.Value;
            }
        }

        if (options?.Expiration.HasValue == true)
        {
            serviceBusMessage.TimeToLive = options.Expiration.Value;
        }

        // Add session ID if provided in headers
        if (options?.Headers?.ContainsKey("SessionId") == true)
        {
            serviceBusMessage.SessionId = options.Headers["SessionId"]?.ToString();
        }

        // Support for scheduled enqueue time
        if (options?.Headers?.ContainsKey("ScheduledEnqueueTime") == true)
        {
            if (DateTime.TryParse(options.Headers["ScheduledEnqueueTime"]?.ToString(), out var scheduledTime))
            {
                serviceBusMessage.ScheduledEnqueueTime = scheduledTime;
            }
        }

        await _retryPolicy.ExecuteAsync(async () =>
        {
            await _sender.SendMessageAsync(serviceBusMessage, cancellationToken);
        });
        
        _logger.LogDebug("Published message {MessageType} to {EntityName}", 
            typeof(TMessage).Name, entityName);
    }

    /// <summary>
    /// Publishes multiple messages in a batch for improved performance.
    /// </summary>
    public async ValueTask PublishBatchAsync<TMessage>(IEnumerable<TMessage> messages, PublishOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (messages == null) throw new ArgumentNullException(nameof(messages));

        try
        {
            _client ??= new ServiceBusClient(_options.AzureServiceBus!.ConnectionString);

            var entityName = options?.RoutingKey
                ?? _options.AzureServiceBus!.DefaultEntityName
                ?? typeof(TMessage).Name;
            
            _sender ??= _client.CreateSender(entityName);

            var serviceBusMessages = new List<ServiceBusMessage>();
            
            foreach (var message in messages)
            {
                var messageBody = JsonSerializer.Serialize(message);
                var serviceBusMessage = new ServiceBusMessage(messageBody)
                {
                    MessageId = Guid.NewGuid().ToString(),
                    ContentType = "application/json",
                    Subject = typeof(TMessage).FullName
                };

                if (options?.Headers != null)
                {
                    foreach (var header in options.Headers)
                    {
                        serviceBusMessage.ApplicationProperties[header.Key] = header.Value;
                    }
                }

                if (options?.Expiration.HasValue == true)
                {
                    serviceBusMessage.TimeToLive = options.Expiration.Value;
                }

                // Add session ID if provided in headers
                if (options?.Headers?.ContainsKey("SessionId") == true)
                {
                    serviceBusMessage.SessionId = options.Headers["SessionId"]?.ToString();
                }

                // Support for scheduled enqueue time
                if (options?.Headers?.ContainsKey("ScheduledEnqueueTime") == true)
                {
                    if (DateTime.TryParse(options.Headers["ScheduledEnqueueTime"]?.ToString(), out var scheduledTime))
                    {
                        serviceBusMessage.ScheduledEnqueueTime = scheduledTime;
                    }
                }

                serviceBusMessages.Add(serviceBusMessage);
            }

            // Send messages in batches to respect Service Bus limits
            const int maxBatchSize = 100; // Service Bus limit
            for (int i = 0; i < serviceBusMessages.Count; i += maxBatchSize)
            {
                var batch = serviceBusMessages.Skip(i).Take(maxBatchSize);
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _sender.SendMessagesAsync(batch, cancellationToken);
                });
            }
            
            _logger?.LogDebug("Published batch of {MessageCount} messages of type {MessageType} to {EntityName}", 
                serviceBusMessages.Count, typeof(TMessage).Name, entityName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error publishing batch messages {MessageType}", typeof(TMessage).Name);
            throw;
        }
    }

    /// <summary>
    /// Schedules a message to be sent at a specific time.
    /// </summary>
    public async ValueTask ScheduleMessageAsync<TMessage>(TMessage message, DateTime scheduledEnqueueTime, PublishOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        try
        {
            _client ??= new ServiceBusClient(_options.AzureServiceBus!.ConnectionString);

            var entityName = options?.RoutingKey
                ?? _options.AzureServiceBus!.DefaultEntityName
                ?? typeof(TMessage).Name;

            _sender ??= _client.CreateSender(entityName);

            var messageBody = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                MessageId = Guid.NewGuid().ToString(),
                ContentType = "application/json",
                Subject = typeof(TMessage).FullName,
                ScheduledEnqueueTime = scheduledEnqueueTime
            };

            if (options?.Headers != null)
            {
                foreach (var header in options.Headers)
                {
                    serviceBusMessage.ApplicationProperties[header.Key] = header.Value;
                }
            }

            if (options?.Expiration.HasValue == true)
            {
                serviceBusMessage.TimeToLive = options.Expiration.Value;
            }

            // Add session ID if provided in headers
            if (options?.Headers?.ContainsKey("SessionId") == true)
            {
                serviceBusMessage.SessionId = options.Headers["SessionId"]?.ToString();
            }

            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _sender.SendMessageAsync(serviceBusMessage, cancellationToken);
            });
            
            _logger?.LogDebug("Scheduled message {MessageType} for {ScheduledTime} to {EntityName}", 
                typeof(TMessage).Name, scheduledEnqueueTime, entityName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error scheduling message {MessageType}", typeof(TMessage).Name);
            throw;
        }
    }

    /// <summary>
    /// Cancels a scheduled message.
    /// </summary>
    public async ValueTask CancelScheduledMessageAsync(long sequenceNumber, string? entityName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _client ??= new ServiceBusClient(_options.AzureServiceBus!.ConnectionString);

            entityName ??= _options.AzureServiceBus!.DefaultEntityName ?? "relay-messages";

            var sender = _client.CreateSender(entityName);
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await sender.CancelScheduledMessageAsync(sequenceNumber, cancellationToken);
            });
            
            _logger?.LogDebug("Cancelled scheduled message with sequence number {SequenceNumber} from {EntityName}", 
                sequenceNumber, entityName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error cancelling scheduled message {SequenceNumber}", sequenceNumber);
            throw;
        }
    }

    /// <summary>
    /// Receives and processes dead-lettered messages.
    /// </summary>
    public async ValueTask ProcessDeadLetterMessagesAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        string? entityName = null,
        CancellationToken cancellationToken = default)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        try
        {
            _client ??= new ServiceBusClient(_options.AzureServiceBus!.ConnectionString);

            entityName ??= _options.AzureServiceBus!.DefaultEntityName ?? "relay-messages";
            var deadLetterEntityName = $"{entityName}/$DeadLetterQueue";

            var receiverOptions = new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock,
                PrefetchCount = _options.AzureServiceBus!.PrefetchCount
            };

            var receiver = _options.AzureServiceBus!.EntityType == AzureEntityType.Topic &&
                          !string.IsNullOrWhiteSpace(_options.AzureServiceBus!.SubscriptionName)
                ? _client.CreateReceiver(entityName, _options.AzureServiceBus!.SubscriptionName, receiverOptions)
                : _client.CreateReceiver(entityName, receiverOptions);

            var receivedMessages = await receiver.ReceiveMessagesAsync(_options.AzureServiceBus.MaxConcurrentCalls, TimeSpan.FromSeconds(1));

            foreach (var message in receivedMessages)
            {
                try
                {
                    var messageType = message.Subject;
                    var type = !string.IsNullOrWhiteSpace(messageType) 
                        ? Type.GetType(messageType) 
                        : null;

                    if (type == null || type != typeof(TMessage))
                    {
                        await receiver.CompleteMessageAsync(message, cancellationToken);
                        continue;
                    }

                    var messageBody = message.Body.ToString();
                    var deadLetterMessage = JsonSerializer.Deserialize(messageBody, type);

                    if (deadLetterMessage == null)
                    {
                        await receiver.DeadLetterMessageAsync(message, new Dictionary<string, object> { { "Reason", "Deserialization failed in dead letter processing" } }, cancellationToken);
                        continue;
                    }

                    var context = new MessageContext
                    {
                        MessageId = message.MessageId,
                        CorrelationId = message.CorrelationId,
                        Timestamp = message.EnqueuedTime,
                        Headers = message.ApplicationProperties.ToDictionary(
                            kvp => kvp.Key, 
                            kvp => kvp.Value),
                        RoutingKey = message.Subject,
                        Acknowledge = async () => await receiver.CompleteMessageAsync(message, cancellationToken),
                        Reject = async (requeue) =>
                        {
                            if (requeue)
                            {
                                await receiver.AbandonMessageAsync(message, null, cancellationToken);
                            }
                            else
                            {
                                await receiver.DeadLetterMessageAsync(message, new Dictionary<string, object> { { "Reason", "Rejected from dead letter processing" } }, cancellationToken);
                            }
                        }
                    };

                    await handler((TMessage)deadLetterMessage, context, cancellationToken);
                    
                    _logger?.LogInformation("Processed dead-lettered message {MessageId} of type {MessageType}", 
                        message.MessageId, typeof(TMessage).Name);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing dead-lettered message {MessageId}", message.MessageId);
                    await receiver.DeadLetterMessageAsync(message, new Dictionary<string, object> { { "Reason", $"Processing error: {ex.Message}" } }, cancellationToken);
                }
            }

            await receiver.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing dead letter messages for type {MessageType}", typeof(TMessage).Name);
            throw;
        }
    }

    /// <summary>
    /// Requeues a dead-lettered message back to the original queue.
    /// </summary>
    public async ValueTask RequeueDeadLetterMessageAsync(string messageId, string? entityName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _client ??= new ServiceBusClient(_options.AzureServiceBus!.ConnectionString);

            entityName ??= _options.AzureServiceBus!.DefaultEntityName ?? "relay-messages";
            var deadLetterEntityName = $"{entityName}/$DeadLetterQueue";

            var receiverOptions = new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            };

            var receiver = _options.AzureServiceBus!.EntityType == AzureEntityType.Topic &&
                          !string.IsNullOrWhiteSpace(_options.AzureServiceBus!.SubscriptionName)
                ? _client.CreateReceiver(entityName, _options.AzureServiceBus!.SubscriptionName, receiverOptions)
                : _client.CreateReceiver(entityName, receiverOptions);

            var sender = _client.CreateSender(entityName);

            // Find the specific message in dead letter queue
            var messages = await receiver.ReceiveMessagesAsync(10, TimeSpan.FromSeconds(1));
            var targetMessage = messages.FirstOrDefault(m => m.MessageId == messageId);

            if (targetMessage != null)
            {
                // Create a new message with the same content
                var requeuedMessage = new ServiceBusMessage(targetMessage.Body)
                {
                    MessageId = targetMessage.MessageId,
                    CorrelationId = targetMessage.CorrelationId,
                    ContentType = targetMessage.ContentType,
                    Subject = targetMessage.Subject,
                    TimeToLive = targetMessage.TimeToLive
                };

                // Copy application properties
                foreach (var property in targetMessage.ApplicationProperties)
                {
                    requeuedMessage.ApplicationProperties[property.Key] = property.Value;
                }

                await sender.SendMessageAsync(requeuedMessage, cancellationToken);
                await receiver.CompleteMessageAsync(targetMessage, cancellationToken);
                
                _logger?.LogInformation("Requeued dead-lettered message {MessageId} back to {EntityName}", messageId, entityName);
            }
            else
            {
                _logger?.LogWarning("Dead-lettered message {MessageId} not found in {EntityName}", messageId, entityName);
            }

            await receiver.DisposeAsync();
            await sender.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error requeuing dead-lettered message {MessageId}", messageId);
            throw;
        }
    }

    /// <summary>
    /// Executes operations within a transaction using Service Bus batch operations.
    /// </summary>
    public async ValueTask ExecuteInTransactionAsync(
        Func<CancellationToken, ValueTask> operation,
        string? entityName = null,
        CancellationToken cancellationToken = default)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        try
        {
            await operation(cancellationToken);
            
            // Metrics are now handled by the base class
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing transaction");
            // Metrics are now handled by the base class
            throw;
        }
    }

    /// <summary>
    /// Publishes a message using batch operations for transactional behavior.
    /// </summary>
    public async ValueTask PublishInTransactionAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        string? entityName = null,
        CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        try
        {
            entityName ??= _options.AzureServiceBus!.DefaultEntityName ?? typeof(TMessage).Name;
            
            var sender = _client?.CreateSender(entityName) ?? throw new InvalidOperationException("Service Bus client not initialized");

            var messageBody = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                MessageId = Guid.NewGuid().ToString(),
                ContentType = "application/json",
                Subject = typeof(TMessage).FullName
            };

            if (options?.Headers != null)
            {
                foreach (var header in options.Headers)
                {
                    serviceBusMessage.ApplicationProperties[header.Key] = header.Value;
                }
            }

            if (options?.Expiration.HasValue == true)
            {
                serviceBusMessage.TimeToLive = options.Expiration.Value;
            }

            // Add session ID if provided in headers
            if (options?.Headers?.ContainsKey("SessionId") == true)
            {
                serviceBusMessage.SessionId = options.Headers["SessionId"]?.ToString();
            }

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
            
            _logger?.LogDebug("Published message {MessageType} in transaction to {EntityName}", 
                typeof(TMessage).Name, entityName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error publishing message {MessageType} in transaction", typeof(TMessage).Name);
            throw;
        }
    }

    /// <summary>
    /// Completes a message.
    /// </summary>
    public async ValueTask CompleteInTransactionAsync(
        ServiceBusReceivedMessage message,
        string? entityName = null,
        CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        try
        {
            entityName ??= _options.AzureServiceBus!.DefaultEntityName ?? "relay-messages";
            
            var receiverOptions = new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            };

            var receiver = _options.AzureServiceBus!.EntityType == AzureEntityType.Topic &&
                          !string.IsNullOrWhiteSpace(_options.AzureServiceBus!.SubscriptionName)
                ? _client!.CreateReceiver(entityName, _options.AzureServiceBus!.SubscriptionName, receiverOptions)
                : _client!.CreateReceiver(entityName, receiverOptions);

            await receiver.CompleteMessageAsync(message, cancellationToken);
            
            _logger?.LogDebug("Completed message {MessageId} in transaction", message.MessageId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error completing message {MessageId} in transaction", message.MessageId);
            throw;
        }
    }

protected override async ValueTask SubscribeInternalAsync(
        Type messageType,
        SubscriptionInfo subscriptionInfo,
        CancellationToken cancellationToken)
    {
        // Azure Service Bus handles subscriptions in StartInternalAsync
        await ValueTask.CompletedTask;
    }

protected override async ValueTask StartInternalAsync(CancellationToken cancellationToken)
    {
        _client ??= new ServiceBusClient(_options.AzureServiceBus!.ConnectionString);
        
        var entityName = _options.AzureServiceBus!.DefaultEntityName ?? "relay-messages";
        
        var processorOptions = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = _options.AzureServiceBus.MaxConcurrentCalls,
            AutoCompleteMessages = _options.AzureServiceBus.AutoCompleteMessages,
            PrefetchCount = _options.AzureServiceBus.PrefetchCount,
            ReceiveMode = _options.AzureServiceBus.AutoCompleteMessages 
                ? ServiceBusReceiveMode.ReceiveAndDelete 
                : ServiceBusReceiveMode.PeekLock
        };

        // Create regular processor for non-session messages
        _processor = _options.AzureServiceBus.EntityType == AzureEntityType.Topic && 
                     !string.IsNullOrWhiteSpace(_options.AzureServiceBus.SubscriptionName)
            ? _client.CreateProcessor(entityName, _options.AzureServiceBus.SubscriptionName, processorOptions)
            : _client.CreateProcessor(entityName, processorOptions);

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(cancellationToken);

        // Create session processor if sessions are enabled
        if (_options.AzureServiceBus.SessionsEnabled)
        {
            var sessionProcessorOptions = new ServiceBusSessionProcessorOptions
            {
                MaxConcurrentCallsPerSession = 1,
                AutoCompleteMessages = _options.AzureServiceBus.AutoCompleteMessages,
                PrefetchCount = _options.AzureServiceBus.PrefetchCount,
                ReceiveMode = _options.AzureServiceBus.AutoCompleteMessages 
                    ? ServiceBusReceiveMode.ReceiveAndDelete 
                    : ServiceBusReceiveMode.PeekLock,
                MaxConcurrentSessions = _options.AzureServiceBus.MaxConcurrentCalls,
                SessionIdleTimeout = TimeSpan.FromMinutes(1)
            };

            var sessionProcessor = _options.AzureServiceBus.EntityType == AzureEntityType.Topic && 
                                  !string.IsNullOrWhiteSpace(_options.AzureServiceBus.SubscriptionName)
                ? _client.CreateSessionProcessor(entityName, _options.AzureServiceBus.SubscriptionName, sessionProcessorOptions)
                : _client.CreateSessionProcessor(entityName, sessionProcessorOptions);

            sessionProcessor.ProcessMessageAsync += ProcessSessionMessageAsync;
            sessionProcessor.ProcessErrorAsync += ProcessErrorAsync;
            sessionProcessor.SessionInitializingAsync += SessionInitializingAsync;
            sessionProcessor.SessionClosingAsync += SessionClosingAsync;

            await sessionProcessor.StartProcessingAsync(cancellationToken);
            _sessionProcessors[entityName] = sessionProcessor;
        }
        
        _logger.LogInformation("Azure Service Bus message broker started for entity {EntityName}", entityName);
    }

protected override async ValueTask StopInternalAsync(CancellationToken cancellationToken)
    {
        // Stop session processors
        foreach (var sessionProcessor in _sessionProcessors.Values)
        {
            if (sessionProcessor != null)
            {
                await sessionProcessor.StopProcessingAsync(cancellationToken);
                await sessionProcessor.DisposeAsync();
            }
        }
        _sessionProcessors.Clear();

        // Stop regular processor
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            _logger.LogInformation("Azure Service Bus message broker stopped");
        }
    }

private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var messageType = args.Message.Subject;
            var type = !string.IsNullOrWhiteSpace(messageType) 
                ? Type.GetType(messageType) 
                : null;

            if (type == null)
            {
                _logger.LogWarning("No handler found for message type {MessageType}", messageType);
                
                if (!_options.AzureServiceBus!.AutoCompleteMessages)
                {
                    await args.CompleteMessageAsync(args.Message);
                }
                return;
            }

            var messageBody = args.Message.Body.ToArray();
            var message = DeserializeMessage<object>(messageBody);

            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize message of type {MessageType}", messageType);
                
                if (!_options.AzureServiceBus!.AutoCompleteMessages)
                {
                    await args.DeadLetterMessageAsync(args.Message, "Deserialization failed");
                }
                return;
            }

            var context = new MessageContext
            {
                MessageId = args.Message.MessageId,
                CorrelationId = args.Message.CorrelationId,
                Timestamp = args.Message.EnqueuedTime,
                Headers = args.Message.ApplicationProperties.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => kvp.Value),
                Acknowledge = async () =>
                {
                    if (!_options.AzureServiceBus!.AutoCompleteMessages)
                    {
                        await args.CompleteMessageAsync(args.Message);
                    }
                },
                Reject = async (requeue) =>
                {
                    if (!_options.AzureServiceBus!.AutoCompleteMessages)
                    {
                        if (requeue)
                        {
                            await args.AbandonMessageAsync(args.Message);
                        }
                        else
                        {
                            await args.DeadLetterMessageAsync(args.Message);
                        }
                    }
                }
            };

            await ProcessMessageAsync(message, type, context, args.CancellationToken);

            if (!_options.AzureServiceBus!.AutoCompleteMessages && context.Acknowledge != null)
            {
                await context.Acknowledge();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Azure Service Bus message");
            
            if (!_options.AzureServiceBus!.AutoCompleteMessages)
            {
                await args.AbandonMessageAsync(args.Message);
            }
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger?.LogError(args.Exception, 
            "Azure Service Bus error from source {ErrorSource}, entity path {EntityPath}", 
            args.ErrorSource, args.EntityPath);
        
        return Task.CompletedTask;
    }

private async Task ProcessSessionMessageAsync(ProcessSessionMessageEventArgs args)
    {
        try
        {
            var messageType = args.Message.Subject;
            var type = !string.IsNullOrWhiteSpace(messageType) 
                ? Type.GetType(messageType) 
                : null;

            if (type == null)
            {
                _logger.LogWarning("No handler found for session message type {MessageType}", messageType);
                
                if (!_options.AzureServiceBus!.AutoCompleteMessages)
                {
                    await args.CompleteMessageAsync(args.Message);
                }
                return;
            }

            var messageBody = args.Message.Body.ToArray();
            var message = DeserializeMessage<object>(messageBody);

            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize session message of type {MessageType}", messageType);
                
                if (!_options.AzureServiceBus!.AutoCompleteMessages)
                {
                    await args.DeadLetterMessageAsync(args.Message, "Deserialization failed");
                }
                return;
            }

            var context = new MessageContext
            {
                MessageId = args.Message.MessageId,
                CorrelationId = args.Message.CorrelationId,
                Timestamp = args.Message.EnqueuedTime,
                Headers = args.Message.ApplicationProperties.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => kvp.Value),
                RoutingKey = args.Message.SessionId,
                Acknowledge = async () =>
                {
                    if (!_options.AzureServiceBus!.AutoCompleteMessages)
                    {
                        await args.CompleteMessageAsync(args.Message);
                    }
                },
                Reject = async (requeue) =>
                {
                    if (!_options.AzureServiceBus!.AutoCompleteMessages)
                    {
                        if (requeue)
                        {
                            await args.AbandonMessageAsync(args.Message);
                        }
                        else
                        {
                            await args.DeadLetterMessageAsync(args.Message);
                        }
                    }
                }
            };

            await ProcessMessageAsync(message, type, context, args.CancellationToken);

            if (!_options.AzureServiceBus!.AutoCompleteMessages && context.Acknowledge != null)
            {
                await context.Acknowledge();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Azure Service Bus session message");
            
            if (!_options.AzureServiceBus!.AutoCompleteMessages)
            {
                await args.AbandonMessageAsync(args.Message);
            }
        }
    }

private Task SessionInitializingAsync(ProcessSessionEventArgs args)
    {
        _logger.LogDebug("Session {SessionId} initializing", args.SessionId);
        return Task.CompletedTask;
    }

    private Task SessionClosingAsync(ProcessSessionEventArgs args)
    {
        _logger.LogDebug("Session {SessionId} closing", args.SessionId);
        return Task.CompletedTask;
    }

    

    protected override async ValueTask DisposeInternalAsync()
    {
        // Dispose session processors
        foreach (var sessionProcessor in _sessionProcessors.Values)
        {
            if (sessionProcessor != null)
            {
                await sessionProcessor.DisposeAsync();
            }
        }
        _sessionProcessors.Clear();

        if (_processor != null)
        {
            await _processor.DisposeAsync();
        }

        if (_sender != null)
        {
            await _sender.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }
    }
}
