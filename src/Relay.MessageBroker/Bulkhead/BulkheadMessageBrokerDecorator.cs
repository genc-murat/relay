using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.MessageBroker.Bulkhead;

/// <summary>
/// Decorator that adds bulkhead pattern capabilities to an IMessageBroker implementation.
/// Provides resource isolation and prevents cascading failures.
/// </summary>
public sealed class BulkheadMessageBrokerDecorator : IMessageBroker, IAsyncDisposable
{
    private readonly IMessageBroker _innerBroker;
    private readonly IBulkhead _publishBulkhead;
    private readonly IBulkhead _subscribeBulkhead;
    private readonly BulkheadOptions _options;
    private readonly ILogger<BulkheadMessageBrokerDecorator> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkheadMessageBrokerDecorator"/> class.
    /// </summary>
    /// <param name="innerBroker">The inner message broker to decorate.</param>
    /// <param name="publishBulkhead">The bulkhead for publish operations.</param>
    /// <param name="subscribeBulkhead">The bulkhead for subscribe operations.</param>
    /// <param name="options">The bulkhead options.</param>
    /// <param name="logger">The logger.</param>
    public BulkheadMessageBrokerDecorator(
        IMessageBroker innerBroker,
        IBulkhead publishBulkhead,
        IBulkhead subscribeBulkhead,
        IOptions<BulkheadOptions> options,
        ILogger<BulkheadMessageBrokerDecorator> logger)
    {
        _innerBroker = innerBroker ?? throw new ArgumentNullException(nameof(innerBroker));
        _publishBulkhead = publishBulkhead ?? throw new ArgumentNullException(nameof(publishBulkhead));
        _subscribeBulkhead = subscribeBulkhead ?? throw new ArgumentNullException(nameof(subscribeBulkhead));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options.Validate();

        _logger.LogInformation(
            "BulkheadMessageBrokerDecorator initialized. Bulkhead enabled: {Enabled}, Max concurrent: {MaxConcurrent}",
            _options.Enabled,
            _options.MaxConcurrentOperations);
    }

    /// <inheritdoc/>
    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // If bulkhead is disabled, publish directly
        if (!_options.Enabled)
        {
            await _innerBroker.PublishAsync(message, options, cancellationToken);
            return;
        }

        try
        {
            _logger.LogTrace(
                "Executing publish operation within bulkhead for message type {MessageType}",
                typeof(TMessage).Name);

            // Execute publish within bulkhead
            await _publishBulkhead.ExecuteAsync(
                async ct => 
                {
                    await _innerBroker.PublishAsync(message, options, ct);
                    return true; // Return value required by ExecuteAsync signature
                },
                cancellationToken);

            _logger.LogTrace(
                "Successfully published message type {MessageType} within bulkhead",
                typeof(TMessage).Name);
        }
        catch (BulkheadRejectedException ex)
        {
            _logger.LogWarning(
                ex,
                "Bulkhead rejected publish operation for message type {MessageType}. Active: {Active}, Queued: {Queued}",
                typeof(TMessage).Name,
                ex.ActiveOperations,
                ex.QueuedOperations);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error during bulkhead-protected publish of message type {MessageType}",
                typeof(TMessage).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // If bulkhead is disabled, subscribe directly
        if (!_options.Enabled)
        {
            return _innerBroker.SubscribeAsync(handler, options, cancellationToken);
        }

        // Wrap the handler to execute within bulkhead
        async ValueTask BulkheadHandler(TMessage message, MessageContext context, CancellationToken ct)
        {
            try
            {
                _logger.LogTrace(
                    "Executing subscribe handler within bulkhead for message type {MessageType}",
                    typeof(TMessage).Name);

                // Execute handler within bulkhead
                await _subscribeBulkhead.ExecuteAsync(
                    async handlerCt =>
                    {
                        await handler(message, context, handlerCt);
                        return true; // Return value required by ExecuteAsync signature
                    },
                    ct);

                _logger.LogTrace(
                    "Successfully processed message type {MessageType} within bulkhead",
                    typeof(TMessage).Name);
            }
            catch (BulkheadRejectedException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Bulkhead rejected subscribe handler for message type {MessageType}. Active: {Active}, Queued: {Queued}",
                    typeof(TMessage).Name,
                    ex.ActiveOperations,
                    ex.QueuedOperations);

                // Reject the message (don't requeue) since we can't process it
                if (context.Reject != null)
                {
                    await context.Reject(false);
                }

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during bulkhead-protected message consumption of type {MessageType}",
                    typeof(TMessage).Name);
                throw;
            }
        }

        // Subscribe with the bulkhead-protected handler
        return _innerBroker.SubscribeAsync<TMessage>(BulkheadHandler, options, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        return _innerBroker.StartAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        return _innerBroker.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the metrics for the publish bulkhead.
    /// </summary>
    /// <returns>The publish bulkhead metrics.</returns>
    public BulkheadMetrics GetPublishMetrics()
    {
        return _publishBulkhead.GetMetrics();
    }

    /// <summary>
    /// Gets the metrics for the subscribe bulkhead.
    /// </summary>
    /// <returns>The subscribe bulkhead metrics.</returns>
    public BulkheadMetrics GetSubscribeMetrics()
    {
        return _subscribeBulkhead.GetMetrics();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _logger.LogInformation("Disposing BulkheadMessageBrokerDecorator");

        // Dispose bulkheads if they're disposable
        if (_publishBulkhead is IDisposable publishDisposable)
        {
            publishDisposable.Dispose();
        }

        if (_subscribeBulkhead is IDisposable subscribeDisposable)
        {
            subscribeDisposable.Dispose();
        }

        // Dispose inner broker if it's disposable
        if (_innerBroker is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }

        _logger.LogInformation("BulkheadMessageBrokerDecorator disposed");
    }
}
