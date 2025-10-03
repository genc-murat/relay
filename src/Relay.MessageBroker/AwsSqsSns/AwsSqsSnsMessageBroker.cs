using System.Text.Json;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using SqsMessageAttributeValue = Amazon.SQS.Model.MessageAttributeValue;
using SnsMessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace Relay.MessageBroker.AwsSqsSns;

/// <summary>
/// AWS SQS/SNS implementation of message broker.
/// </summary>
public sealed class AwsSqsSnsMessageBroker : IMessageBroker, IAsyncDisposable
{
    private readonly MessageBrokerOptions _options;
    private readonly ILogger<AwsSqsSnsMessageBroker>? _logger;
    private readonly Dictionary<Type, List<Func<object, MessageContext, CancellationToken, ValueTask>>> _handlers = new();
    private AmazonSQSClient? _sqsClient;
    private AmazonSimpleNotificationServiceClient? _snsClient;
    private bool _isStarted;
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;

    public AwsSqsSnsMessageBroker(
        MessageBrokerOptions options,
        ILogger<AwsSqsSnsMessageBroker>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        
        if (_options.AwsSqsSns == null)
            throw new InvalidOperationException("AWS SQS/SNS options are required.");
    }

    public async ValueTask PublishAsync<TMessage>(TMessage message, PublishOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        try
        {
            var messageBody = JsonSerializer.Serialize(message);
            var messageType = typeof(TMessage).FullName ?? typeof(TMessage).Name;

            // Use SNS for pub/sub if topic ARN is provided
            if (!string.IsNullOrWhiteSpace(_options.AwsSqsSns!.DefaultTopicArn))
            {
                _snsClient ??= CreateSnsClient();

                var publishRequest = new PublishRequest
                {
                    TopicArn = options?.RoutingKey ?? _options.AwsSqsSns.DefaultTopicArn,
                    Message = messageBody,
                    Subject = messageType
                };

                if (options?.Headers != null)
                {
                    foreach (var header in options.Headers)
                    {
                        publishRequest.MessageAttributes[header.Key] = new SnsMessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = header.Value?.ToString() ?? string.Empty
                        };
                    }
                }

                await _snsClient.PublishAsync(publishRequest, cancellationToken);
                _logger?.LogDebug("Published message {MessageType} to SNS topic", typeof(TMessage).Name);
            }
            // Use SQS for direct queue messaging
            else if (!string.IsNullOrWhiteSpace(_options.AwsSqsSns.DefaultQueueUrl))
            {
                _sqsClient ??= CreateSqsClient();

                var sendRequest = new SendMessageRequest
                {
                    QueueUrl = options?.RoutingKey ?? _options.AwsSqsSns.DefaultQueueUrl,
                    MessageBody = messageBody
                };

                sendRequest.MessageAttributes["MessageType"] = new SqsMessageAttributeValue
                {
                    DataType = "String",
                    StringValue = messageType
                };

                if (options?.Headers != null)
                {
                    foreach (var header in options.Headers)
                    {
                        sendRequest.MessageAttributes[header.Key] = new SqsMessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = header.Value?.ToString() ?? string.Empty
                        };
                    }
                }

                if (_options.AwsSqsSns.UseFifoQueue && !string.IsNullOrWhiteSpace(_options.AwsSqsSns.MessageGroupId))
                {
                    sendRequest.MessageGroupId = _options.AwsSqsSns.MessageGroupId;
                    if (!string.IsNullOrWhiteSpace(_options.AwsSqsSns.MessageDeduplicationId))
                    {
                        sendRequest.MessageDeduplicationId = _options.AwsSqsSns.MessageDeduplicationId;
                    }
                }

                await _sqsClient.SendMessageAsync(sendRequest, cancellationToken);
                _logger?.LogDebug("Published message {MessageType} to SQS queue", typeof(TMessage).Name);
            }
            else
            {
                throw new InvalidOperationException("Either DefaultTopicArn or DefaultQueueUrl must be configured.");
            }
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
            _sqsClient ??= CreateSqsClient();
            _pollingCts = new CancellationTokenSource();
            
            var queueUrl = _options.AwsSqsSns!.DefaultQueueUrl 
                ?? throw new InvalidOperationException("DefaultQueueUrl is required for consuming messages.");

