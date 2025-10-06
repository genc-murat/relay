using System;

namespace Relay.Core.AI
{
    public class MetricAnomaly
    {
        public string MetricName { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public double ExpectedValue { get; set; }
        public double Deviation { get; set; }
        public double ZScore { get; set; }
        public AnomalySeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
