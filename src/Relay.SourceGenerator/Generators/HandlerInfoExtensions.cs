using System.Linq;
using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Extension to HandlerInfo to include endpoint attribute information.
    /// </summary>
    internal static class HandlerInfoExtensions
    {
        public static bool HasExposeAsEndpointAttribute(this HandlerInfo handler)
        {
            return handler.GetExposeAsEndpointAttribute() != null;
        }

        public static AttributeData? GetExposeAsEndpointAttribute(this HandlerInfo handler)
        {
            return handler.Attributes
                .FirstOrDefault(attr => attr.Type == RelayAttributeType.ExposeAsEndpoint)
                ?.AttributeData;
        }
    }
}