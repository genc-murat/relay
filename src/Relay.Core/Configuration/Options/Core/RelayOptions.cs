using System;
using System.Collections.Generic;
using Relay.Core.Configuration.Options.Authorization;
using Relay.Core.Configuration.Options.Caching;
using Relay.Core.Configuration.Options.ContractValidation;
using Relay.Core.Configuration.Options.DistributedTracing;
using Relay.Core.Configuration.Options.Endpoints;
using Relay.Core.Configuration.Options.EventSourcing;
using Relay.Core.Configuration.Options.Handlers;
using Relay.Core.Configuration.Options.MessageQueue;
using Relay.Core.Configuration.Options.Notifications;
using Relay.Core.Configuration.Options.Performance;
using Relay.Core.Configuration.Options.RateLimiting;
using Relay.Core.Configuration.Options.Retry;

namespace Relay.Core.Configuration.Options.Core;

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
    /// Gets or sets the performance optimization options.
    /// </summary>
    public PerformanceOptions Performance { get; set; } = new();

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
