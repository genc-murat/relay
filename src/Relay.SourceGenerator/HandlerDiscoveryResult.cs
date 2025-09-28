using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    /// <summary>
    /// Information about a discovered handler.
    /// </summary>
    public class HandlerInfo
    {
        public Type? HandlerType { get; set; }
        public Type? RequestType { get; set; }
        public Type? ResponseType { get; set; }
        public string? MethodName { get; set; }
        public string? HandlerName { get; set; }
        public int Priority { get; set; }
        public bool IsAsync { get; set; }
        public bool HasCancellationToken { get; set; }
        public string? FullTypeName { get; set; }
        public string? Namespace { get; set; }

        // Additional properties for compatibility with old HandlerInfo
        public MethodDeclarationSyntax? Method { get; set; }
        public IMethodSymbol? MethodSymbol { get; set; }
        public List<RelayAttributeInfo> Attributes { get; set; } = new();
        public ITypeSymbol? RequestTypeSymbol { get; set; }
        public ITypeSymbol? ResponseTypeSymbol { get; set; }
        public ITypeSymbol? HandlerTypeSymbol { get; set; }
    }

    /// <summary>
    /// Information about a discovered notification handler.
    /// </summary>
    public class NotificationHandlerInfo
    {
        public Type? HandlerType { get; set; }
        public Type? NotificationType { get; set; }
        public string? MethodName { get; set; }
        public int Priority { get; set; }
        public bool IsAsync { get; set; }
        public bool HasCancellationToken { get; set; }
        public string? DispatchMode { get; set; }
        public string? FullTypeName { get; set; }
        public string? Namespace { get; set; }
    }

    /// <summary>
    /// Information about a discovered pipeline behavior.
    /// </summary>
    public class PipelineBehaviorInfo
    {
        public Type? BehaviorType { get; set; }
        public Type? RequestType { get; set; }
        public Type? ResponseType { get; set; }
        public string? MethodName { get; set; }
        public int Order { get; set; }
        public string? Scope { get; set; }
        public bool IsAsync { get; set; }
        public string? FullTypeName { get; set; }
        public string? Namespace { get; set; }
    }

    /// <summary>
    /// Information about a discovered stream handler.
    /// </summary>
    public class StreamHandlerInfo
    {
        public Type? HandlerType { get; set; }
        public Type? RequestType { get; set; }
        public Type? ResponseType { get; set; }
        public string? MethodName { get; set; }
        public string? HandlerName { get; set; }
        public int Priority { get; set; }
        public bool IsAsync { get; set; }
        public bool HasCancellationToken { get; set; }
        public string? FullTypeName { get; set; }
        public string? Namespace { get; set; }
    }

    /// <summary>
    /// Information about a Relay attribute on a handler method.
    /// </summary>
    public class RelayAttributeInfo
    {
        public RelayAttributeType Type { get; set; }
        public AttributeData? AttributeData { get; set; }
    }

    /// <summary>
    /// Types of Relay attributes.
    /// </summary>
    public enum RelayAttributeType
    {
        None,
        Handle,
        Notification,
        Pipeline,
        ExposeAsEndpoint,
        Stream
    }
}