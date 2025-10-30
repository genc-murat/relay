namespace Relay.MessageBroker.Batch;

/// <summary>
/// Represents a single item in a batch with its associated publish options.
/// </summary>
/// <typeparam name="TMessage">The type of the message.</typeparam>
public sealed class BatchItem<TMessage>
{
    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public required TMessage Message { get; set; }

    /// <summary>
    /// Gets or sets the publish options for this message.
    /// </summary>
    public PublishOptions? Options { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the message was added to the batch.
    /// </summary>
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
}
