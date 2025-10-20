using System;

namespace Relay.Core.AI
{
    public class RequestContext
    {
        public Type RequestType { get; init; } = null!;
        public object Request { get; init; } = null!;
        public RequestExecutionMetrics HistoricalMetrics { get; init; } = null!;
        public SystemLoadMetrics CurrentLoad { get; init; } = null!;
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}