using System.Threading.Tasks;

namespace Relay.Core.Security.Interfaces
{
    /// <summary>
    /// Interface for rate limiting functionality.
    /// </summary>
    public interface IRateLimiter
    {
        /// <summary>
        /// Checks if the request is allowed under rate limiting rules.
        /// </summary>
        /// <param name="key">The rate limit key (typically user:requesttype)</param>
        /// <returns>True if allowed, false if rate limit exceeded</returns>
        ValueTask<bool> CheckRateLimitAsync(string key);
    }
}