using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Enables AI-powered memory optimization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SmartMemoryOptimizationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to enable object pooling.
        /// </summary>
        public bool EnableObjectPooling { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use stack allocation where possible.
        /// </summary>
        public bool PreferStackAllocation { get; set; } = true;

        /// <summary>
        /// Gets or sets the memory threshold (in bytes) for optimization consideration.
        /// </summary>
        public long MemoryThreshold { get; set; } = 1024 * 1024; // 1MB

        /// <summary>
        /// Gets or sets whether to enable buffer pooling.
        /// </summary>
        public bool EnableBufferPooling { get; set; } = true;

        /// <summary>
        /// Gets or sets the optimization aggressiveness level.
        /// </summary>
        public OptimizationAggressiveness Aggressiveness { get; set; } = OptimizationAggressiveness.Moderate;
    }
}