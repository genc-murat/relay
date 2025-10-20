using System;

namespace Relay.Core.Security.RateLimiting
{
    /// <summary>
    /// Exception thrown when rate limit is exceeded.
    /// </summary>
    public class RateLimitExceededException : Exception
    {
        public string UserId { get; }
        public string RequestType { get; }

        public RateLimitExceededException(string userId, string requestType)
            : base($"Rate limit exceeded for user {userId} on request type {requestType}")
        {
            UserId = userId;
            RequestType = requestType;
        }
    }
}