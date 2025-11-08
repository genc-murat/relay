using System;

namespace Relay.Core.Testing;

public class StreamingOperation
{
    public Type RequestType { get; set; } = null!;
    public Type ResponseType { get; set; } = null!;
    public string? HandlerName { get; set; }
    public TimeSpan Duration { get; set; }
    public long ItemCount { get; set; }
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
}
