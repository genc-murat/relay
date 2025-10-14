using System;

namespace Relay.SourceGenerator
{
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
}