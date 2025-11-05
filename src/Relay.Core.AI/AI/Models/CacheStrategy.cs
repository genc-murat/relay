namespace Relay.Core.AI
{
    /// <summary>
    /// Available caching strategies.
    /// </summary>
    public enum CacheStrategy
    {
        None,
        LRU,
        LFU,
        TimeBasedExpiration,
        SlidingExpiration,
        Adaptive,
        Distributed
    }
}