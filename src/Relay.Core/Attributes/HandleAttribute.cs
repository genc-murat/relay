using System;

namespace Relay.Core
{
    /// <summary>
    /// Attribute to mark methods as request handlers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HandleAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the handler. Used for named handler resolution.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the priority of the handler. Higher values indicate higher priority.
        /// </summary>
        public int Priority { get; set; } = 0;
    }
}