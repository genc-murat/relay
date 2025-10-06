using System;

namespace Relay.Core.AI
{
    public class MetricTrendData
    {
        public string MetricName { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public double MA5 { get; set; }
        public double MA15 { get; set; }
        public TrendDirection Trend { get; set; }
    }
}
