using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Retry
{
    /// <summary>
    /// Exponential backoff retry strategy with jitter.
    /// </summary>
    public class ExponentialBackoffRetryStrategy : IRetryStrategy
    {
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _maxDelay;
        private readonly double _backoffMultiplier;
        private readonly bool _useJitter;
        private readonly Random _random;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialBackoffRetryStrategy"/> class.
        /// </summary>
        /// <param name="initialDelay">The initial delay between retry attempts.</param>
        /// <param name="maxDelay">The maximum delay between retry attempts.</param>
        /// <param name="backoffMultiplier">The multiplier for exponential backoff (default is 2.0).</param>
        /// <param name="useJitter">Whether to add jitter to the delay to prevent thundering herd.</param>
        public ExponentialBackoffRetryStrategy(
            TimeSpan initialDelay,
            TimeSpan maxDelay,
            double backoffMultiplier = 2.0,
            bool useJitter = true)
        {
            if (initialDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(initialDelay), "Initial delay must be non-negative.");
            }

            if (maxDelay < initialDelay)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDelay), "Max delay must be greater than or equal to initial delay.");
            }

            if (backoffMultiplier < 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(backoffMultiplier), "Backoff multiplier must be greater than or equal to 1.0.");
            }

            _initialDelay = initialDelay;
            _maxDelay = maxDelay;
            _backoffMultiplier = backoffMultiplier;
            _useJitter = useJitter;
            _random = new Random();
        }

        /// <inheritdoc />
        public async ValueTask<bool> ShouldRetryAsync(int attempt, Exception exception, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask; // Make method async for interface compliance
            return attempt > 0; // Always retry except for the first attempt
        }

        /// <inheritdoc />
        public async ValueTask<TimeSpan> GetRetryDelayAsync(int attempt, Exception exception, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask; // Make method async for interface compliance

            // Calculate exponential backoff delay
            var delay = TimeSpan.FromMilliseconds(_initialDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attempt - 1));

            // Cap the delay at the maximum
            if (delay > _maxDelay)
            {
                delay = _maxDelay;
            }

            // Add jitter if enabled
            if (_useJitter && delay > TimeSpan.Zero)
            {
                var jitter = _random.NextDouble() * 0.1 * delay.TotalMilliseconds; // ±10% jitter
                delay = delay.Add(TimeSpan.FromMilliseconds(jitter));
            }

            return delay;
        }
    }
}