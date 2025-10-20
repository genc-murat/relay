using System;

namespace Relay.Core.AI.Pipeline.Options
{
    /// <summary>
    /// Configuration options for AIPerformanceTrackingBehavior.
    /// </summary>
    public class AIPerformanceTrackingOptions
    {
        /// <summary>
        /// Gets or sets whether performance tracking is enabled.
        /// </summary>
        public bool EnableTracking { get; set; } = true;

        /// <summary>
        /// Gets or sets whether detailed logging is enabled.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets whether periodic metrics export is enabled.
        /// </summary>
        public bool EnablePeriodicExport { get; set; } = true;

        /// <summary>
        /// Gets or sets whether immediate export on threshold is enabled.
        /// </summary>
        public bool EnableImmediateExport { get; set; } = false;

        /// <summary>
        /// Gets or sets the interval for periodic metrics export.
        /// </summary>
        public TimeSpan ExportInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the threshold for immediate export (number of metrics).
        /// </summary>
        public int ImmediateExportThreshold { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to reset metrics after export.
        /// </summary>
        public bool ResetAfterExport { get; set; } = true;

        /// <summary>
        /// Gets or sets the sliding window size for metrics aggregation.
        /// </summary>
        public int SlidingWindowSize { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the model version identifier.
        /// </summary>
        public string ModelVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets whether to track percentiles (P50, P95, P99).
        /// </summary>
        public bool TrackPercentiles { get; set; } = true;
    }
}
