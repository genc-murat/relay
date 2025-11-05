using System;
using Relay.Core.AI.Pipeline.Interfaces;

namespace Relay.Core.AI.Pipeline.Options
{
    /// <summary>
    /// Configuration options for SystemLoadMetricsProvider.
    /// </summary>
    public class SystemLoadMetricsOptions
    {
        /// <summary>
        /// Gets or sets whether metrics caching is enabled.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the cache TTL.
        /// </summary>
        public TimeSpan CacheTtl { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the cache refresh interval.
        /// </summary>
        public TimeSpan CacheRefreshInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets whether to use cached CPU measurements.
        /// </summary>
        public bool UseCachedCpuMeasurements { get; set; } = true;

        /// <summary>
        /// Gets or sets the CPU measurement interval (for non-blocking measurements).
        /// </summary>
        public int CpuMeasurementIntervalMs { get; set; } = 10;

        /// <summary>
        /// Gets or sets the baseline memory for utilization calculation (bytes).
        /// </summary>
        public long BaselineMemory { get; set; } = 1024L * 1024 * 1024; // 1GB

        /// <summary>
        /// Gets or sets whether to enable detailed logging.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets the active request counter (for testing/injection).
        /// </summary>
        public IActiveRequestCounter? ActiveRequestCounter { get; set; }
    }
}