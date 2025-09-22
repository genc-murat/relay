using System;
using System.Collections.Generic;

namespace Relay.Core.Configuration
{
    /// <summary>
    /// Global configuration options for the Relay framework.
    /// </summary>
    public class RelayOptions
    {
        /// <summary>
        /// Gets or sets the default configuration for request handlers.
        /// </summary>
        public HandlerOptions DefaultHandlerOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the default configuration for notification handlers.
        /// </summary>
        public NotificationOptions DefaultNotificationOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the default configuration for pipeline behaviors.
        /// </summary>
        public PipelineOptions DefaultPipelineOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the default configuration for endpoint generation.
        /// </summary>
        public EndpointOptions DefaultEndpointOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the default configuration for validation.
        /// </summary>
        public ValidationOptions DefaultValidationOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the default configuration for caching.
        /// </summary>
        public CachingOptions DefaultCachingOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the default configuration for rate limiting.
        /// </summary>
        public RateLimitingOptions DefaultRateLimitingOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the default configuration for authorization.
        /// </summary>
        public AuthorizationOptions DefaultAuthorizationOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the default configuration for retry behavior.
        /// </summary>
        public RetryOptions DefaultRetryOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the default configuration for contract validation.
        /// </summary>
        public ContractValidationOptions DefaultContractValidationOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the default configuration for distributed tracing.
        /// </summary>
        public DistributedTracingOptions DefaultDistributedTracingOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the default configuration for handler versioning.
        /// </summary>
        public HandlerVersioningOptions DefaultHandlerVersioningOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the default configuration for event sourcing.
        /// </summary>
        public EventSourcingOptions DefaultEventSourcingOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the default configuration for message queue integration.
        /// </summary>
        public MessageQueueOptions DefaultMessageQueueOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to enable telemetry by default.
        /// </summary>
        public bool EnableTelemetry { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable performance optimizations.
        /// </summary>
        public bool EnablePerformanceOptimizations { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of concurrent notification handlers.
        /// </summary>
        public int MaxConcurrentNotificationHandlers { get; set; } = Environment.ProcessorCount * 2;

        /// <summary>
        /// Gets or sets handler-specific configuration overrides.
        /// </summary>
        public Dictionary<string, HandlerOptions> HandlerOverrides { get; set; } = new();

        /// <summary>
        /// Gets or sets notification-specific configuration overrides.
        /// </summary>
        public Dictionary<string, NotificationOptions> NotificationOverrides { get; set; } = new();

        /// <summary>
        /// Gets or sets pipeline-specific configuration overrides.
        /// </summary>
        public Dictionary<string, PipelineOptions> PipelineOverrides { get; set; } = new();

        /// <summary>
        /// Gets or sets validation-specific configuration overrides.
        /// </summary>
        public Dictionary<string, ValidationOptions> ValidationOverrides { get; set; } = new();

        /// <summary>
        /// Gets or sets caching-specific configuration overrides.
        /// </summary>
        public Dictionary<string, CachingOptions> CachingOverrides { get; set; } = new();

        /// <summary>
        /// Gets or sets rate limiting-specific configuration overrides.
        /// </summary>
        public Dictionary<string, RateLimitingOptions> RateLimitingOverrides { get; set; } = new();

        /// <summary>
        /// Gets or sets authorization-specific configuration overrides.
        /// </summary>
        public Dictionary<string, AuthorizationOptions> AuthorizationOverrides { get; set; } = new();

        /// <summary>
        /// Gets or sets retry-specific configuration overrides.
        /// </summary>
        public Dictionary<string, RetryOptions> RetryOverrides { get; set; } = new();

        /// <summary>
        /// Gets or sets contract validation-specific configuration overrides.
        /// </summary>
        public Dictionary<string, ContractValidationOptions> ContractValidationOverrides { get; set; } = new();

        /// <summary>
        /// Gets or sets distributed tracing-specific configuration overrides.
        /// </summary>
        public Dictionary<string, DistributedTracingOptions> DistributedTracingOverrides { get; set; } = new();

        /// <summary>
        /// Gets or sets handler versioning-specific configuration overrides.
        /// </summary>
        public Dictionary<string, HandlerVersioningOptions> HandlerVersioningOverrides { get; set; } = new();

        /// <summary>
        /// Gets or sets event sourcing-specific configuration overrides.
        /// </summary>
        public Dictionary<string, EventSourcingOptions> EventSourcingOverrides { get; set; } = new();

        /// <summary>
        /// Gets or sets message queue-specific configuration overrides.
        /// </summary>
        public Dictionary<string, MessageQueueOptions> MessageQueueOverrides { get; set; } = new();
    }

    /// <summary>
    /// Configuration options for request handlers.
    /// </summary>
    public class HandlerOptions
    {
        /// <summary>
        /// Gets or sets the default priority for handlers.
        /// </summary>
        public int DefaultPriority { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether to enable caching for handlers.
        /// </summary>
        public bool EnableCaching { get; set; } = false;

        /// <summary>
        /// Gets or sets the default timeout for handler execution.
        /// </summary>
        public TimeSpan? DefaultTimeout { get; set; }

        /// <summary>
        /// Gets or sets whether to enable retry logic for handlers.
        /// </summary>
        public bool EnableRetry { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;
    }

    /// <summary>
    /// Configuration options for notification handlers.
    /// </summary>
    public class NotificationOptions
    {
        /// <summary>
        /// Gets or sets the default dispatch mode for notifications.
        /// </summary>
        public NotificationDispatchMode DefaultDispatchMode { get; set; } = NotificationDispatchMode.Parallel;

        /// <summary>
        /// Gets or sets the default priority for notification handlers.
        /// </summary>
        public int DefaultPriority { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether to continue execution if a notification handler fails.
        /// </summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>
        /// Gets or sets the default timeout for notification handler execution.
        /// </summary>
        public TimeSpan? DefaultTimeout { get; set; }

        /// <summary>
        /// Gets or sets the maximum degree of parallelism for parallel notification dispatch.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    }

    /// <summary>
    /// Configuration options for pipeline behaviors.
    /// </summary>
    public class PipelineOptions
    {
        /// <summary>
        /// Gets or sets the default order for pipeline behaviors.
        /// </summary>
        public int DefaultOrder { get; set; } = 0;

        /// <summary>
        /// Gets or sets the default scope for pipeline behaviors.
        /// </summary>
        public PipelineScope DefaultScope { get; set; } = PipelineScope.All;

        /// <summary>
        /// Gets or sets whether to enable pipeline caching.
        /// </summary>
        public bool EnableCaching { get; set; } = false;

        /// <summary>
        /// Gets or sets the default timeout for pipeline execution.
        /// </summary>
        public TimeSpan? DefaultTimeout { get; set; }
    }

    /// <summary>
    /// Configuration options for endpoint generation.
    /// </summary>
    public class EndpointOptions
    {
        /// <summary>
        /// Gets or sets the default HTTP method for endpoints.
        /// </summary>
        public string DefaultHttpMethod { get; set; } = "POST";

        /// <summary>
        /// Gets or sets the default route prefix for endpoints.
        /// </summary>
        public string? DefaultRoutePrefix { get; set; }

        /// <summary>
        /// Gets or sets the default API version for endpoints.
        /// </summary>
        public string? DefaultVersion { get; set; }

        /// <summary>
        /// Gets or sets whether to enable OpenAPI documentation generation.
        /// </summary>
        public bool EnableOpenApiGeneration { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable automatic route generation.
        /// </summary>
        public bool EnableAutoRouteGeneration { get; set; } = true;
    }
}