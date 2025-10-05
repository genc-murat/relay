using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Represents a transition between load levels
    /// </summary>
    internal class LoadTransition
    {
        public LoadLevel FromLevel { get; set; }
        public LoadLevel ToLevel { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan TimeSincePrevious { get; set; }
        public TimeSpan PerformanceImpact { get; set; }
    }
}
