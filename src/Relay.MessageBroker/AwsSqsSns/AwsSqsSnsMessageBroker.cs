using System.Text.Json;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Relay.MessageBroker.Compression;
using Relay.Core.ContractValidation;
using Relay.MessageBroker.PoisonMessage;
using Relay.MessageBroker.Backpressure;
using SqsMessageAttributeValue = Amazon.SQS.Model.MessageAttributeValue;
using SnsMessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace Relay.MessageBroker.AwsSqsSns;

/// <summary>
/// AWS SQS/SNS implementation of message broker.
/// </summary>
public sealed class AwsSqsSnsMessageBroker : BaseMessageBroker
{
    private readonly Dictionary<string, CancellationTokenSource> _consumerTasks = new();
    private readonly AsyncRetryPolicy _sqsRetryPolicy;
    private readonly AsyncRetryPolicy _snsRetryPolicy;
    private readonly AsyncCircuitBreakerPolicy _sqsCircuitBreaker;
    private readonly AsyncCircuitBreakerPolicy _snsCircuitBreaker;
    private readonly AsyncTimeoutPolicy _sqsTimeoutPolicy;
    private readonly AsyncTimeoutPolicy _snsTimeoutPolicy;
    private AmazonSQSClient? _sqsClient;
    private AmazonSimpleNotificationServiceClient? _snsClient;
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;

    public AwsSqsSnsMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger<AwsSqsSnsMessageBroker> logger,
        IMessageCompressor? compressor = null,
        IContractValidator? contractValidator = null,
        IPoisonMessageHandler? poisonMessageHandler = null,
        IBackpressureController? backpressureController = null)
        : base(options, logger, compressor, contractValidator, poisonMessageHandler, backpressureController)
    {
        if (_options.AwsSqsSns == null)
            throw new InvalidOperationException("AWS SQS/SNS options are required.");

        // Configure retry policies for AWS operations
        _sqsRetryPolicy = Policy
            .Handle<AmazonSQSException>(ex => ex.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: _options.RetryPolicy?.MaxAttempts ?? 3,
                sleepDurationProvider: retryAttempt => 
                    TimeSpan.FromMilliseconds(Math.Min(
                        1000 * Math.Pow(2, retryAttempt), 
                        (_options.RetryPolicy?.MaxDelay ?? TimeSpan.FromSeconds(30)).TotalMilliseconds)));

        _snsRetryPolicy = Policy
            .Handle<AmazonSimpleNotificationServiceException>(ex => ex.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: _options.RetryPolicy?.MaxAttempts ?? 3,
                sleepDurationProvider: retryAttempt => 
                    TimeSpan.FromMilliseconds(Math.Min(
                        1000 * Math.Pow(2, retryAttempt), 
                        (_options.RetryPolicy?.MaxDelay ?? TimeSpan.FromSeconds(30)).TotalMilliseconds)));

        // Configure circuit breaker policies
        _sqsCircuitBreaker = Policy
            .Handle<AmazonSQSException>()
            .Or<HttpRequestException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: _options.CircuitBreaker?.FailureThreshold ?? 5,
                durationOfBreak: _options.CircuitBreaker?.Timeout ?? TimeSpan.FromSeconds(30));

        _snsCircuitBreaker = Policy
            .Handle<AmazonSimpleNotificationServiceException>()
            .Or<HttpRequestException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: _options.CircuitBreaker?.FailureThreshold ?? 5,
                durationOfBreak: _options.CircuitBreaker?.Timeout ?? TimeSpan.FromSeconds(30));

        // Configure timeout policies
        _sqsTimeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(30));
        _snsTimeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(30));
    }

