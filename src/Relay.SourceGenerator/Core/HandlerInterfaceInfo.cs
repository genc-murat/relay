using System;
using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Information about a handler interface implementation.
    /// Implements value-based equality for incremental generator caching.
    /// </summary>
    public class HandlerInterfaceInfo : IEquatable<HandlerInterfaceInfo>
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

        /// <summary>
        /// Determines whether the specified HandlerInterfaceInfo is equal to the current HandlerInterfaceInfo.
        /// Uses value-based equality for incremental generator caching optimization.
        /// </summary>
        public bool Equals(HandlerInterfaceInfo? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            // Compare interface type (enum comparison is fast)
            if (InterfaceType != other.InterfaceType)
                return false;

            // Compare interface symbols using Roslyn's symbol equality comparer
            if (!SymbolEqualityComparer.Default.Equals(InterfaceSymbol, other.InterfaceSymbol))
                return false;

            // Compare request type symbols
            if (!SymbolEqualityComparer.Default.Equals(RequestType, other.RequestType))
                return false;

            // Compare response type symbols
            if (!SymbolEqualityComparer.Default.Equals(ResponseType, other.ResponseType))
                return false;

            return true;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current HandlerInterfaceInfo.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return Equals(obj as HandlerInterfaceInfo);
        }

        /// <summary>
        /// Returns a hash code for the current HandlerInterfaceInfo.
        /// Uses value-based hashing for incremental generator caching optimization.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)InterfaceType;
                hash = (hash * 397) ^ SymbolEqualityComparer.Default.GetHashCode(InterfaceSymbol);
                hash = (hash * 397) ^ (RequestType != null ? SymbolEqualityComparer.Default.GetHashCode(RequestType) : 0);
                hash = (hash * 397) ^ (ResponseType != null ? SymbolEqualityComparer.Default.GetHashCode(ResponseType) : 0);
                return hash;
            }
        }
    }
}