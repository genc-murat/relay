namespace Relay.MessageBroker;
/// <summary>
/// Represents a message broker abstraction for publishing and subscribing to messages.
/// </summary>
public interface IMessageBroker
{
    /// <summary>
    /// Publishes a message to the message broker.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">Optional publishing options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask PublishAsync<TMessage>(TMessage message, PublishOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to messages of a specific type.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="handler">The handler to process messages.</param>
    /// <param name="options">Optional subscription options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SubscribeAsync<TMessage>(Func<TMessage, MessageContext, CancellationToken, ValueTask> handler, SubscriptionOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts consuming messages.
    /// </summary>
    ValueTask StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops consuming messages.
    /// </summary>
    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
