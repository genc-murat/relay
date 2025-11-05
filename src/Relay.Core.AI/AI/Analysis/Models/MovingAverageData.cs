using System;

namespace Relay.Core.AI
{
    public class MovingAverageData
    {
        public double MA5 { get; set; }
        public double MA15 { get; set; }
        public double MA60 { get; set; }
        public double EMA { get; set; }
        public double CurrentValue { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
