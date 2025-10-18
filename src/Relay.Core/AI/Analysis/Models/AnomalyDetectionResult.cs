using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Anomaly detection result
    /// </summary>
    public class AnomalyDetectionResult
    {
        public string MetricName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public float Value { get; set; }
        public bool IsAnomaly { get; set; }
        public float Score { get; set; }
        public float Magnitude { get; set; }
    }
}
