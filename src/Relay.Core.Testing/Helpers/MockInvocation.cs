using System;

namespace Relay.Core.Testing;

/// <summary>
/// Represents a method invocation on a mock.
/// </summary>
internal class MockInvocation
{
    public object[] Arguments { get; set; }
    public DateTime Timestamp { get; set; }
}
