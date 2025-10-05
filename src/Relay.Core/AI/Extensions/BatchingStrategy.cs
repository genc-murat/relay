namespace Relay.Core.AI
{
    /// <summary>
    /// Available batching strategies.
    /// </summary>
    public enum BatchingStrategy
    {
        /// <summary>Fixed batch size</summary>
        Fixed,
        
        /// <summary>Dynamic batch size based on system load</summary>
        Dynamic,
        
        /// <summary>AI-predicted optimal batch size</summary>
        AIPredictive,
        
        /// <summary>Time-based batching</summary>
        TimeBased,
        
        /// <summary>Adaptive batching based on throughput</summary>
        Adaptive,
        
        /// <summary>Batching based on both size and time</summary>
        SizeAndTime
    }
}