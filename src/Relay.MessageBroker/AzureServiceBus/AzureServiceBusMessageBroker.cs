using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Relay.MessageBroker.AzureServiceBus;

/// <summary>
/// Azure Service Bus implementation of message broker.
/// </summary>
public sealed class AzureServiceBusMessageBroker : IMessageBroker, IAsyncDisposable
{
    private readonly MessageBrokerOptions _options;
    private readonly ILogger<AzureServiceBusMessageBroker>? _logger;
    private readonly Dictionary<Type, List<Func<object, MessageContext, CancellationToken, ValueTask>>> _handlers = new();
    private ServiceBusClient? _client;
    private ServiceBusSender? _sender;
    private ServiceBusProcessor? _processor;
    private bool _isStarted;

    public AzureServiceBusMessageBroker(
        MessageBrokerOptions options,
        ILogger<AzureServiceBusMessageBroker>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        
        if (_options.AzureServiceBus == null)
            throw new InvalidOperationException("Azure Service Bus options are required.");
        
        if (string.IsNullOrWhiteSpace(_options.AzureServiceBus.ConnectionString))
            throw new InvalidOperationException("Azure Service Bus connection string is required.");
    }

    public async ValueTask PublishAsync<TMessage>(TMessage message, PublishOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        try
        {
            _client ??= new ServiceBusClient(_options.AzureServiceBus!.ConnectionString);
            
            var entityName = options?.RoutingKey 
                ?? _options.AzureServiceBus.DefaultEntityName 
                ?? typeof(TMessage).Name;
            
            _sender ??= _client.CreateSender(entityName);

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

            await _sender.SendMessageAsync(serviceBusMessage, cancellationToken);
            
            _logger?.LogDebug("Published message {MessageType} to {EntityName}", 
                typeof(TMessage).Name, entityName);
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
            _client ??= new ServiceBusClient(_options.AzureServiceBus!.ConnectionString);
            
            var entityName = _options.AzureServiceBus.DefaultEntityName ?? "relay-messages";
            
            var processorOptions = new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = _options.AzureServiceBus.MaxConcurrentCalls,
                AutoCompleteMessages = _options.AzureServiceBus.AutoCompleteMessages,
                PrefetchCount = _options.AzureServiceBus.PrefetchCount,
                ReceiveMode = _options.AzureServiceBus.AutoCompleteMessages 
                    ? ServiceBusReceiveMode.ReceiveAndDelete 
                    : ServiceBusReceiveMode.PeekLock
            };

            _processor = _options.AzureServiceBus.EntityType == AzureEntityType.Topic && 
                         !string.IsNullOrWhiteSpace(_options.AzureServiceBus.SubscriptionName)
                ? _client.CreateProcessor(entityName, _options.AzureServiceBus.SubscriptionName, processorOptions)
                : _client.CreateProcessor(entityName, processorOptions);

            _processor.ProcessMessageAsync += ProcessMessageAsync;
            _processor.ProcessErrorAsync += ProcessErrorAsync;

            await _processor.StartProcessingAsync(cancellationToken);
            
            _isStarted = true;
            _logger?.LogInformation("Azure Service Bus message broker started for entity {EntityName}", entityName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting Azure Service Bus message broker");
            throw;
        }
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted) return;

        try
        {
            if (_processor != null)
            {
                await _processor.StopProcessingAsync(cancellationToken);
                _logger?.LogInformation("Azure Service Bus message broker stopped");
            }

            _isStarted = false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping Azure Service Bus message broker");
            throw;
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

            if (type == null || !_handlers.ContainsKey(type))
            {
                _logger?.LogWarning("No handler found for message type {MessageType}", messageType);
                
                if (!_options.AzureServiceBus!.AutoCompleteMessages)
                {
                    await args.CompleteMessageAsync(args.Message);
                }
                return;
            }

            var messageBody = args.Message.Body.ToString();
            var message = JsonSerializer.Deserialize(messageBody, type);

            if (message == null)
            {
                _logger?.LogWarning("Failed to deserialize message of type {MessageType}", messageType);
                
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

            var handlers = _handlers[type];
            foreach (var handler in handlers)
            {
                await handler(message, context, args.CancellationToken);
            }

            if (!_options.AzureServiceBus!.AutoCompleteMessages && context.Acknowledge != null)
            {
                await context.Acknowledge();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing Azure Service Bus message");
            
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

    public async ValueTask DisposeAsync()
    {
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
