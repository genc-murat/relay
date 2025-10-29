using System.Collections.Generic;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Equality comparer for HandlerClassInfo that uses value-based equality.
    /// Used by incremental generator pipeline for efficient caching.
    /// </summary>
    public sealed class HandlerClassInfoComparer : IEqualityComparer<HandlerClassInfo?>
    {
        /// <summary>
        /// Singleton instance of the comparer.
        /// </summary>
        public static readonly HandlerClassInfoComparer Instance = new();

        // Private constructor to enforce singleton pattern
        private HandlerClassInfoComparer()
        {
        }

        /// <summary>
        /// Determines whether two HandlerClassInfo instances are equal.
        /// </summary>
        public bool Equals(HandlerClassInfo? x, HandlerClassInfo? y)
        {
            // Handle null cases
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            // Delegate to HandlerClassInfo's IEquatable implementation
            return x.Equals(y);
        }

        /// <summary>
        /// Returns a hash code for the specified HandlerClassInfo.
        /// </summary>
        public int GetHashCode(HandlerClassInfo? obj)
        {
            // Handle null case
            if (obj is null) return 0;

            // Delegate to HandlerClassInfo's GetHashCode implementation
            return obj.GetHashCode();
        }
    }
}
