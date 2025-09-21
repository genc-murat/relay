using System;

namespace Relay.Core.Caching
{
    /// <summary>
    /// Enables caching for a request handler. When applied to an IRequest class,
    /// the result of the handler will be cached for the specified duration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class CacheAttribute : Attribute
    {
        /// <summary>
        /// Gets the absolute expiration time for the cache, in seconds.
        /// </summary>
        public int AbsoluteExpirationSeconds { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheAttribute"/> class.
        /// </summary>
        /// <param name="absoluteExpirationSeconds">The absolute expiration time for the cache, in seconds.</param>
        public CacheAttribute(int absoluteExpirationSeconds)
        {
            if (absoluteExpirationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(absoluteExpirationSeconds), "Cache duration must be a positive number.");
            }
            AbsoluteExpirationSeconds = absoluteExpirationSeconds;
        }
    }
}
