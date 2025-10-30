using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.MessageBroker.Outbox;

/// <summary>
/// Background service that polls the outbox store and publishes pending messages.
/// </summary>
public sealed class OutboxWorker : BackgroundService
{
    private readonly IOutboxStore _store;
    private readonly IMessageBroker _broker;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxWorker"/> class.
    /// </summary>
    /// <param name="store">The outbox store.</param>
    /// <param name="broker">The message broker.</param>
    /// <param name="options">The outbox options.</param>
    /// <param name="logger">The logger.</param>
    public OutboxWorker(
        IOutboxStore store,
        IMessageBroker broker,
        IOptions<OutboxOptions> options,
        ILogger<OutboxWorker> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Validate options
        _options.Validate();
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox worker started with polling interval: {PollingInterval}, batch size: {BatchSize}",
            _options.PollingInterval,
            _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            try
            {
                await Task.Delay(_options.PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
        }

        _logger.LogInformation("Outbox worker stopped");
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        var messages = await _store.GetPendingAsync(_options.BatchSize, cancellationToken);
        var messageList = messages.ToList();

        if (messageList.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Processing {Count} pending outbox messages", messageList.Count);

        foreach (var message in messageList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessMessageAsync(message, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            // Check if we've exceeded max retry attempts
            if (message.RetryCount >= _options.MaxRetryAttempts)
            {
                _logger.LogWarning(
                    "Message {MessageId} of type {MessageType} exceeded max retry attempts ({MaxRetries}), marking as failed",
                    message.Id,
                    message.MessageType,
                    _options.MaxRetryAttempts);

                await _store.MarkAsFailedAsync(
                    message.Id,
                    $"Exceeded maximum retry attempts ({_options.MaxRetryAttempts})",
                    cancellationToken);

                return;
            }

            // Apply exponential backoff if this is a retry
            if (message.RetryCount > 0)
            {
                var delay = CalculateExponentialBackoff(message.RetryCount);
                _logger.LogDebug(
                    "Applying exponential backoff delay of {Delay}ms for message {MessageId} (retry {RetryCount})",
                    delay.TotalMilliseconds,
                    message.Id,
                    message.RetryCount);

                await Task.Delay(delay, cancellationToken);
            }

            // Create publish options from the outbox message
            var publishOptions = new PublishOptions
            {
                RoutingKey = message.RoutingKey,
                Exchange = message.Exchange,
                Headers = message.Headers
            };

            // Publish the message using the raw payload
            // Note: We need to use a generic method, but we don't have the type at runtime
            // So we'll need to deserialize to object or use a non-generic publish method
            // For now, we'll assume the broker can handle byte[] directly
            await PublishMessageAsync(message, publishOptions, cancellationToken);

            await _store.MarkAsPublishedAsync(message.Id, cancellationToken);

            _logger.LogDebug(
                "Successfully published outbox message {MessageId} of type {MessageType}",
                message.Id,
                message.MessageType);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish outbox message {MessageId} of type {MessageType} (retry {RetryCount})",
                message.Id,
                message.MessageType,
                message.RetryCount);

            // Increment retry count but keep status as Pending for next attempt
            message.RetryCount++;
            message.LastError = ex.Message;

            // Only mark as failed if we've reached max retries
            if (message.RetryCount >= _options.MaxRetryAttempts)
            {
                await _store.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }

    private async Task PublishMessageAsync(
        OutboxMessage message,
        PublishOptions options,
        CancellationToken cancellationToken)
    {
        // We need to deserialize the payload back to the original type
        // Since we don't have the type information at runtime, we'll use System.Text.Json
        // to deserialize to a JsonDocument and then publish it
        var jsonDocument = System.Text.Json.JsonDocument.Parse(message.Payload);
        var rootElement = jsonDocument.RootElement;

        // Convert JsonElement to object for publishing
        var messageObject = System.Text.Json.JsonSerializer.Deserialize<object>(message.Payload);

        if (messageObject != null)
        {
            await _broker.PublishAsync(messageObject, options, cancellationToken);
        }
    }

    private TimeSpan CalculateExponentialBackoff(int retryCount)
    {
        // Exponential backoff: baseDelay * 2^(retryCount - 1)
        var multiplier = Math.Pow(2, retryCount - 1);
        var delay = TimeSpan.FromMilliseconds(_options.RetryBaseDelay.TotalMilliseconds * multiplier);

        // Cap at 1 minute to avoid excessive delays
        var maxDelay = TimeSpan.FromMinutes(1);
        return delay > maxDelay ? maxDelay : delay;
    }
}