protected override async ValueTask PublishInternalAsync<TMessage>(
        TMessage message,
        byte[] serializedMessage,
        PublishOptions? options,
        CancellationToken cancellationToken)
    {
        var messageBody = System.Text.Encoding.UTF8.GetString(serializedMessage);
        var messageType = typeof(TMessage).AssemblyQualifiedName ?? typeof(TMessage).FullName ?? typeof(TMessage).Name;

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

            await _snsTimeoutPolicy.WrapAsync(_snsCircuitBreaker.WrapAsync(_snsRetryPolicy))
                .ExecuteAsync(async () =>
                {
                    await _snsClient!.PublishAsync(publishRequest, cancellationToken);
                });
            _logger.LogDebug("Published message {MessageType} to SNS topic", typeof(TMessage).Name);
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

            await _sqsTimeoutPolicy.WrapAsync(_sqsCircuitBreaker.WrapAsync(_sqsRetryPolicy))
                .ExecuteAsync(async () =>
                {
                    await _sqsClient!.SendMessageAsync(sendRequest, cancellationToken);
                });
            _logger.LogDebug("Published message {MessageType} to SQS queue", typeof(TMessage).Name);
        }
        else
        {
            throw new InvalidOperationException("Either DefaultTopicArn or DefaultQueueUrl must be configured.");
        }
    }

protected override async ValueTask SubscribeInternalAsync(
        Type messageType,
        SubscriptionInfo subscriptionInfo,
        CancellationToken cancellationToken)
    {
        // AWS SQS/SNS handles subscriptions in StartInternalAsync
        await ValueTask.CompletedTask;
    }

protected override async ValueTask StartInternalAsync(CancellationToken cancellationToken)
    {
        _sqsClient ??= CreateSqsClient();
        _pollingCts = new CancellationTokenSource();
        
        var queueUrl = _options.AwsSqsSns!.DefaultQueueUrl 
            ?? throw new InvalidOperationException("DefaultQueueUrl is required for consuming messages.");

        _pollingTask = Task.Run(async () =>
        {
            _logger.LogInformation("Starting SQS long polling for queue {QueueUrl}", queueUrl);

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

                    var response = await _sqsTimeoutPolicy.WrapAsync(_sqsCircuitBreaker.WrapAsync(_sqsRetryPolicy))
                        .ExecuteAsync(async () =>
                        {
                            return await _sqsClient.ReceiveMessageAsync(receiveRequest, _pollingCts.Token);
                        });

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
                    _logger.LogError(ex, "Error in SQS polling loop");
                    await Task.Delay(TimeSpan.FromSeconds(5), _pollingCts.Token);
                }
            }
        }, _pollingCts.Token);
    }

    protected override async ValueTask StopInternalAsync(CancellationToken cancellationToken)
    {
        _pollingCts?.Cancel();

        if (_pollingTask != null)
        {
            try
            {
                await _pollingTask;
            }
            catch (TaskCanceledException)
            {
                // Expected when canceling the polling task
            }
        }

        _pollingCts?.Dispose();
        _pollingCts = null;
        _pollingTask = null;

        _logger.LogInformation("AWS SQS/SNS message broker stopped");
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

            if (type == null)
            {
                _logger.LogWarning("No handler found for message type {MessageType}", messageType);
                await DeleteMessageAsync(queueUrl, sqsMessage.ReceiptHandle, cancellationToken);
                return;
            }

            var messageBytes = System.Text.Encoding.UTF8.GetBytes(sqsMessage.Body);
            var message = System.Text.Json.JsonSerializer.Deserialize(sqsMessage.Body, type);

            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize message of type {MessageType}", messageType);
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

            await ProcessMessageAsync(message, type, context, cancellationToken);

            // Auto-acknowledge if enabled
            if (_options.AwsSqsSns!.AutoDeleteMessages && context.Acknowledge != null)
            {
                await context.Acknowledge();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SQS message {MessageId}", sqsMessage.MessageId);
        }
    }

    private async Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken cancellationToken)
    {
        try
        {
            await _sqsTimeoutPolicy.WrapAsync(_sqsCircuitBreaker.WrapAsync(_sqsRetryPolicy))
                .ExecuteAsync(async () =>
                {
                    await _sqsClient!.DeleteMessageAsync(queueUrl, receiptHandle, cancellationToken);
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting SQS message {ReceiptHandle}", receiptHandle);
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

protected override async ValueTask DisposeInternalAsync()
    {
        _sqsClient?.Dispose();
        _snsClient?.Dispose();
    }
}
