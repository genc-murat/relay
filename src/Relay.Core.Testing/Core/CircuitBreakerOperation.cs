using System;

namespace Relay.Core.Testing;

public class CircuitBreakerOperation
{
    public string CircuitBreakerName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
}
