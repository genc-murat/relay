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

    /// <summary>
    /// Attribute to mark methods as notification handlers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class NotificationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the dispatch mode for notification handling.
        /// </summary>
        public NotificationDispatchMode DispatchMode { get; set; } = NotificationDispatchMode.Parallel;

        /// <summary>
        /// Gets or sets the priority of the notification handler. Higher values indicate higher priority.
        /// </summary>
        public int Priority { get; set; } = 0;
    }

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

    /// <summary>
    /// Attribute to mark handlers for automatic HTTP endpoint generation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ExposeAsEndpointAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the route template for the endpoint.
        /// </summary>
        public string? Route { get; set; }

        /// <summary>
        /// Gets or sets the HTTP method for the endpoint.
        /// </summary>
        public string HttpMethod { get; set; } = "POST";

        /// <summary>
        /// Gets or sets the version of the endpoint for API versioning.
        /// </summary>
        public string? Version { get; set; }
    }

    /// <summary>
    /// Defines the dispatch mode for notification handlers.
    /// </summary>
    public enum NotificationDispatchMode
    {
        /// <summary>
        /// Execute notification handlers in parallel.
        /// </summary>
        Parallel,

        /// <summary>
        /// Execute notification handlers sequentially.
        /// </summary>
        Sequential
    }

    /// <summary>
    /// Defines the scope for pipeline behaviors.
    /// </summary>
    public enum PipelineScope
    {
        /// <summary>
        /// Apply to all request types.
        /// </summary>
        All,

        /// <summary>
        /// Apply only to regular requests.
        /// </summary>
        Requests,

        /// <summary>
        /// Apply only to streaming requests.
        /// </summary>
        Streams,

        /// <summary>
        /// Apply only to notifications.
        /// </summary>
        Notifications
    }
}