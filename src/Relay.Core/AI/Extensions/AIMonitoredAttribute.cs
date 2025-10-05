using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Marks a request type for AI-powered monitoring and optimization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class AIMonitoredAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the monitoring level.
        /// </summary>
        public MonitoringLevel Level { get; set; } = MonitoringLevel.Standard;

        /// <summary>
        /// Gets or sets whether to collect detailed execution metrics.
        /// </summary>
        public bool CollectDetailedMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to track user access patterns.
        /// </summary>
        public bool TrackAccessPatterns { get; set; } = true;

        /// <summary>
        /// Gets or sets the sampling rate for metrics collection (0.0 to 1.0).
        /// </summary>
        public double SamplingRate { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets custom tags for categorizing this request type.
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();
    }
}