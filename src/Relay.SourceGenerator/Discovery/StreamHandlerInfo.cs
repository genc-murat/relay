using System;

namespace Relay.SourceGenerator
{
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
}