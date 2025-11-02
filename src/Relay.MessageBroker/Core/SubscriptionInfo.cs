namespace Relay.MessageBroker;

/// <summary>
/// Information about a message subscription.
/// </summary>
public sealed class SubscriptionInfo
{
    public Type MessageType { get; set; } = null!;
    public Func<object, MessageContext, CancellationToken, ValueTask> Handler { get; set; } = null!;
    public SubscriptionOptions Options { get; set; } = null!;
}