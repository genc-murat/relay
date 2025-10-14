using System.Collections.Generic;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Result of handler discovery process.
    /// </summary>
    public class HandlerDiscoveryResult
    {
        /// <summary>
        /// List of discovered handlers.
        /// </summary>
        public List<HandlerInfo> Handlers { get; } = new();

        /// <summary>
        /// List of discovered notification handlers.
        /// </summary>
        public List<NotificationHandlerInfo> NotificationHandlers { get; } = new();

        /// <summary>
        /// List of discovered pipeline behaviors.
        /// </summary>
        public List<PipelineBehaviorInfo> PipelineBehaviors { get; } = new();

        /// <summary>
        /// List of discovered stream handlers.
        /// </summary>
        public List<StreamHandlerInfo> StreamHandlers { get; } = new();
    }
}