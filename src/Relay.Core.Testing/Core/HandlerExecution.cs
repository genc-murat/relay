using System;

namespace Relay.Core.Testing;

public class HandlerExecution
{
    public Type RequestType { get; set; } = null!;
    public Type? ResponseType { get; set; }
    public string? HandlerName { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
}
