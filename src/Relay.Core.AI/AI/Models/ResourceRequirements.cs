namespace Relay.Core.AI
{
    /// <summary>
    /// Resource requirements for an optimization strategy.
    /// </summary>
    public sealed class ResourceRequirements
    {
        /// <summary>
        /// Memory requirement in MB.
        /// </summary>
        public int MemoryMB { get; set; }

        /// <summary>
        /// CPU requirement as percentage (0-100).
        /// </summary>
        public int CpuPercent { get; set; }

        /// <summary>
        /// Network bandwidth requirement in Mbps.
        /// </summary>
        public int NetworkMbps { get; set; }

        /// <summary>
        /// Disk I/O requirement in operations per second.
        /// </summary>
        public int DiskIops { get; set; }
    }
}