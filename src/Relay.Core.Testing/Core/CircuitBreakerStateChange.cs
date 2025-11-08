namespace Relay.Core.Testing;

public class CircuitBreakerStateChange
{
    public string CircuitBreakerName { get; set; } = string.Empty;
    public string OldState { get; set; } = string.Empty;
    public string NewState { get; set; } = string.Empty;
}
