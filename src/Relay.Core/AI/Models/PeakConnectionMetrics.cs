using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Peak connection metrics
    /// </summary>
    internal class PeakConnectionMetrics
    {
        public int DailyPeak { get; set; }
        public int HourlyPeak { get; set; }
        public int AllTimePeak { get; set; }
        public DateTime LastPeakTimestamp { get; set; }
    }
}