            _pollingTask = Task.Run(async () =>
            {
                _logger?.LogInformation("Starting SQS long polling for queue {QueueUrl}", queueUrl);

                while (!_pollingCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var receiveRequest = new ReceiveMessageRequest
                        {
                            QueueUrl = queueUrl,
                            MaxNumberOfMessages = _options.AwsSqsSns.MaxNumberOfMessages,
                            WaitTimeSeconds = (int)_options.AwsSqsSns.WaitTimeSeconds.TotalSeconds,
                            MessageAttributeNames = new List<string> { "All" }
                        };

                        var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, _pollingCts.Token);

                        foreach (var sqsMessage in response.Messages)
                        {
                            await ProcessMessageAsync(sqsMessage, queueUrl, _pollingCts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error in SQS polling loop");
                        await Task.Delay(TimeSpan.FromSeconds(5), _pollingCts.Token);
                    }
                }
            }, _pollingCts.Token);

            _isStarted = true;
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting AWS SQS/SNS message broker");
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
            _logger?.LogInformation("AWS SQS/SNS message broker stopped");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping AWS SQS/SNS message broker");
            throw;
        }
    }

    private async Task ProcessMessageAsync(Message sqsMessage, string queueUrl, CancellationToken cancellationToken)
    {
        try
        {
            var messageType = sqsMessage.MessageAttributes.TryGetValue("MessageType", out var typeAttr)
                ? typeAttr.StringValue
                : null;

            var type = !string.IsNullOrWhiteSpace(messageType) 
                ? Type.GetType(messageType) 
                : null;

            if (type == null || !_handlers.ContainsKey(type))
            {
                _logger?.LogWarning("No handler found for message type {MessageType}", messageType);
                await DeleteMessageAsync(queueUrl, sqsMessage.ReceiptHandle, cancellationToken);
                return;
            }

            var message = JsonSerializer.Deserialize(sqsMessage.Body, type);

            if (message == null)
            {
                _logger?.LogWarning("Failed to deserialize message of type {MessageType}", messageType);
                await DeleteMessageAsync(queueUrl, sqsMessage.ReceiptHandle, cancellationToken);
                return;
            }

            var context = new MessageContext
            {
                MessageId = sqsMessage.MessageId,
                Timestamp = DateTimeOffset.UtcNow,
                Headers = sqsMessage.MessageAttributes.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => (object)kvp.Value.StringValue),
                Acknowledge = async () => 
                    await DeleteMessageAsync(queueUrl, sqsMessage.ReceiptHandle, cancellationToken),
                Reject = async (requeue) =>
                {
                    if (!requeue)
                    {
                        await DeleteMessageAsync(queueUrl, sqsMessage.ReceiptHandle, cancellationToken);
                    }
                    // For requeue, we simply don't delete the message
                }
            };

            var handlers = _handlers[type];
            foreach (var handler in handlers)
            {
                await handler(message, context, cancellationToken);
            }

            // Auto-acknowledge if enabled
            if (_options.AwsSqsSns!.AutoDeleteMessages && context.Acknowledge != null)
            {
                await context.Acknowledge();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing SQS message {MessageId}", sqsMessage.MessageId);
        }
    }

    private async Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken cancellationToken)
    {
        try
        {
            await _sqsClient!.DeleteMessageAsync(queueUrl, receiptHandle, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting SQS message");
        }
    }

    private AmazonSQSClient CreateSqsClient()
    {
        var region = RegionEndpoint.GetBySystemName(_options.AwsSqsSns!.Region);
        
        if (!string.IsNullOrWhiteSpace(_options.AwsSqsSns.AccessKeyId) && 
            !string.IsNullOrWhiteSpace(_options.AwsSqsSns.SecretAccessKey))
        {
            return new AmazonSQSClient(
                _options.AwsSqsSns.AccessKeyId,
                _options.AwsSqsSns.SecretAccessKey,
                region);
        }
        
        return new AmazonSQSClient(region);
    }

    private AmazonSimpleNotificationServiceClient CreateSnsClient()
    {
        var region = RegionEndpoint.GetBySystemName(_options.AwsSqsSns!.Region);
        
        if (!string.IsNullOrWhiteSpace(_options.AwsSqsSns.AccessKeyId) && 
            !string.IsNullOrWhiteSpace(_options.AwsSqsSns.SecretAccessKey))
        {
            return new AmazonSimpleNotificationServiceClient(
                _options.AwsSqsSns.AccessKeyId,
                _options.AwsSqsSns.SecretAccessKey,
                region);
        }
        
        return new AmazonSimpleNotificationServiceClient(region);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        
        _sqsClient?.Dispose();
        _snsClient?.Dispose();
    }
}
