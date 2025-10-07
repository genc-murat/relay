using System;

namespace Relay.Core.Telemetry;

/// <summary>
/// Statistics for notification publishing
/// </summary>
public class NotificationPublishStats
{
    public Type NotificationType { get; set; } = null!;
    public long TotalPublishes { get; set; }
    public long SuccessfulPublishes { get; set; }
    public long FailedPublishes { get; set; }
    public double SuccessRate => TotalPublishes > 0 ? (double)SuccessfulPublishes / TotalPublishes : 0;
    public TimeSpan AveragePublishTime { get; set; }
    public TimeSpan MinPublishTime { get; set; }
    public TimeSpan MaxPublishTime { get; set; }
    public double AverageHandlerCount { get; set; }
    public DateTimeOffset LastPublish { get; set; }
}
