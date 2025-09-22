using System;

namespace Relay.Core.Retry
{
    /// <summary>
    /// Attribute to mark handlers or requests that should have retry logic applied.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class RetryAttribute : Attribute
    {
        /// <summary>
        /// Gets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetryAttempts { get; }

        /// <summary>
        /// Gets the retry delay in milliseconds.
        /// </summary>
        public int RetryDelayMilliseconds { get; }

        /// <summary>
        /// Gets the retry strategy type.
        /// </summary>
        public Type? RetryStrategyType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryAttribute"/> class.
        /// </summary>
        /// <param name="maxRetryAttempts">The maximum number of retry attempts.</param>
        /// <param name="retryDelayMilliseconds">The retry delay in milliseconds.</param>
        public RetryAttribute(int maxRetryAttempts, int retryDelayMilliseconds = 1000)
        {
            if (maxRetryAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRetryAttempts), "Max retry attempts must be a positive number.");
            }

            if (retryDelayMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryDelayMilliseconds), "Retry delay must be a non-negative number.");
            }

            MaxRetryAttempts = maxRetryAttempts;
            RetryDelayMilliseconds = retryDelayMilliseconds;
            RetryStrategyType = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryAttribute"/> class with a custom retry strategy.
        /// </summary>
        /// <param name="retryStrategyType">The type of the custom retry strategy.</param>
        /// <param name="maxRetryAttempts">The maximum number of retry attempts.</param>
        public RetryAttribute(Type retryStrategyType, int maxRetryAttempts = 3)
        {
            if (retryStrategyType == null)
            {
                throw new ArgumentNullException(nameof(retryStrategyType));
            }

            if (!typeof(IRetryStrategy).IsAssignableFrom(retryStrategyType))
            {
                throw new ArgumentException($"Retry strategy type must implement {nameof(IRetryStrategy)}.", nameof(retryStrategyType));
            }

            if (maxRetryAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRetryAttempts), "Max retry attempts must be a positive number.");
            }

            MaxRetryAttempts = maxRetryAttempts;
            RetryDelayMilliseconds = 0;
            RetryStrategyType = retryStrategyType;
        }
    }
}