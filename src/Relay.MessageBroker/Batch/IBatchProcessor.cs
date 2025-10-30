namespace Relay.MessageBroker.Batch;

/// <summary>
/// Interface for batch processing of messages.
/// </summary>
/// <typeparam name="TMessage">The type of messages to batch.</typeparam>
public interface IBatchProcessor<TMessage> : IAsyncDisposable
{
    /// <summary>
    /// Adds a message to the batch.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <param name="options">Optional publish options for the message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask AddAsync(TMessage message, PublishOptions? options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes all pending messages in the batch.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask FlushAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current batch processing metrics.
    /// </summary>
    /// <returns>The batch processor metrics.</returns>
    BatchProcessorMetrics GetMetrics();
}
