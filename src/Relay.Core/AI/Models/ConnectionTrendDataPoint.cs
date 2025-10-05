using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Connection trend data point with technical indicators
    /// </summary>
    internal class ConnectionTrendDataPoint
    {
        public DateTime Timestamp { get; set; }
        public int ConnectionCount { get; set; }
        public double MovingAverage5Min { get; set; }
        public double MovingAverage15Min { get; set; }
        public double MovingAverage1Hour { get; set; }
        public string TrendDirection { get; set; } = "stable";
        public double VolatilityScore { get; set; }
    }
}
