using System;

namespace Relay.Core.RateLimiting.Implementations;

public partial class SlidingWindowRateLimiter
{
    /// <summary>
    /// Internal state for sliding window tracking
    /// </summary>
    private class SlidingWindowState
    {
        public object Lock { get; } = new object();
        public DateTimeOffset CurrentWindowStart { get; set; }
        public DateTimeOffset PreviousWindowStart { get; set; }
        public int CurrentWindowCount { get; set; }
        public int PreviousWindowCount { get; set; }
        public DateTimeOffset LastAccessTime { get; set; }
    }
}
