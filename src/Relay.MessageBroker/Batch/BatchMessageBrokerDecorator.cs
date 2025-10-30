using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.MessageBroker.Batch;

/// <summary>
/// Decorator that adds batch processing capabilities to an IMessageBroker implementation.
/// </summary>
public sealed class BatchMessageBrokerDecorator : IMessageBroker, IAsyncDisposable
{
    private readonly IMessageBroker _innerBroker;
    private readonly BatchOptions _options;
    private readonly ILogger<BatchMessageBrokerDecorator> _logger;
    private readonly Dictionary<Type, object> _batchProcessors;
    private readonly SemaphoreSlim _lock;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchMessageBrokerDecorator"/> class.
    /// </summary>
    /// <param name="innerBroker">The inner message broker to decorate.</param>
    /// <param name="options">The batch processing options.</param>
    /// <param name="logger">The logger.</param>
    public BatchMessageBrokerDecorator(
        IMessageBroker innerBroker,
        IOptions<BatchOptions> options,
        ILogger<BatchMessageBrokerDecorator> logger)
    {
        _innerBroker = innerBroker ?? throw new ArgumentNullException(nameof(innerBroker));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options.Validate();

        _batchProcessors = new Dictionary<Type, object>();
        _lock = new SemaphoreSlim(1, 1);

        _logger.LogInformation(
            "BatchMessageBrokerDecorator initialized. Batching enabled: {Enabled}",
            _options.Enabled);
    }

    /// <inheritdoc/>
    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // If batching is disabled, publish directly
        if (!_options.Enabled)
        {
            await _innerBroker.PublishAsync(message, options, cancellationToken);
            return;
        }

        // Get or create batch processor for this message type
        var batchProcessor = await GetOrCreateBatchProcessorAsync<TMessage>(cancellationToken);

        // Add message to batch
        await batchProcessor.AddAsync(message, options, cancellationToken);

        _logger.LogTrace(
            "Message of type {MessageType} added to batch",
            typeof(TMessage).Name);
    }

    /// <inheritdoc/>
    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Subscription is not affected by batching, delegate to inner broker
        return _innerBroker.SubscribeAsync(handler, options, cancellationToken);
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
    /// Gets or creates a batch processor for the specified message type.
    /// </summary>
    private async ValueTask<IBatchProcessor<TMessage>> GetOrCreateBatchProcessorAsync<TMessage>(
        CancellationToken cancellationToken)
    {
        var messageType = typeof(TMessage);

        // Fast path: check without lock
        if (_batchProcessors.TryGetValue(messageType, out var existingProcessor))
        {
            return (IBatchProcessor<TMessage>)existingProcessor;
        }

        // Slow path: create with lock
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_batchProcessors.TryGetValue(messageType, out existingProcessor))
            {
                return (IBatchProcessor<TMessage>)existingProcessor;
            }

            // Create new batch processor
            var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            var processorLogger = loggerFactory.CreateLogger<BatchProcessor<TMessage>>();
            
            var processor = new BatchProcessor<TMessage>(
                _innerBroker,
                Options.Create(_options),
                processorLogger);

            _batchProcessors[messageType] = processor;

            _logger.LogDebug(
                "Created batch processor for message type {MessageType}",
                messageType.Name);

            return processor;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets the batch processor metrics for a specific message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <returns>The batch processor metrics, or null if no processor exists for this type.</returns>
    public BatchProcessorMetrics? GetMetrics<TMessage>()
    {
        var messageType = typeof(TMessage);
        if (_batchProcessors.TryGetValue(messageType, out var processor))
        {
            return ((IBatchProcessor<TMessage>)processor).GetMetrics();
        }
        return null;
    }

    /// <summary>
    /// Flushes all pending batches for all message types.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask FlushAllAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogInformation("Flushing all pending batches");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var flushTasks = _batchProcessors.Values
                .Cast<dynamic>()
                .Select(async processor =>
                {
                    try
                    {
                        await processor.FlushAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error flushing batch processor");
                    }
                });

            await Task.WhenAll(flushTasks);
        }
        finally
        {
            _lock.Release();
        }

        _logger.LogInformation("All batches flushed");
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _logger.LogInformation("Disposing BatchMessageBrokerDecorator");

        // Flush and dispose all batch processors
        await _lock.WaitAsync();
        try
        {
            foreach (var processor in _batchProcessors.Values.Cast<IAsyncDisposable>())
            {
                try
                {
                    await processor.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing batch processor");
                }
            }

            _batchProcessors.Clear();
        }
        finally
        {
            _lock.Release();
        }

        _lock.Dispose();

        // Dispose inner broker if it's disposable
        if (_innerBroker is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }

        _logger.LogInformation("BatchMessageBrokerDecorator disposed");
    }
}
