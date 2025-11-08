namespace Relay.Core.Testing;

/// <summary>
/// Defines the types of mock behaviors.
/// </summary>
internal enum MockBehaviorType
{
    Return,
    ReturnFactory,
    ReturnAsyncFactory,
    Throw,
    ThrowFactory
}