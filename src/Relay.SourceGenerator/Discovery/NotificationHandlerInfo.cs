using System;

namespace Relay.SourceGenerator
{
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
}