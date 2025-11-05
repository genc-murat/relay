namespace Relay.Core.AI
{
    /// <summary>
    /// Statistical summary for a metric
    /// </summary>
    public class MetricStatistics
    {
        public string MetricName { get; set; } = string.Empty;
        public int Count { get; set; }
        public float Mean { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public double StdDev { get; set; }
        public float Median { get; set; }
        public float P95 { get; set; }
        public float P99 { get; set; }
    }
}
