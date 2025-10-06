using System;
using Relay.Core.Configuration.Resolved;

namespace Relay.Core.Configuration.Core;

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
