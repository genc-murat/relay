using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Information about a handler class and its implemented interfaces.
    /// Implements value-based equality for incremental generator caching.
    /// </summary>
    public class HandlerClassInfo : IEquatable<HandlerClassInfo>
    {
        /// <summary>
        /// Gets or sets the class declaration syntax node.
        /// </summary>
        public ClassDeclarationSyntax ClassDeclaration { get; set; } = null!;

        /// <summary>
        /// Gets or sets the semantic symbol for the handler class.
        /// </summary>
        public INamedTypeSymbol ClassSymbol { get; set; } = null!;

        /// <summary>
        /// Gets the list of handler interfaces implemented by this class.
        /// </summary>
        public List<HandlerInterfaceInfo> ImplementedInterfaces { get; set; } = new();

        /// <summary>
        /// Determines whether the specified HandlerClassInfo is equal to the current HandlerClassInfo.
        /// Uses value-based equality for incremental generator caching optimization.
        /// </summary>
        public bool Equals(HandlerClassInfo? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            // Compare class symbols using Roslyn's symbol equality comparer
            if (!SymbolEqualityComparer.Default.Equals(ClassSymbol, other.ClassSymbol))
                return false;

            // Compare implemented interfaces count first (fast path)
            if (ImplementedInterfaces.Count != other.ImplementedInterfaces.Count)
                return false;

            // Compare each interface using value-based equality
            return ImplementedInterfaces.SequenceEqual(other.ImplementedInterfaces);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current HandlerClassInfo.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return Equals(obj as HandlerClassInfo);
        }

        /// <summary>
        /// Returns a hash code for the current HandlerClassInfo.
        /// Uses value-based hashing for incremental generator caching optimization.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                // Start with class symbol hash
                int hash = SymbolEqualityComparer.Default.GetHashCode(ClassSymbol);
                
                // Combine with each interface hash using a prime multiplier
                foreach (var iface in ImplementedInterfaces)
                {
                    hash = (hash * 397) ^ iface.GetHashCode();
                }
                
                return hash;
            }
        }
    }
}