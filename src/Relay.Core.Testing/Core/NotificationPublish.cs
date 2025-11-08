using System;

namespace Relay.Core.Testing;

public class NotificationPublish
{
    public Type NotificationType { get; set; } = null!;
    public int HandlerCount { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
}
