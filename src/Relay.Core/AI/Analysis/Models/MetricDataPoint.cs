using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Data point for time-series storage with ML.NET compatibility
    /// </summary>
    internal class MetricDataPoint
    {
        public string MetricName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public float Value { get; set; }
        public float MA5 { get; set; }
        public float MA15 { get; set; }
        public int Trend { get; set; }
        public int HourOfDay { get; set; }
        public int DayOfWeek { get; set; }
    }
}
