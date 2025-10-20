using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Provides performance hints to the AI optimization engine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class PerformanceHintAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the PerformanceHintAttribute.
        /// </summary>
        /// <param name="hint">The performance hint message</param>
        public PerformanceHintAttribute(string hint)
        {
            Hint = hint ?? throw new ArgumentNullException(nameof(hint));
        }

        /// <summary>
        /// Gets the performance hint message.
        /// </summary>
        public string Hint { get; }

        /// <summary>
        /// Gets or sets the hint category.
        /// </summary>
        public string Category { get; set; } = "General";

        /// <summary>
        /// Gets or sets the priority of this hint.
        /// </summary>
        public OptimizationPriority Priority { get; set; } = OptimizationPriority.Medium;

        /// <summary>
        /// Gets or sets whether this is a suggestion or requirement.
        /// </summary>
        public bool IsRequired { get; set; } = false;
    }
}