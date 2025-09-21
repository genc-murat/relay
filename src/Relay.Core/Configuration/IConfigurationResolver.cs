using System;

namespace Relay.Core.Configuration
{
    /// <summary>
    /// Interface for resolving configuration values with attribute parameter overrides.
    /// </summary>
    public interface IConfigurationResolver
    {
        /// <summary>
        /// Resolves handler configuration for a specific handler type and method.
        /// </summary>
        /// <param name="handlerType">The type containing the handler method.</param>
        /// <param name="methodName">The name of the handler method.</param>
        /// <param name="attribute">The handle attribute applied to the method.</param>
        /// <returns>The resolved handler configuration.</returns>
        ResolvedHandlerConfiguration ResolveHandlerConfiguration(Type handlerType, string methodName, HandleAttribute? attribute);

        /// <summary>
        /// Resolves notification configuration for a specific notification handler.
        /// </summary>
        /// <param name="handlerType">The type containing the notification handler method.</param>
        /// <param name="methodName">The name of the notification handler method.</param>
        /// <param name="attribute">The notification attribute applied to the method.</param>
        /// <returns>The resolved notification configuration.</returns>
        ResolvedNotificationConfiguration ResolveNotificationConfiguration(Type handlerType, string methodName, NotificationAttribute? attribute);

        /// <summary>
        /// Resolves pipeline configuration for a specific pipeline behavior.
        /// </summary>
        /// <param name="pipelineType">The type containing the pipeline method.</param>
        /// <param name="methodName">The name of the pipeline method.</param>
        /// <param name="attribute">The pipeline attribute applied to the method.</param>
        /// <returns>The resolved pipeline configuration.</returns>
        ResolvedPipelineConfiguration ResolvePipelineConfiguration(Type pipelineType, string methodName, PipelineAttribute? attribute);

        /// <summary>
        /// Resolves endpoint configuration for a specific endpoint handler.
        /// </summary>
        /// <param name="handlerType">The type containing the endpoint handler method.</param>
        /// <param name="methodName">The name of the endpoint handler method.</param>
        /// <param name="attribute">The endpoint attribute applied to the method.</param>
        /// <returns>The resolved endpoint configuration.</returns>
        ResolvedEndpointConfiguration ResolveEndpointConfiguration(Type handlerType, string methodName, ExposeAsEndpointAttribute? attribute);
    }

    /// <summary>
    /// Resolved configuration for a request handler.
    /// </summary>
    public class ResolvedHandlerConfiguration
    {
        /// <summary>
        /// Gets the handler name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets the handler priority.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets whether caching is enabled for this handler.
        /// </summary>
        public bool EnableCaching { get; set; }

        /// <summary>
        /// Gets the timeout for handler execution.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Gets whether retry logic is enabled for this handler.
        /// </summary>
        public bool EnableRetry { get; set; }

        /// <summary>
        /// Gets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetryAttempts { get; set; }
    }

    /// <summary>
    /// Resolved configuration for a notification handler.
    /// </summary>
    public class ResolvedNotificationConfiguration
    {
        /// <summary>
        /// Gets the dispatch mode for the notification.
        /// </summary>
        public NotificationDispatchMode DispatchMode { get; set; }

        /// <summary>
        /// Gets the notification handler priority.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets whether to continue execution if this handler fails.
        /// </summary>
        public bool ContinueOnError { get; set; }

        /// <summary>
        /// Gets the timeout for notification handler execution.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Gets the maximum degree of parallelism for parallel dispatch.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; }
    }

    /// <summary>
    /// Resolved configuration for a pipeline behavior.
    /// </summary>
    public class ResolvedPipelineConfiguration
    {
        /// <summary>
        /// Gets the execution order of the pipeline behavior.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets the scope of the pipeline behavior.
        /// </summary>
        public PipelineScope Scope { get; set; }

        /// <summary>
        /// Gets whether caching is enabled for this pipeline.
        /// </summary>
        public bool EnableCaching { get; set; }

        /// <summary>
        /// Gets the timeout for pipeline execution.
        /// </summary>
        public TimeSpan? Timeout { get; set; }
    }

    /// <summary>
    /// Resolved configuration for an endpoint.
    /// </summary>
    public class ResolvedEndpointConfiguration
    {
        /// <summary>
        /// Gets the route template for the endpoint.
        /// </summary>
        public string? Route { get; set; }

        /// <summary>
        /// Gets the HTTP method for the endpoint.
        /// </summary>
        public string HttpMethod { get; set; } = "POST";

        /// <summary>
        /// Gets the version of the endpoint.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets whether OpenAPI documentation generation is enabled.
        /// </summary>
        public bool EnableOpenApiGeneration { get; set; }

        /// <summary>
        /// Gets whether automatic route generation is enabled.
        /// </summary>
        public bool EnableAutoRouteGeneration { get; set; }
    }
}