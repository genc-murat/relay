using System;

namespace Relay.Core
{
    /// <summary>
    /// Attribute to mark methods as pipeline behaviors.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PipelineAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the execution order of the pipeline behavior. Lower values execute first.
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// Gets or sets the scope of the pipeline behavior.
        /// </summary>
        public PipelineScope Scope { get; set; } = PipelineScope.All;
    }
}