using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Information about a Relay attribute on a handler method.
    /// </summary>
    public class RelayAttributeInfo
    {
        public RelayAttributeType Type { get; set; }
        public AttributeData? AttributeData { get; set; }
    }
}