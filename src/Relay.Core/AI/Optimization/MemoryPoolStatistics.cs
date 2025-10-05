namespace Relay.Core.AI
{
    /// <summary>
    /// Statistics for memory pool operations.
    /// </summary>
    public sealed class MemoryPoolStatistics
    {
        public int BuffersRented { get; set; }
        public int BuffersReturned { get; set; }
        public int PoolHits { get; set; }
        public int PoolMisses { get; set; }
        public long TotalBytesAllocated { get; set; }
        public long EstimatedSavings { get; set; }
        public double Efficiency => PoolHits + PoolMisses > 0 ? (double)PoolHits / (PoolHits + PoolMisses) : 0.0;
        public double PoolEfficiency => BuffersRented > 0 ? (double)BuffersReturned / BuffersRented : 0.0;
    }
}
