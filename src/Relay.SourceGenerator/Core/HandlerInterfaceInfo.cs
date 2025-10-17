using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Information about a handler interface implementation.
    /// </summary>
    public class HandlerInterfaceInfo
    {
        /// <summary>
        /// Gets or sets the type of handler interface.
        /// </summary>
        public HandlerType InterfaceType { get; set; }

        /// <summary>
        /// Gets or sets the semantic symbol for the interface.
        /// </summary>
        public INamedTypeSymbol InterfaceSymbol { get; set; } = null!;

        /// <summary>
        /// Gets or sets the request type parameter.
        /// </summary>
        public ITypeSymbol? RequestType { get; set; }

        /// <summary>
        /// Gets or sets the response type parameter (null for void handlers).
        /// </summary>
        public ITypeSymbol? ResponseType { get; set; }
    }
}